namespace SuzerainSaveEditor.App.ViewModels;

// extracts field and category tree filtering logic from MainWindowViewModel
public sealed class FieldFilterService
{
    private Dictionary<string, bool>? _savedExpansionStates;

    public sealed record TreeFilterResult(
        List<CategoryNodeViewModel> VisibleRootNodes,
        Dictionary<string, bool>? ExpansionUpdates);

    // filters a flat field list by search query (static, pure)
    public static List<FieldViewModel> FilterGroup(string query, List<FieldViewModel> source)
    {
        if (string.IsNullOrEmpty(query))
            return source;

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
        return filtered;
    }

    // filters the category tree, manages expansion state save/restore,
    // returns visible roots and any expansion updates to apply
    public TreeFilterResult FilterCategoryTree(
        string query,
        List<CategoryNodeViewModel> allCategoryNodes,
        Dictionary<string, CategoryNodeViewModel> categoryNodeLookup)
    {
        var isSearching = !string.IsNullOrEmpty(query);

        // save expansion states when entering search mode
        if (isSearching && _savedExpansionStates is null)
        {
            _savedExpansionStates = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var (key, node) in categoryNodeLookup)
                _savedExpansionStates[key] = node.IsExpanded;
        }

        // apply filter to each root node (mutates IsVisible/FilteredCount/Children)
        var visible = new List<CategoryNodeViewModel>();
        foreach (var node in allCategoryNodes)
        {
            node.ApplyFilter(query);
            if (node.IsVisible)
                visible.Add(node);
        }

        // compute expansion updates
        Dictionary<string, bool>? expansionUpdates = null;

        if (isSearching)
        {
            // expand all visible nodes so matching descendants are visible
            expansionUpdates = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var (key, node) in categoryNodeLookup)
            {
                if (node.IsVisible)
                    expansionUpdates[key] = true;
            }
        }
        else if (_savedExpansionStates is not null)
        {
            // restore pre-search expansion states
            expansionUpdates = _savedExpansionStates;
            _savedExpansionStates = null;
        }

        return new TreeFilterResult(visible, expansionUpdates);
    }

    // clears saved expansion states (call on file load/clear)
    public void Reset()
    {
        _savedExpansionStates = null;
    }

    // exposes whether expansion states are currently saved (for testing)
    internal bool HasSavedExpansionStates => _savedExpansionStates is not null;
}
