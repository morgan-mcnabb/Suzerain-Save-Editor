using System.Text.Json.Nodes;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Core.Services;

// tracks edits to a save document with dirty state, validation, and revert
public sealed class EditSession : IEditSession
{
    private readonly ISchemaService _schema;
    private readonly IFieldResolver _resolver;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, FieldEdit> _edits = new();

    public string? FilePath { get; }
    public SaveDocument OriginalDocument { get; }
    public SaveDocument CurrentDocument { get; private set; }

    public bool IsDirty
    {
        get { lock (_lock) return _edits.Count > 0; }
    }

    public int DirtyCount
    {
        get { lock (_lock) return _edits.Count; }
    }

    public EditSession(
        SaveDocument document,
        string? filePath,
        ISchemaService schema,
        IFieldResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(resolver);

        var variables = document.Variables.ToList();
        var entities = document.EntityUpdates.ToList();

        OriginalDocument = new SaveDocument
        {
            Metadata = document.Metadata,
            WarSaveData = (JsonObject)document.WarSaveData.DeepClone(),
            Variables = variables,
            EntityUpdates = entities
        };
        CurrentDocument = new SaveDocument
        {
            Metadata = document.Metadata,
            WarSaveData = (JsonObject)document.WarSaveData.DeepClone(),
            Variables = variables,
            EntityUpdates = entities
        };
        FilePath = filePath;
        _schema = schema;
        _resolver = resolver;
    }

    public string? GetValue(string fieldId)
    {
        var field = GetFieldOrThrow(fieldId);
        lock (_lock)
            return _resolver.ReadValue(CurrentDocument, field);
    }

    public ValidationResult SetValue(string fieldId, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var field = GetFieldOrThrow(fieldId);
        var originalValue = _resolver.ReadValue(OriginalDocument, field);

        // clearing a field that was never set is a no-op, not a validation error
        if (value.Length == 0 && originalValue is null)
        {
            lock (_lock)
            {
                if (_edits.Remove(fieldId))
                    RebuildCurrentDocument();
            }
            return ValidationResult.Success;
        }

        var validation = ValidateFieldValue(field, value);
        if (!validation.IsValid)
            return validation;

        // normalize both sides so semantically equal values (e.g. "1.0" vs "1E+00") match
        var normalizedValue = NormalizeValue(field, value);
        var normalizedOriginal = originalValue is not null ? NormalizeValue(field, originalValue) : null;

        lock (_lock)
        {
            // if the normalized written value matches the original, remove the edit
            if (normalizedValue == normalizedOriginal)
            {
                _edits.Remove(fieldId);
            }
            else
            {
                _edits[fieldId] = new FieldEdit(fieldId, originalValue, value);
            }

            // apply just this single write to the current document instead of rebuilding from scratch
            CurrentDocument = _resolver.WriteValue(CurrentDocument, field, value);
        }
        return ValidationResult.Success;
    }

    public void RevertField(string fieldId)
    {
        var field = GetFieldOrThrow(fieldId);
        lock (_lock)
        {
            if (_edits.TryGetValue(fieldId, out var edit))
            {
                _edits.Remove(fieldId);
                // write the original value back incrementally instead of full rebuild
                if (edit.OldValue is not null)
                    CurrentDocument = _resolver.WriteValue(CurrentDocument, field, edit.OldValue);
                else
                    RebuildCurrentDocument();
            }
        }
    }

    public void RevertAll()
    {
        // build replacement document before clearing edits so state remains
        // consistent if an exception occurs during construction
        var reverted = new SaveDocument
        {
            Metadata = OriginalDocument.Metadata,
            WarSaveData = (JsonObject)OriginalDocument.WarSaveData.DeepClone(),
            Variables = OriginalDocument.Variables,
            EntityUpdates = OriginalDocument.EntityUpdates
        };
        lock (_lock)
        {
            _edits.Clear();
            CurrentDocument = reverted;
        }
    }

    public bool IsFieldDirty(string fieldId)
    {
        lock (_lock) return _edits.ContainsKey(fieldId);
    }

    public IReadOnlyCollection<FieldEdit> GetDirtyFields()
    {
        lock (_lock) return _edits.Values.ToList();
    }

    public ValidationResult ValidateField(string fieldId, string value)
    {
        var field = GetFieldOrThrow(fieldId);
        return ValidateFieldValue(field, value);
    }

    public ValidationResult ValidateAll()
    {
        List<FieldEdit> snapshot;
        lock (_lock) snapshot = [.. _edits.Values];

        foreach (var edit in snapshot)
        {
            var field = _schema.GetById(edit.FieldId);
            if (field is null)
                return ValidationResult.Failure($"Edit references unknown field '{edit.FieldId}' not found in schema.");
            var validation = ValidateFieldValue(field, edit.NewValue);
            if (!validation.IsValid)
                return validation;
        }
        return ValidationResult.Success;
    }

    // caller must hold _lock
    private void RebuildCurrentDocument()
    {
        var doc = new SaveDocument
        {
            Metadata = OriginalDocument.Metadata,
            WarSaveData = (JsonObject)OriginalDocument.WarSaveData.DeepClone(),
            Variables = OriginalDocument.Variables,
            EntityUpdates = OriginalDocument.EntityUpdates
        };
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

    // normalize value to match what a write-then-read round-trip would produce
    // (e.g. "true" → "True", "007" → "7" for variable/metadata ints)
    // uses TryParse so malformed original data degrades gracefully instead of throwing
    private static string NormalizeValue(FieldDefinition field, string value)
    {
        if (field.Type == FieldType.Bool)
            return bool.TryParse(value, out var b) ? b.ToString() : value;

        if (field.Type == FieldType.Int && field.Source is not FieldSource.EntityUpdate)
            return int.TryParse(value, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var i) ? i.ToString() : value;

        if (field.Type == FieldType.Decimal)
            return double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d)
                ? d.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : value;

        return value;
    }

    private static ValidationResult ValidateFieldValue(FieldDefinition field, string value)
    {
        if (value.Length == 0 && field.Type is not FieldType.String)
            return ValidationResult.Failure("Value is required.");

        return field.Type switch
        {
            FieldType.Bool => ValidateBool(value),
            FieldType.Int => ValidateInt(field, value),
            FieldType.Decimal => ValidateDecimal(field, value),
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

    private static ValidationResult ValidateDecimal(FieldDefinition field, string value)
    {
        if (!double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var doubleValue))
            return ValidationResult.Failure($"'{value}' is not a valid number.");

        if (field.Min.HasValue && doubleValue < field.Min.Value)
            return ValidationResult.Failure($"Value {doubleValue} is below minimum {field.Min.Value}.");

        if (field.Max.HasValue && doubleValue > field.Max.Value)
            return ValidationResult.Failure($"Value {doubleValue} exceeds maximum {field.Max.Value}.");

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
