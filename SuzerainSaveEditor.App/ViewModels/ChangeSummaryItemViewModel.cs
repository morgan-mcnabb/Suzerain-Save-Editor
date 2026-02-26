namespace SuzerainSaveEditor.App.ViewModels;

public sealed record ChangeSummaryItemViewModel(
    string Label,
    string FieldType,
    string? OldValue,
    string NewValue
)
{
    // formatted display for the old value column
    public string OldValueDisplay => OldValue ?? "(none)";

    public bool HasOldValue => OldValue is not null;
}
