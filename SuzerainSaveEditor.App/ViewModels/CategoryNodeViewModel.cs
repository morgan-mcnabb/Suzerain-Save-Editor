using System.Collections.ObjectModel;
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
    public CategoryNodeViewModel? Parent { get; }

    // children visible in the tree (filtered)
    public ObservableCollection<CategoryNodeViewModel> Children { get; } = [];

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

    // breadcrumb path from root to this node
    public string BreadcrumbPath
    {
        get
        {
            if (Parent is null)
                return Label;
            return $"{Parent.BreadcrumbPath} > {Label}";
        }
    }

    public bool IsLeaf => _allChildren.Count == 0;

    public IReadOnlyList<FieldViewModel> AllFields => _allFields;

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

        foreach (var child in children)
            Children.Add(child);

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
            Children.Clear();
            foreach (var child in _allChildren)
            {
                child.ApplyFilter(query);
                Children.Add(child);
            }

            FilteredCount = TotalCount;
            IsVisible = true;
            OnPropertyChanged(nameof(HeaderText));
            return true;
        }

        // filter children first
        var anyChildMatch = false;
        Children.Clear();
        foreach (var child in _allChildren)
        {
            if (child.ApplyFilter(query))
            {
                anyChildMatch = true;
                Children.Add(child);
            }
        }

        // count matching leaf fields
        var matchingFieldCount = _allFields.Count(f => FieldMatchesQuery(f, query));

        // sum up filtered counts from visible children
        var childFilteredCount = Children.Sum(c => c.FilteredCount);
        FilteredCount = matchingFieldCount + childFilteredCount;

        IsVisible = matchingFieldCount > 0 || anyChildMatch;
        OnPropertyChanged(nameof(HeaderText));
        return IsVisible;
    }

    // get leaf fields matching the current search query
    public IReadOnlyList<FieldViewModel> GetFilteredFields(string query)
    {
        if (string.IsNullOrEmpty(query))
            return _allFields;

        return _allFields.Where(f => FieldMatchesQuery(f, query)).ToList();
    }

    private static bool FieldMatchesQuery(FieldViewModel field, string query)
    {
        return field.Label.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               field.FieldId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               (field.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    partial void OnFilteredCountChanged(int value) => OnPropertyChanged(nameof(HeaderText));
}
