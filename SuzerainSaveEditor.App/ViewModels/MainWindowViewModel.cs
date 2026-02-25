using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuzerainSaveEditor.App.Services;
using SuzerainSaveEditor.Core.Schema;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISaveFileService _saveFileService;
    private readonly ISchemaService _schemaService;
    private readonly IFieldResolver _fieldResolver;
    private readonly IFileDialogService _fileDialogService;
    private readonly IFieldDiscoveryService _discoveryService;

    private IEditSession? _editSession;
    private ISchemaService? _activeSchema;

    // backing lists (unfiltered, preserving creation order)
    private readonly List<FieldViewModel> _allGeneralFields = [];
    private readonly List<FieldViewModel> _allSordlandFields = [];
    private readonly List<FieldViewModel> _allRiziaFields = [];
    private readonly List<FieldViewModel> _allAdvancedFields = [];
    private readonly List<CategoryNodeViewModel> _allCategoryNodes = [];
    private readonly Dictionary<string, FieldViewModel> _fieldLookup = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _validationErrors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CategoryNodeViewModel> _categoryNodeLookup = new(StringComparer.Ordinal);
    private bool _suppressCategoryChanged;
    private Dictionary<string, bool>? _savedExpansionStates;
    private CancellationTokenSource? _searchDebounce;
    private const int SearchDebounceMs = 250;

    // observable collections bound to UI (filtered by search)
    public BatchObservableCollection<FieldViewModel> GeneralFields { get; } = new();
    public BatchObservableCollection<FieldViewModel> SordlandFields { get; } = new();
    public BatchObservableCollection<FieldViewModel> RiziaFields { get; } = new();

    // tree navigation for advanced tab
    public BatchObservableCollection<CategoryNodeViewModel> CategoryNodes { get; } = new();
    public BatchObservableCollection<FieldViewModel> SelectedCategoryFields { get; } = new();
    public BatchObservableCollection<SubCategorySummaryViewModel> SubCategorySummaries { get; } = new();
    public BatchObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = new();

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

    public string WindowTitle => IsFileLoaded
        ? $"Suzerain Save Editor \u2014 {Path.GetFileName(FilePath)}{(IsDirty ? " *" : "")}"
        : "Suzerain Save Editor";

    public MainWindowViewModel(
        ISaveFileService saveFileService,
        ISchemaService schemaService,
        IFieldResolver fieldResolver,
        IFileDialogService fileDialogService,
        IFieldDiscoveryService discoveryService)
    {
        _saveFileService = saveFileService;
        _schemaService = schemaService;
        _fieldResolver = fieldResolver;
        _fileDialogService = fileDialogService;
        _discoveryService = discoveryService;
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
                StatusMessage = $"Failed to load: {ex.Message}";
                IsFileLoaded = false;
                return;
            }

            SelectedGroupIndex = savedTab;
            SearchText = savedSearch;

            // restore category selection
            if (savedCategoryKey is not null)
                SelectCategoryByKey(savedCategoryKey);

            StatusMessage = "Saved successfully";
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

        foreach (var field in AllFields())
            field.ResetToOriginal();

        UpdateDirtyState();
        PopulateSelectedCategoryContent();
        StatusMessage = "All changes reverted";
    }

    private bool CanRevert() => IsDirty && IsFileLoaded;

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;
        _ = ApplyFilterDebouncedAsync(token);
    }

    private async Task ApplyFilterDebouncedAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(SearchDebounceMs, token);
            ApplyFilter();
        }
        catch (OperationCanceledException) { }
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
            StatusMessage = $"Failed to load: {ex.Message}";
            IsFileLoaded = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // core loading logic without IsLoading management so callers can control the overlay
    private async Task LoadFileCoreAsync(string path)
    {
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

        FilePath = path;
        IsFileLoaded = true;
        SaveCommittedToDisk = false;

        PopulateFields();
        UpdateDirtyState();

        StatusMessage = $"Loaded: {Path.GetFileName(path)}";
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
        _savedExpansionStates = null;

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
        _categoryNodeLookup.Clear();

        // get field definitions for advanced fields
        var advancedDefs = _allAdvancedFields
            .Select(vm => schema.GetById(vm.FieldId))
            .Where(d => d is not null)
            .Cast<FieldDefinition>()
            .ToList();

        // build hierarchical categories
        var categories = AdvancedFieldGrouper.GroupFieldsHierarchical(advancedDefs);

        // convert FieldCategory tree → CategoryNodeViewModel tree
        foreach (var category in categories)
        {
            var node = BuildCategoryNode(category, _fieldLookup, parent: null);
            _allCategoryNodes.Add(node);
            IndexCategoryNode(node);
        }
    }

    private void IndexCategoryNode(CategoryNodeViewModel node)
    {
        _categoryNodeLookup[node.Key] = node;
        foreach (var child in node.Children)
            IndexCategoryNode(child);
    }

    private static CategoryNodeViewModel BuildCategoryNode(
        FieldCategory category,
        Dictionary<string, FieldViewModel> vmLookup,
        CategoryNodeViewModel? parent)
    {
        // resolve field VMs for this category's leaf fields
        var fieldVms = new List<FieldViewModel>();
        foreach (var def in category.Fields)
        {
            if (vmLookup.TryGetValue(def.Id, out var vm))
                fieldVms.Add(vm);
        }

        // build children first with null parent (fixed up below)
        var childNodes = new List<CategoryNodeViewModel>();
        foreach (var childCategory in category.Children)
        {
            var childNode = BuildCategoryNode(childCategory, vmLookup, parent: null);
            childNodes.Add(childNode);
        }

        // create node with actual children
        var node = new CategoryNodeViewModel(
            category.Key,
            category.Label,
            category.SortOrder,
            fieldVms,
            childNodes,
            parent);

        // fix up children's parent references to point to this node
        foreach (var child in childNodes)
            child.Parent = node;

        return node;
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

        SaveCommittedToDisk = false;
        var result = _editSession.SetValue(fieldId, value);

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
        ValidationStatusText = HasValidationErrors
            ? _validationErrors.Values.First()
            : "Valid";
    }

    internal void ApplyFilter()
    {
        ApplyFilterToGroup(_allGeneralFields, GeneralFields);
        ApplyFilterToGroup(_allSordlandFields, SordlandFields);
        ApplyFilterToGroup(_allRiziaFields, RiziaFields);
        ApplyFilterToCategoryTree();
    }

    private void ApplyFilterToGroup(List<FieldViewModel> source, BatchObservableCollection<FieldViewModel> target)
    {
        var query = SearchText?.Trim() ?? "";

        if (string.IsNullOrEmpty(query))
        {
            target.ReplaceAll(source);
            return;
        }

        var filtered = new List<FieldViewModel>();
        foreach (var field in source)
        {
            if (field.Label.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                field.FieldId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (field.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                filtered.Add(field);
            }
        }
        target.ReplaceAll(filtered);
    }

    private void ApplyFilterToCategoryTree()
    {
        var query = SearchText?.Trim() ?? "";
        var isSearching = !string.IsNullOrEmpty(query);

        // save expansion states when entering search mode
        if (isSearching && _savedExpansionStates is null)
        {
            _savedExpansionStates = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var (key, node) in _categoryNodeLookup)
                _savedExpansionStates[key] = node.IsExpanded;
        }

        // capture selection before replacing — Reset triggers the TreeView
        // two-way binding to write null back into SelectedCategory
        var savedSelection = SelectedCategory;

        var visible = new List<CategoryNodeViewModel>();
        foreach (var node in _allCategoryNodes)
        {
            node.ApplyFilter(query);
            if (node.IsVisible)
                visible.Add(node);
        }

        if (isSearching)
        {
            // expand all visible nodes at every level so matching descendants are visible
            foreach (var node in _categoryNodeLookup.Values)
            {
                if (node.IsVisible)
                    node.IsExpanded = true;
            }
        }
        else if (_savedExpansionStates is not null)
        {
            // restore pre-search expansion states
            foreach (var (key, wasExpanded) in _savedExpansionStates)
            {
                if (_categoryNodeLookup.TryGetValue(key, out var node))
                    node.IsExpanded = wasExpanded;
            }
            _savedExpansionStates = null;
        }

        _suppressCategoryChanged = true;
        try
        {
            CategoryNodes.ReplaceAll(visible);

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
