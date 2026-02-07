using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Core.Services;

// tracks edits to a save document with dirty state, validation, and revert
public sealed class EditSession : IEditSession
{
    private readonly ISchemaService _schema;
    private readonly IFieldResolver _resolver;
    private readonly Dictionary<string, FieldEdit> _edits = new();

    public string? FilePath { get; }
    public SaveDocument OriginalDocument { get; }
    public SaveDocument CurrentDocument { get; private set; }
    public bool IsDirty => _edits.Count > 0;

    public EditSession(
        SaveDocument document,
        string? filePath,
        ISchemaService schema,
        IFieldResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(resolver);

        OriginalDocument = document;
        CurrentDocument = document;
        FilePath = filePath;
        _schema = schema;
        _resolver = resolver;
    }

    public string? GetValue(string fieldId)
    {
        var field = GetFieldOrThrow(fieldId);
        return _resolver.ReadValue(CurrentDocument, field);
    }

    public ValidationResult SetValue(string fieldId, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var field = GetFieldOrThrow(fieldId);
        var validation = ValidateFieldValue(field, value);
        if (!validation.IsValid)
            return validation;

        var originalValue = _resolver.ReadValue(OriginalDocument, field);

        // compare through the resolver's normalization to handle casing differences
        // (e.g. user types "true" but ReadValue normalizes to "True")
        var written = _resolver.WriteValue(OriginalDocument, field, value);
        var normalizedValue = _resolver.ReadValue(written, field);

        // if the normalized written value matches the original, remove the edit
        if (normalizedValue == originalValue)
        {
            _edits.Remove(fieldId);
            RebuildCurrentDocument();
            return ValidationResult.Success;
        }

        _edits[fieldId] = new FieldEdit(fieldId, originalValue, value);
        RebuildCurrentDocument();
        return ValidationResult.Success;
    }

    public void RevertField(string fieldId)
    {
        GetFieldOrThrow(fieldId);
        if (_edits.Remove(fieldId))
            RebuildCurrentDocument();
    }

    public void RevertAll()
    {
        _edits.Clear();
        CurrentDocument = OriginalDocument;
    }

    public IReadOnlyList<FieldEdit> GetDirtyFields() => _edits.Values.ToList();

    public ValidationResult ValidateField(string fieldId, string value)
    {
        var field = GetFieldOrThrow(fieldId);
        return ValidateFieldValue(field, value);
    }

    public ValidationResult ValidateAll()
    {
        foreach (var edit in _edits.Values)
        {
            var field = _schema.GetById(edit.FieldId);
            if (field is null) continue;
            var validation = ValidateFieldValue(field, edit.NewValue);
            if (!validation.IsValid)
                return validation;
        }
        return ValidationResult.Success;
    }

    private void RebuildCurrentDocument()
    {
        var doc = OriginalDocument;
        foreach (var edit in _edits.Values)
        {
            var field = _schema.GetById(edit.FieldId);
            if (field is null) continue;
            doc = _resolver.WriteValue(doc, field, edit.NewValue);
        }
        CurrentDocument = doc;
    }

    private FieldDefinition GetFieldOrThrow(string fieldId)
    {
        return _schema.GetById(fieldId)
            ?? throw new KeyNotFoundException($"Field '{fieldId}' not found in schema.");
    }

    private static ValidationResult ValidateFieldValue(FieldDefinition field, string value)
    {
        return field.Type switch
        {
            FieldType.Bool => ValidateBool(value),
            FieldType.Int => ValidateInt(field, value),
            FieldType.Decimal => ValidateDecimal(value),
            FieldType.String => ValidationResult.Success,
            FieldType.Enum => ValidateEnum(field, value),
            _ => ValidationResult.Success
        };
    }

    private static ValidationResult ValidateBool(string value)
    {
        if (!bool.TryParse(value, out _))
            return ValidationResult.Failure($"'{value}' is not a valid boolean. Use 'True' or 'False'.");
        return ValidationResult.Success;
    }

    private static ValidationResult ValidateInt(FieldDefinition field, string value)
    {
        if (!int.TryParse(value, out var intValue))
            return ValidationResult.Failure($"'{value}' is not a valid integer.");

        if (field.Min.HasValue && intValue < field.Min.Value)
            return ValidationResult.Failure($"Value {intValue} is below minimum {field.Min.Value}.");

        if (field.Max.HasValue && intValue > field.Max.Value)
            return ValidationResult.Failure($"Value {intValue} exceeds maximum {field.Max.Value}.");

        return ValidationResult.Success;
    }

    private static ValidationResult ValidateDecimal(string value)
    {
        if (!double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
            return ValidationResult.Failure($"'{value}' is not a valid number.");
        return ValidationResult.Success;
    }

    private static ValidationResult ValidateEnum(FieldDefinition field, string value)
    {
        if (field.Options is null || field.Options.Count == 0)
            return ValidationResult.Success;

        if (!field.Options.Contains(value))
            return ValidationResult.Failure(
                $"'{value}' is not a valid option. Valid options: {string.Join(", ", field.Options)}.");

        return ValidationResult.Success;
    }
}
