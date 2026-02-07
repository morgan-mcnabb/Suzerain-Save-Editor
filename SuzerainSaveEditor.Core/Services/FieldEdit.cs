namespace SuzerainSaveEditor.Core.Services;

// tracks a single field edit with original and new values
public sealed record FieldEdit(string FieldId, string? OldValue, string NewValue);
