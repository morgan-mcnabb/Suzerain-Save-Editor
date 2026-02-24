using CommunityToolkit.Mvvm.ComponentModel;

namespace SuzerainSaveEditor.App.ViewModels;

// represents a clickable card summarizing a sub-category in the parent dashboard
public sealed partial class SubCategorySummaryViewModel : ViewModelBase
{
    private const int MaxPreviewLabels = 3;

    private readonly List<FieldViewModel> _trackedFields;

    public string Label { get; }
    public int FieldCount { get; }
    public IReadOnlyList<string> PreviewLabels { get; }
    public int RemainingCount { get; }
    public bool HasRemaining => RemainingCount > 0;
    public string RemainingText => $"+{RemainingCount} more";
    public CategoryNodeViewModel TargetNode { get; }

    [ObservableProperty]
    private int _dirtyCount;

    public bool HasDirtyFields => DirtyCount > 0;
    public string DirtyText => DirtyCount == 1 ? "1 modified" : $"{DirtyCount} modified";

    public SubCategorySummaryViewModel(CategoryNodeViewModel targetNode, string? searchQuery = null)
    {
        TargetNode = targetNode;
        Label = targetNode.Label;

        var fields = targetNode.GetAllDescendantFields(searchQuery);
        _trackedFields = fields is List<FieldViewModel> list ? list : [..fields];

        FieldCount = _trackedFields.Count;
        DirtyCount = _trackedFields.Count(f => f.IsDirty);

        var preview = _trackedFields.Take(MaxPreviewLabels).Select(f => f.Label).ToList();
        PreviewLabels = preview;
        RemainingCount = Math.Max(0, FieldCount - preview.Count);
    }

    // recalculates dirty count from tracked fields without rebuilding
    public void RefreshDirtyCount()
    {
        DirtyCount = _trackedFields.Count(f => f.IsDirty);
    }

    partial void OnDirtyCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasDirtyFields));
        OnPropertyChanged(nameof(DirtyText));
    }
}
