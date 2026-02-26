using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuzerainSaveEditor.App.Services;
using SuzerainSaveEditor.Core.Schema;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISaveFileService _saveFileService;
    private readonly ISchemaService _schemaService;
    private readonly IFieldResolver _fieldResolver;
    private readonly IFileDialogService _fileDialogService;
    private readonly IFieldDiscoveryService _discoveryService;
    private readonly IUndoRedoService _undoRedoService;
    private readonly IRecentFilesService? _recentFilesService;

    private IEditSession? _editSession;
    private ISchemaService? _activeSchema;
    private bool _isApplyingUndoRedo;

    // backing lists (unfiltered, preserving creation order)
    private readonly List<FieldViewModel> _allGeneralFields = [];
    private readonly List<FieldViewModel> _allSordlandFields = [];
    private readonly List<FieldViewModel> _allRiziaFields = [];
    private readonly List<FieldViewModel> _allAdvancedFields = [];
    private readonly List<CategoryNodeViewModel> _allCategoryNodes = [];
    private readonly Dictionary<string, FieldViewModel> _fieldLookup = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _validationErrors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CategoryNodeViewModel> _categoryNodeLookup = new(StringComparer.Ordinal);
    private readonly FieldFilterService _filterService = new();
    private bool _suppressCategoryChanged;
    private CancellationTokenSource? _searchDebounce;
    private CancellationTokenSource? _statusClearTimer;
    private const int SearchDebounceMs = 250;
    private const int StatusClearDelayMs = 4000;

    // observable collections bound to UI (filtered by search)
    public BatchObservableCollection<FieldViewModel> GeneralFields { get; } = new();
    public BatchObservableCollection<FieldViewModel> SordlandFields { get; } = new();
    public BatchObservableCollection<FieldViewModel> RiziaFields { get; } = new();

    // tree navigation for advanced tab
    public BatchObservableCollection<CategoryNodeViewModel> CategoryNodes { get; } = new();
    public BatchObservableCollection<FieldViewModel> SelectedCategoryFields { get; } = new();
    public BatchObservableCollection<SubCategorySummaryViewModel> SubCategorySummaries { get; } = new();
    public BatchObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = new();

    // recent files shown on the empty-state landing page
    public BatchObservableCollection<RecentFileViewModel> RecentFiles { get; } = new();

    [ObservableProperty]
    private bool _isFileLoaded;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private int _selectedGroupIndex;

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private int _changeCount;

    [ObservableProperty]
    private string _changeCountText = "";

    [ObservableProperty]
    private string _validationStatusText = "";

    [ObservableProperty]
    private bool _hasValidationErrors;

    [ObservableProperty]
    private int _advancedFieldCount;

    [ObservableProperty]
    private CategoryNodeViewModel? _selectedCategory;

    [ObservableProperty]
    private string _selectedCategoryPath = "";

    [ObservableProperty]
    private bool _hasCategorySelected;

    [ObservableProperty]
    private bool _showCategoryCards;

    [ObservableProperty]
    private bool _showCategoryFields;

    [ObservableProperty]
    private bool _saveCommittedToDisk;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private string _undoTooltip = "Nothing to undo";

    [ObservableProperty]
    private string _redoTooltip = "Nothing to redo";

    [ObservableProperty]
    private bool _hasRecentFiles;

    public string WindowTitle => IsFileLoaded
        ? $"Suzerain Save Editor \u2014 {Path.GetFileName(FilePath)}{(IsDirty ? " *" : "")}"
        : "Suzerain Save Editor";

    public MainWindowViewModel(
        ISaveFileService saveFileService,
        ISchemaService schemaService,
        IFieldResolver fieldResolver,
        IFileDialogService fileDialogService,
        IFieldDiscoveryService discoveryService,
        IUndoRedoService? undoRedoService = null,
        IRecentFilesService? recentFilesService = null)
    {
        _saveFileService = saveFileService;
        _schemaService = schemaService;
        _fieldResolver = fieldResolver;
        _fileDialogService = fileDialogService;
        _discoveryService = discoveryService;
        _undoRedoService = undoRedoService ?? new UndoRedoService();
        _recentFilesService = recentFilesService;

        _undoRedoService.StateChanged += OnUndoRedoStateChanged;
    }

    // parameterless constructor for avalonia designer
    public MainWindowViewModel() : this(null!, null!, null!, null!, null!) { }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var path = await _fileDialogService.OpenFileAsync();
        if (path is null) return;
        await LoadFileAsync(path);
    }

    [RelayCommand]
    private async Task OpenRecentFileAsync(RecentFileViewModel entry)
    {
        await LoadFileAsync(entry.FilePath);
    }

    [RelayCommand]
    private async Task ClearRecentFilesAsync()
    {
        if (_recentFilesService is null) return;
        await _recentFilesService.ClearAsync();
        RecentFiles.Clear();
        HasRecentFiles = false;
    }

    [RelayCommand]
    private async Task RemoveRecentFileAsync(RecentFileViewModel entry)
    {
        if (_recentFilesService is null) return;
        await _recentFilesService.RemoveAsync(entry.FilePath);
        await LoadRecentFilesAsync();
    }

    public async Task LoadRecentFilesAsync()
    {
        if (_recentFilesService is null) return;

        try
        {
            var entries = await _recentFilesService.LoadAsync();
            var vms = entries.Select(e => new RecentFileViewModel(
                e.FilePath,
                e.DisplayName,
                e.LastOpenedUtc.ToLocalTime().ToString("MMM d, yyyy h:mm tt"))).ToList();

            RecentFiles.ReplaceAll(vms);
            HasRecentFiles = RecentFiles.Count > 0;
        }
        catch
        {
            // recent files is non-critical, never block the user
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_editSession?.FilePath is null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Validating...";

            var validation = _editSession.ValidateAll();
            if (!validation.IsValid)
            {
                StatusMessage = $"Cannot save: {validation.Error}";
                return;
            }

            StatusMessage = "Saving...";
            await _saveFileService.SaveAsync(_editSession.FilePath, _editSession.CurrentDocument);
            SaveCommittedToDisk = true;

            // preserve tab selection and search, reload to reset dirty state
            var savedTab = SelectedGroupIndex;
            var savedSearch = SearchText;
            var savedCategoryKey = SelectedCategory?.Key;

            try
            {
                await LoadFileCoreAsync(_editSession.FilePath);
            }
            catch (Exception ex)
            {
                ClearLoadedState();
                StatusMessage = $"Failed to load: {ex.Message}";
                return;
            }

            SelectedGroupIndex = savedTab;
            SearchText = savedSearch;

            // restore category selection
            if (savedCategoryKey is not null)
                SelectCategoryByKey(savedCategoryKey);

            SetTransientStatus("Saved successfully");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSave() => IsDirty && IsFileLoaded && !HasValidationErrors;

    [RelayCommand(CanExecute = nameof(CanRevert))]
    private void RevertAll()
    {
        if (_editSession is null) return;

        _editSession.RevertAll();
        _validationErrors.Clear();
        _undoRedoService.Clear();

        foreach (var field in AllFields())
            field.ResetToOriginal();

        UpdateDirtyState();
        PopulateSelectedCategoryContent();
        SetTransientStatus("All changes reverted");
    }

    private bool CanRevert() => IsDirty && IsFileLoaded;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_editSession is null) return;

        var entry = _undoRedoService.Undo();
        if (entry is null) return;

        _isApplyingUndoRedo = true;
        try
        {
            if (entry.OldValue is not null)
                _editSession.SetValue(entry.FieldId, entry.OldValue);
            else
                _editSession.RevertField(entry.FieldId);

            SyncFieldFromSession(entry.FieldId);
            UpdateDirtyState();

            var label = _fieldLookup.TryGetValue(entry.FieldId, out var vm) ? vm.Label : entry.FieldId;
            SetTransientStatus($"Undo: {label}");

            if (ShowCategoryCards)
                RefreshSubCategorySummaries();
        }
        finally
        {
            _isApplyingUndoRedo = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_editSession is null) return;

        var entry = _undoRedoService.Redo();
        if (entry is null) return;

        _isApplyingUndoRedo = true;
        try
        {
            _editSession.SetValue(entry.FieldId, entry.NewValue);
            SyncFieldFromSession(entry.FieldId);
            UpdateDirtyState();

            var label = _fieldLookup.TryGetValue(entry.FieldId, out var vm) ? vm.Label : entry.FieldId;
            SetTransientStatus($"Redo: {label}");

            if (ShowCategoryCards)
                RefreshSubCategorySummaries();
        }
        finally
        {
            _isApplyingUndoRedo = false;
        }
    }

    private void SyncFieldFromSession(string fieldId)
    {
        if (_editSession is null) return;
        if (!_fieldLookup.TryGetValue(fieldId, out var fieldVm)) return;

        var currentValue = _editSession.GetValue(fieldId) ?? "";
        var isDirty = _editSession.IsFieldDirty(fieldId);
        var validation = _editSession.ValidateField(fieldId, currentValue);
        var error = validation.IsValid ? null : validation.Error;

        fieldVm.UpdateFromSession(currentValue, isDirty, error);

        if (error is not null)
            _validationErrors[fieldId] = error;
        else
            _validationErrors.Remove(fieldId);
    }

    private void OnUndoRedoStateChanged()
    {
        CanUndo = _undoRedoService.CanUndo;
        CanRedo = _undoRedoService.CanRedo;
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();

        var undoPeek = _undoRedoService.PeekUndo();
        UndoTooltip = undoPeek is not null
            ? $"Undo: {GetFieldLabel(undoPeek.FieldId)} (Ctrl+Z)"
            : "Nothing to undo";

        var redoPeek = _undoRedoService.PeekRedo();
        RedoTooltip = redoPeek is not null
            ? $"Redo: {GetFieldLabel(redoPeek.FieldId)} (Ctrl+Y)"
            : "Nothing to redo";
    }

    private string GetFieldLabel(string fieldId)
    {
        return _fieldLookup.TryGetValue(fieldId, out var vm) ? vm.Label : fieldId;
    }

    partial void OnSearchTextChanged(string value)
    {
        var newCts = new CancellationTokenSource();
        var old = Interlocked.Exchange(ref _searchDebounce, newCts);
        old?.Cancel();
        old?.Dispose();
        BeginApplyFilterDebounced(newCts.Token);
    }

    // async void is intentional — this is an event handler dispatch and all
    // exceptions are caught internally so no Task needs to be observed
    private async void BeginApplyFilterDebounced(CancellationToken token)
    {
        try
        {
            await Task.Delay(SearchDebounceMs, token);
            ApplyFilter();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusMessage = $"Filter error: {ex.Message}";
        }
    }

    partial void OnIsFileLoadedChanged(bool value) => OnPropertyChanged(nameof(WindowTitle));

    partial void OnFilePathChanged(string value) => OnPropertyChanged(nameof(WindowTitle));

    partial void OnIsDirtyChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        RevertAllCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(WindowTitle));
    }

    partial void OnHasValidationErrorsChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedCategoryChanged(CategoryNodeViewModel? oldValue, CategoryNodeViewModel? newValue)
    {
        if (_suppressCategoryChanged) return;

        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
        {
            newValue.IsSelected = true;

            // always expand parent nodes when selected (breadcrumb or tree click)
            if (newValue.IsParent)
                newValue.IsExpanded = true;
        }

        PopulateSelectedCategoryContent();
    }

    public void SelectCategory(CategoryNodeViewModel? node)
    {
        SelectedCategory = node;
    }

    [RelayCommand]
    private void NavigateToSubCategory(SubCategorySummaryViewModel card)
    {
        SelectCategory(card.TargetNode);
    }

    [RelayCommand]
    private void NavigateToBreadcrumb(BreadcrumbItem item)
    {
        SelectCategory(item.Node);
    }

    private async Task LoadFileAsync(string path)
    {
        try
        {
            IsLoading = true;
            await LoadFileCoreAsync(path);
        }
        catch (Exception ex)
        {
            ClearLoadedState();
            StatusMessage = $"Failed to load: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // core loading logic without IsLoading management so callers can control the overlay
    private async Task LoadFileCoreAsync(string path)
    {
        var old = Interlocked.Exchange(ref _searchDebounce, null);
        old?.Cancel();
        old?.Dispose();

        StatusMessage = "Loading...";

        var document = await _saveFileService.OpenAsync(path);

        // offload CPU-bound work to a background thread to avoid blocking the UI
        var (editSession, activeSchema) = await Task.Run(() =>
        {
            var discovered = _discoveryService.DiscoverFields(document);
            var schema = new Core.Schema.CompositeSchemaService(_schemaService, discovered);
            var session = new EditSession(document, path, schema, _fieldResolver);
            return (session, (ISchemaService)schema);
        });

        _activeSchema = activeSchema;
        _editSession = editSession;
        _undoRedoService.Clear();

        FilePath = path;
        IsFileLoaded = true;
        SaveCommittedToDisk = false;

        PopulateFields();
        UpdateDirtyState();

        StatusMessage = $"Loaded: {Path.GetFileName(path)}";

        if (_recentFilesService is not null)
        {
            try
            {
                await _recentFilesService.AddAsync(path);
                await LoadRecentFilesAsync();
            }
            catch
            {
                // recent files persistence failure should never block the user
            }
        }
    }

    private void ClearLoadedState()
    {
        var oldCts = Interlocked.Exchange(ref _searchDebounce, null);
        oldCts?.Cancel();
        oldCts?.Dispose();

        _editSession = null;
        _activeSchema = null;

        // detach callbacks before clearing to break delegate reference to this VM
        foreach (var field in AllFields())
            field.Detach();

        _allGeneralFields.Clear();
        _allSordlandFields.Clear();
        _allRiziaFields.Clear();
        _allAdvancedFields.Clear();
        _allCategoryNodes.Clear();
        _fieldLookup.Clear();
        _validationErrors.Clear();
        _categoryNodeLookup.Clear();
        _filterService.Reset();

        GeneralFields.Clear();
        SordlandFields.Clear();
        RiziaFields.Clear();
        CategoryNodes.Clear();
        SelectedCategoryFields.Clear();
        SubCategorySummaries.Clear();
        BreadcrumbItems.Clear();

        AdvancedFieldCount = 0;
        SelectedCategory = null;
        HasCategorySelected = false;
        ShowCategoryCards = false;
        ShowCategoryFields = false;
        SelectedCategoryPath = "";
        FilePath = "";
        IsFileLoaded = false;

        UpdateDirtyState();
    }

    private void PopulateFields()
    {
        _allGeneralFields.Clear();
        _allSordlandFields.Clear();
        _allRiziaFields.Clear();
        _allAdvancedFields.Clear();
        _allCategoryNodes.Clear();
        _fieldLookup.Clear();
        _validationErrors.Clear();
        _filterService.Reset();

        var schema = _activeSchema ?? _schemaService;

        foreach (var field in schema.GetAll())
        {
            var value = _editSession!.GetValue(field.Id);
            var vm = new FieldViewModel(
                field.Id,
                field.Label,
                field.Description,
                field.Type,
                value,
                field.Min,
                field.Max,
                field.Options,
                OnFieldValueChanged);

            _fieldLookup[field.Id] = vm;

            switch (field.Group)
            {
                case FieldGroup.General:
                    _allGeneralFields.Add(vm);
                    break;
                case FieldGroup.Sordland:
                    _allSordlandFields.Add(vm);
                    break;
                case FieldGroup.Rizia:
                    _allRiziaFields.Add(vm);
                    break;
                case FieldGroup.Advanced:
                    _allAdvancedFields.Add(vm);
                    break;
            }
        }

        // build hierarchical category tree for advanced fields
        BuildCategoryTree(schema);

        AdvancedFieldCount = _allAdvancedFields.Count;
        SelectedCategory = null;
        HasCategorySelected = false;
        ShowCategoryCards = false;
        ShowCategoryFields = false;
        SelectedCategoryPath = "";
        SelectedCategoryFields.Clear();
        SubCategorySummaries.Clear();
        ApplyFilter();
    }

    private void BuildCategoryTree(ISchemaService schema)
    {
        var result = CategoryTreeBuilder.Build(_allAdvancedFields, schema, _fieldLookup);

        _categoryNodeLookup.Clear();
        foreach (var (key, node) in result.NodeLookup)
            _categoryNodeLookup[key] = node;

        _allCategoryNodes.AddRange(result.RootNodes);
    }

    private void PopulateSelectedCategoryContent()
    {
        if (SelectedCategory is null)
        {
            SelectedCategoryFields.Clear();
            SubCategorySummaries.Clear();
            BreadcrumbItems.Clear();
            HasCategorySelected = false;
            ShowCategoryCards = false;
            ShowCategoryFields = false;
            SelectedCategoryPath = "";
            return;
        }

        HasCategorySelected = true;
        SelectedCategoryPath = SelectedCategory.BreadcrumbPath;
        BuildBreadcrumbItems(SelectedCategory);

        var query = SearchText?.Trim() ?? "";

        if (SelectedCategory.IsParent)
        {
            // parent node — show sub-category cards
            ShowCategoryCards = true;
            ShowCategoryFields = false;

            var summaries = SelectedCategory.GetSubCategorySummaries(query);
            SubCategorySummaries.ReplaceAll(summaries);
            SelectedCategoryFields.Clear();
        }
        else
        {
            // leaf node — show field editors
            ShowCategoryCards = false;
            ShowCategoryFields = true;

            var fields = SelectedCategory.GetFilteredFields(query);
            SelectedCategoryFields.ReplaceAll(fields);
            SubCategorySummaries.Clear();
        }
    }

    private void BuildBreadcrumbItems(CategoryNodeViewModel node)
    {
        // walk up to root, collect ancestors
        var segments = new List<BreadcrumbItem>();
        var current = node;
        while (current is not null)
        {
            segments.Add(new BreadcrumbItem(current.Label, current, false));
            current = current.Parent;
        }

        segments.Reverse();
        if (segments.Count > 0)
            segments[^1] = segments[^1] with { IsLast = true };

        BreadcrumbItems.ReplaceAll(segments);
    }

    private void OnFieldValueChanged(string fieldId, string value)
    {
        if (_editSession is null) return;

        // when undo/redo is applying a value, do not push back onto the stack
        if (_isApplyingUndoRedo) return;

        SaveCommittedToDisk = false;

        // capture the value before this edit so undo can restore it
        var previousValue = _editSession.GetValue(fieldId);
        var result = _editSession.SetValue(fieldId, value);

        // only record valid edits in the undo stack
        if (result.IsValid)
            _undoRedoService.Push(fieldId, previousValue, value);

        var fieldVm = FindFieldViewModel(fieldId);
        if (fieldVm is null) return;

        // always sync dirty indicator from session state — validation failure
        // doesn't modify the edit, but the indicator should still be consistent
        fieldVm.IsDirty = _editSession.IsFieldDirty(fieldId);

        if (!result.IsValid)
        {
            fieldVm.ValidationError = result.Error;
            _validationErrors[fieldId] = result.Error ?? "Invalid";
        }
        else
        {
            fieldVm.ValidationError = null;
            _validationErrors.Remove(fieldId);
        }

        UpdateDirtyState();

        // refresh sub-category cards so dirty pills stay current
        if (ShowCategoryCards)
            RefreshSubCategorySummaries();
    }

    private void UpdateDirtyState()
    {
        if (_editSession is null)
        {
            IsDirty = false;
            ChangeCount = 0;
            ChangeCountText = "";
            ValidationStatusText = "";
            HasValidationErrors = false;
            return;
        }

        ChangeCount = _editSession.DirtyCount;
        IsDirty = ChangeCount > 0;
        ChangeCountText = ChangeCount switch
        {
            0 => "No changes",
            1 => "1 unsaved change",
            _ => $"{ChangeCount} unsaved changes"
        };

        HasValidationErrors = _validationErrors.Count > 0;
        ValidationStatusText = _validationErrors.Count switch
        {
            0 => "Valid",
            1 => FormatSingleValidationError(),
            _ => $"{_validationErrors.Count} validation errors"
        };
    }

    private string DefaultStatusMessage()
    {
        if (!IsFileLoaded) return "Ready";
        return $"Loaded: {Path.GetFileName(FilePath)}";
    }

    private void SetTransientStatus(string message)
    {
        StatusMessage = message;

        var newCts = new CancellationTokenSource();
        var old = Interlocked.Exchange(ref _statusClearTimer, newCts);
        old?.Cancel();
        old?.Dispose();
        BeginClearStatusDebounced(newCts.Token);
    }

    // async void is intentional — fire-and-forget with internal error handling
    private async void BeginClearStatusDebounced(CancellationToken token)
    {
        try
        {
            await Task.Delay(StatusClearDelayMs, token);
            StatusMessage = DefaultStatusMessage();
        }
        catch (OperationCanceledException) { }
    }

    private string FormatSingleValidationError()
    {
        var (fieldId, error) = _validationErrors.First();
        var label = _fieldLookup.TryGetValue(fieldId, out var vm) ? vm.Label : fieldId;
        return $"{label}: {error}";
    }

    internal void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? "";

        GeneralFields.ReplaceAll(FieldFilterService.FilterGroup(query, _allGeneralFields));
        SordlandFields.ReplaceAll(FieldFilterService.FilterGroup(query, _allSordlandFields));
        RiziaFields.ReplaceAll(FieldFilterService.FilterGroup(query, _allRiziaFields));

        ApplyTreeFilterResult(
            _filterService.FilterCategoryTree(query, _allCategoryNodes, _categoryNodeLookup));
    }

    private void ApplyTreeFilterResult(FieldFilterService.TreeFilterResult result)
    {
        // apply expansion updates to nodes
        if (result.ExpansionUpdates is not null)
        {
            foreach (var (key, expanded) in result.ExpansionUpdates)
            {
                if (_categoryNodeLookup.TryGetValue(key, out var node))
                    node.IsExpanded = expanded;
            }
        }

        // capture selection before replacing — ReplaceAll triggers the TreeView
        // two-way binding to write null back into SelectedCategory
        var savedSelection = SelectedCategory;

        _suppressCategoryChanged = true;
        try
        {
            CategoryNodes.ReplaceAll(result.VisibleRootNodes);

            // restore or clear selection based on visibility
            if (savedSelection is not null && savedSelection.IsVisible)
                SelectedCategory = savedSelection;
            else if (savedSelection is not null)
                SelectedCategory = null;
        }
        finally
        {
            _suppressCategoryChanged = false;
        }

        // refresh content panel for the (restored or cleared) selection
        PopulateSelectedCategoryContent();
    }

    private void RefreshSubCategorySummaries()
    {
        foreach (var summary in SubCategorySummaries)
            summary.RefreshDirtyCount();
    }

    private void SelectCategoryByKey(string key)
    {
        if (_categoryNodeLookup.TryGetValue(key, out var node) && node.IsVisible)
            SelectCategory(node);
    }

    private FieldViewModel? FindFieldViewModel(string fieldId)
    {
        return _fieldLookup.GetValueOrDefault(fieldId);
    }

    private IEnumerable<FieldViewModel> AllFields()
    {
        return _allGeneralFields
            .Concat(_allSordlandFields)
            .Concat(_allRiziaFields)
            .Concat(_allAdvancedFields);
    }
}
