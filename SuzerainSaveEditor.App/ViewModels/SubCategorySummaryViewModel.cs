namespace SuzerainSaveEditor.App.ViewModels;

// represents a clickable card summarizing a sub-category in the parent dashboard
public sealed class SubCategorySummaryViewModel
{
    private const int MaxPreviewLabels = 3;

    public string Label { get; }
    public int FieldCount { get; }
    public int DirtyCount { get; }
    public IReadOnlyList<string> PreviewLabels { get; }
    public int RemainingCount { get; }
    public bool HasDirtyFields => DirtyCount > 0;
    public bool HasRemaining => RemainingCount > 0;
    public string RemainingText => $"+{RemainingCount} more";
    public string DirtyText => DirtyCount == 1 ? "1 modified" : $"{DirtyCount} modified";
    public CategoryNodeViewModel TargetNode { get; }

    public SubCategorySummaryViewModel(CategoryNodeViewModel targetNode, string? searchQuery = null)
    {
        TargetNode = targetNode;
        Label = targetNode.Label;

        var fields = targetNode.GetAllDescendantFields(searchQuery);

        FieldCount = fields.Count;
        DirtyCount = fields.Count(f => f.IsDirty);

        var preview = fields.Take(MaxPreviewLabels).Select(f => f.Label).ToList();
        PreviewLabels = preview;
        RemainingCount = Math.Max(0, FieldCount - preview.Count);
    }
}
