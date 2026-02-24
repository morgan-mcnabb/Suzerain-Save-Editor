using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Core.Services;

public interface IEditSession
{
    string? FilePath { get; }
    bool IsDirty { get; }
    int DirtyCount { get; }
    SaveDocument OriginalDocument { get; }
    SaveDocument CurrentDocument { get; }

    string? GetValue(string fieldId);
    ValidationResult SetValue(string fieldId, string value);
    void RevertField(string fieldId);
    void RevertAll();
    bool IsFieldDirty(string fieldId);
    IReadOnlyCollection<FieldEdit> GetDirtyFields();
    ValidationResult ValidateField(string fieldId, string value);
    ValidationResult ValidateAll();
}
