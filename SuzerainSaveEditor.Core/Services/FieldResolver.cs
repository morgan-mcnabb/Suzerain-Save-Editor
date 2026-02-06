using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Core.Services;

// resolves field values from a save document using path conventions:
//   variable:BaseGame.GovernmentBudget
//   entity:Economy_Budget.ProgressPercentage
//   meta:campaignName
public sealed class FieldResolver : IFieldResolver
{
    public string? ReadValue(SaveDocument document, FieldDefinition field)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(field);

        var raw = field.Source switch
        {
            FieldSource.Variable => ReadVariable(document, field.Path),
            FieldSource.EntityUpdate => ReadEntityUpdate(document, field.Path),
            FieldSource.Metadata => ReadMetadata(document, field.Path),
            _ => throw new ArgumentOutOfRangeException(nameof(field), $"Unknown field source: {field.Source}")
        };

        // normalize bool values to consistent casing regardless of source
        if (raw is not null && field.Type == FieldType.Bool && bool.TryParse(raw, out var b))
            return b.ToString();

        return raw;
    }

    public SaveDocument WriteValue(SaveDocument document, FieldDefinition field, string value)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);

        return field.Source switch
        {
            FieldSource.Variable => WriteVariable(document, field, value),
            FieldSource.EntityUpdate => WriteEntityUpdate(document, field.Path, value),
            FieldSource.Metadata => WriteMetadata(document, field.Path, value),
            _ => throw new ArgumentOutOfRangeException(nameof(field), $"Unknown field source: {field.Source}")
        };
    }

    private static string? ReadVariable(SaveDocument document, string path)
    {
        var key = StripPrefix(path, "variable:");
        var variable = document.Variables.FirstOrDefault(v => v.Key == key);
        return variable?.Value switch
        {
            LuaValue.Bool b => b.Value.ToString(),
            LuaValue.Int i => i.Value.ToString(),
            LuaValue.Str s => s.Value,
            null => null,
            _ => null
        };
    }

    private static SaveDocument WriteVariable(SaveDocument document, FieldDefinition field, string value)
    {
        var key = StripPrefix(field.Path, "variable:");
        var luaValue = ConvertToLuaValue(field.Type, value);

        if (!document.Variables.Any(v => v.Key == key))
            throw new KeyNotFoundException($"Variable '{key}' not found in save document.");

        var newVariables = document.Variables.Select(v =>
            v.Key == key ? new LuaVariable(key, luaValue) : v).ToList();

        return new SaveDocument
        {
            Metadata = document.Metadata,
            WarSaveData = document.WarSaveData,
            Variables = newVariables,
            EntityUpdates = document.EntityUpdates
        };
    }

    private static string? ReadEntityUpdate(SaveDocument document, string path)
    {
        var (nameInDatabase, fieldName) = ParseEntityPath(path);
        var entity = document.EntityUpdates.FirstOrDefault(e =>
            e.NameInDatabase == nameInDatabase && e.FieldName == fieldName);
        return entity?.FieldValue;
    }

    private static SaveDocument WriteEntityUpdate(SaveDocument document, string path, string value)
    {
        var (nameInDatabase, fieldName) = ParseEntityPath(path);

        if (!document.EntityUpdates.Any(e => e.NameInDatabase == nameInDatabase && e.FieldName == fieldName))
            throw new KeyNotFoundException($"Entity update '{nameInDatabase}.{fieldName}' not found in save document.");

        var newUpdates = document.EntityUpdates.Select(e =>
            e.NameInDatabase == nameInDatabase && e.FieldName == fieldName
                ? new EntityUpdate(nameInDatabase, fieldName, value)
                : e).ToList();

        return new SaveDocument
        {
            Metadata = document.Metadata,
            WarSaveData = document.WarSaveData,
            Variables = document.Variables,
            EntityUpdates = newUpdates
        };
    }

    private static string? ReadMetadata(SaveDocument document, string path)
    {
        var property = StripPrefix(path, "meta:");
        return property switch
        {
            "saveFileType" => document.Metadata.SaveFileType.ToString(),
            "campaignName" => document.Metadata.CampaignName,
            "currentStoryPack" => document.Metadata.CurrentStoryPack,
            "turnNo" => document.Metadata.TurnNo.ToString(),
            "saveFileName" => document.Metadata.SaveFileName,
            "sceneBuildIndex" => document.Metadata.SceneBuildIndex.ToString(),
            "lastModified" => document.Metadata.LastModified,
            "version" => document.Metadata.Version,
            "isVersionMismatched" => document.Metadata.IsVersionMismatched.ToString(),
            "isTorporModeOn" => document.Metadata.IsTorporModeOn.ToString(),
            "notes" => document.Metadata.Notes,
            _ => null
        };
    }

    private static SaveDocument WriteMetadata(SaveDocument document, string path, string value)
    {
        var property = StripPrefix(path, "meta:");
        var meta = document.Metadata;

        var newMeta = property switch
        {
            "saveFileType" => meta with { SaveFileType = int.Parse(value) },
            "campaignName" => meta with { CampaignName = value },
            "currentStoryPack" => meta with { CurrentStoryPack = value },
            "turnNo" => meta with { TurnNo = int.Parse(value) },
            "saveFileName" => meta with { SaveFileName = value },
            "sceneBuildIndex" => meta with { SceneBuildIndex = int.Parse(value) },
            "lastModified" => meta with { LastModified = value },
            "version" => meta with { Version = value },
            "isVersionMismatched" => meta with { IsVersionMismatched = bool.Parse(value) },
            "isTorporModeOn" => meta with { IsTorporModeOn = bool.Parse(value) },
            "notes" => meta with { Notes = value },
            _ => throw new ArgumentException($"Unknown metadata property: {property}")
        };

        return new SaveDocument
        {
            Metadata = newMeta,
            WarSaveData = document.WarSaveData,
            Variables = document.Variables,
            EntityUpdates = document.EntityUpdates
        };
    }

    private static LuaValue ConvertToLuaValue(FieldType type, string value) => type switch
    {
        FieldType.Bool => new LuaValue.Bool(bool.Parse(value)),
        FieldType.Int => new LuaValue.Int(int.Parse(value)),
        FieldType.String or FieldType.Enum => new LuaValue.Str(value),
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown field type: {type}")
    };

    private static string StripPrefix(string path, string prefix)
    {
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
            throw new ArgumentException($"Path '{path}' does not start with expected prefix '{prefix}'.");
        return path[prefix.Length..];
    }

    // parses entity:NameInDatabase.FieldName — splits on last dot
    private static (string NameInDatabase, string FieldName) ParseEntityPath(string path)
    {
        var key = StripPrefix(path, "entity:");
        var lastDot = key.LastIndexOf('.');
        if (lastDot < 0)
            throw new ArgumentException($"Entity path '{path}' must contain a dot separator.");
        return (key[..lastDot], key[(lastDot + 1)..]);
    }
}
