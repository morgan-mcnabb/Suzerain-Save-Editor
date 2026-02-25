using CommunityToolkit.Mvvm.ComponentModel;

namespace SuzerainSaveEditor.App.ViewModels;

// represents a node in the advanced field category tree (namespace or sub-category)
public partial class CategoryNodeViewModel : ViewModelBase
{
    private readonly List<FieldViewModel> _allFields;
    private readonly List<CategoryNodeViewModel> _allChildren;

    public string Key { get; }
    public string Label { get; }
    public int SortOrder { get; }
    public CategoryNodeViewModel? Parent { get; internal set; }

    // children visible in the tree (filtered)
    public BatchObservableCollection<CategoryNodeViewModel> Children { get; } = new();

    // total field count including all descendants
    public int TotalCount { get; }

    [ObservableProperty]
    private int _filteredCount;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isVisible = true;

    public string HeaderText => FilteredCount == TotalCount
        ? $"{Label} ({TotalCount})"
        : $"{Label} ({FilteredCount}/{TotalCount})";

    // breadcrumb path from root to this node (cached since tree structure never changes)
    private string? _breadcrumbPath;
    public string BreadcrumbPath => _breadcrumbPath ??= Parent is null
        ? Label
        : $"{Parent.BreadcrumbPath} > {Label}";

    public bool IsLeaf => _allChildren.Count == 0;

    public bool IsParent => _allChildren.Count > 0;

    public IReadOnlyList<FieldViewModel> AllFields => _allFields;

    // collects all fields from this node and all descendants
    public IReadOnlyList<FieldViewModel> GetAllDescendantFields(string? query = null)
    {
        if (IsLeaf)
            return string.IsNullOrEmpty(query) ? _allFields : GetFilteredFields(query);

        var result = new List<FieldViewModel>();
        CollectDescendantFields(result, query);
        return result;
    }

    private void CollectDescendantFields(List<FieldViewModel> result, string? query)
    {
        foreach (var field in _allFields)
        {
            if (string.IsNullOrEmpty(query) || FieldMatchesQuery(field, query))
                result.Add(field);
        }
        foreach (var child in _allChildren)
            child.CollectDescendantFields(result, query);
    }

    // builds summary cards for each child node (used by the parent dashboard)
    public IReadOnlyList<SubCategorySummaryViewModel> GetSubCategorySummaries(string? searchQuery = null)
    {
        if (IsLeaf) return [];

        var query = searchQuery?.Trim() ?? "";
        var source = string.IsNullOrEmpty(query) ? _allChildren : Children.ToList();

        return source
            .Select(child => new SubCategorySummaryViewModel(child, string.IsNullOrEmpty(query) ? null : query))
            .Where(s => s.FieldCount > 0)
            .ToList();
    }

    public CategoryNodeViewModel(
        string key,
        string label,
        int sortOrder,
        List<FieldViewModel> fields,
        List<CategoryNodeViewModel> children,
        CategoryNodeViewModel? parent = null)
    {
        Key = key;
        Label = label;
        SortOrder = sortOrder;
        Parent = parent;
        _allFields = fields;
        _allChildren = children;

        Children.ReplaceAll(children);

        // total count = own fields + sum of children's totals
        TotalCount = _allFields.Count + _allChildren.Sum(c => c.TotalCount);
        FilteredCount = TotalCount;
    }

    // applies search filter, returns true if this node or any descendant has matches
    public bool ApplyFilter(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            // no filter — restore all children and full count
            // skip collection rebuild if already showing all children to avoid unnecessary UI re-layout
            if (Children.Count != _allChildren.Count)
                Children.ReplaceAll(_allChildren);

            foreach (var child in _allChildren)
                child.ApplyFilter(query);

            FilteredCount = TotalCount;
            IsVisible = true;
            return true;
        }

        // filter children first
        var visibleChildren = new List<CategoryNodeViewModel>();
        foreach (var child in _allChildren)
        {
            if (child.ApplyFilter(query))
                visibleChildren.Add(child);
        }

        // skip collection rebuild if the visible set hasn't changed
        if (!ChildrenMatchSequence(visibleChildren))
            Children.ReplaceAll(visibleChildren);

        // count matching leaf fields
        var matchingFieldCount = _allFields.Count(f => FieldMatchesQuery(f, query));

        // sum up filtered counts from visible children
        var childFilteredCount = visibleChildren.Sum(c => c.FilteredCount);
        FilteredCount = matchingFieldCount + childFilteredCount;

        IsVisible = matchingFieldCount > 0 || visibleChildren.Count > 0;
        return IsVisible;
    }

    // get leaf fields matching the current search query
    public IReadOnlyList<FieldViewModel> GetFilteredFields(string query)
    {
        if (string.IsNullOrEmpty(query))
            return _allFields;

        return _allFields.Where(f => FieldMatchesQuery(f, query)).ToList();
    }

    private bool ChildrenMatchSequence(List<CategoryNodeViewModel> newChildren)
    {
        if (Children.Count != newChildren.Count) return false;
        for (var i = 0; i < Children.Count; i++)
        {
            if (!ReferenceEquals(Children[i], newChildren[i])) return false;
        }
        return true;
    }

    private static bool FieldMatchesQuery(FieldViewModel field, string query)
    {
        return field.Label.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               field.FieldId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               (field.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    partial void OnFilteredCountChanged(int value) => OnPropertyChanged(nameof(HeaderText));
}
