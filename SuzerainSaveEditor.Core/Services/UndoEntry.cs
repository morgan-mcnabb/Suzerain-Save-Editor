namespace SuzerainSaveEditor.Core.Services;

public sealed record UndoEntry(string FieldId, string? OldValue, string NewValue);
