using System.Collections.ObjectModel;
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
    private CancellationTokenSource? _searchDebounce;
    private const int SearchDebounceMs = 250;

    // observable collections bound to UI (filtered by search)
    public ObservableCollection<FieldViewModel> GeneralFields { get; } = [];
    public ObservableCollection<FieldViewModel> SordlandFields { get; } = [];
    public ObservableCollection<FieldViewModel> RiziaFields { get; } = [];

    // tree navigation for advanced tab
    public ObservableCollection<CategoryNodeViewModel> CategoryNodes { get; } = [];
    public ObservableCollection<FieldViewModel> SelectedCategoryFields { get; } = [];
    public ObservableCollection<SubCategorySummaryViewModel> SubCategorySummaries { get; } = [];
    public ObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = [];

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
            await LoadFileAsync(_editSession.FilePath);

            if (!IsFileLoaded)
                return;

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

    private bool CanSave() => IsDirty && IsFileLoaded;

    [RelayCommand(CanExecute = nameof(CanRevert))]
    private void RevertAll()
    {
        if (_editSession is null) return;

        _editSession.RevertAll();
        _validationErrors.Clear();

        foreach (var field in AllFields())
            field.ResetToOriginal();

        UpdateDirtyState();
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

    partial void OnSelectedCategoryChanged(CategoryNodeViewModel? oldValue, CategoryNodeViewModel? newValue)
    {
        if (_suppressCategoryChanged) return;

        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
        {
            newValue.IsSelected = true;

            // auto-expand/collapse parent nodes when selected
            if (newValue.IsParent)
                newValue.IsExpanded = !newValue.IsExpanded;
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
            StatusMessage = "Loading...";

            var document = await _saveFileService.OpenAsync(path);
            var discovered = _discoveryService.DiscoverFields(document);
            _activeSchema = new Core.Schema.CompositeSchemaService(_schemaService, discovered);
            _editSession = new EditSession(document, path, _activeSchema, _fieldResolver);

            FilePath = path;
            IsFileLoaded = true;
            SaveCommittedToDisk = false;

            PopulateFields();
            UpdateDirtyState();

            StatusMessage = $"Loaded: {Path.GetFileName(path)}";
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

    private void PopulateFields()
    {
        _allGeneralFields.Clear();
        _allSordlandFields.Clear();
        _allRiziaFields.Clear();
        _allAdvancedFields.Clear();
        _allCategoryNodes.Clear();
        _fieldLookup.Clear();
        _validationErrors.Clear();

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

        // map FieldDefinition → FieldViewModel for quick lookup
        var vmLookup = _allAdvancedFields.ToDictionary(vm => vm.FieldId, StringComparer.Ordinal);

        // convert FieldCategory tree → CategoryNodeViewModel tree
        foreach (var category in categories)
        {
            var node = BuildCategoryNode(category, vmLookup, parent: null);
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
        SelectedCategoryFields.Clear();
        SubCategorySummaries.Clear();
        BreadcrumbItems.Clear();

        if (SelectedCategory is null)
        {
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
            foreach (var summary in summaries)
                SubCategorySummaries.Add(summary);
        }
        else
        {
            // leaf node — show field editors
            ShowCategoryCards = false;
            ShowCategoryFields = true;

            var fields = SelectedCategory.GetFilteredFields(query);
            foreach (var field in fields)
                SelectedCategoryFields.Add(field);
        }
    }

    private void BuildBreadcrumbItems(CategoryNodeViewModel node)
    {
        // walk up to root, collect ancestors
        var segments = new List<CategoryNodeViewModel>();
        var current = node;
        while (current is not null)
        {
            segments.Add(current);
            current = current.Parent;
        }

        segments.Reverse();

        for (var i = 0; i < segments.Count; i++)
            BreadcrumbItems.Add(new BreadcrumbItem(segments[i].Label, segments[i], i == segments.Count - 1));
    }

    private void OnFieldValueChanged(string fieldId, string value)
    {
        if (_editSession is null) return;

        SaveCommittedToDisk = false;
        var result = _editSession.SetValue(fieldId, value);

        var fieldVm = FindFieldViewModel(fieldId);
        if (fieldVm is null) return;

        if (!result.IsValid)
        {
            fieldVm.ValidationError = result.Error;
            _validationErrors[fieldId] = result.Error ?? "Invalid";
        }
        else
        {
            fieldVm.ValidationError = null;
            fieldVm.IsDirty = _editSession.IsFieldDirty(fieldId);
            _validationErrors.Remove(fieldId);
        }

        UpdateDirtyState();
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

    private void ApplyFilterToGroup(List<FieldViewModel> source, ObservableCollection<FieldViewModel> target)
    {
        target.Clear();
        var query = SearchText?.Trim() ?? "";

        foreach (var field in source)
        {
            if (string.IsNullOrEmpty(query) ||
                field.Label.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                field.FieldId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (field.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                target.Add(field);
            }
        }
    }

    private void ApplyFilterToCategoryTree()
    {
        var query = SearchText?.Trim() ?? "";
        var isSearching = !string.IsNullOrEmpty(query);

        // capture selection before clearing — Clear() triggers the TreeView
        // two-way binding to write null back into SelectedCategory
        var savedSelection = SelectedCategory;

        _suppressCategoryChanged = true;
        try
        {
            CategoryNodes.Clear();
            foreach (var node in _allCategoryNodes)
            {
                node.ApplyFilter(query);
                if (node.IsVisible)
                {
                    if (isSearching)
                        node.IsExpanded = true;
                    CategoryNodes.Add(node);
                }
            }

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
