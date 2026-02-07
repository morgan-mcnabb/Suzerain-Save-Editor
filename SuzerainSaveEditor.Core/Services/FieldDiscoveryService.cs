using System.Text;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Core.Services;

// discovers unmapped fields in a save document and generates synthetic field definitions
public sealed class FieldDiscoveryService(ISchemaService schemaService) : IFieldDiscoveryService
{
    public IReadOnlyList<FieldDefinition> DiscoverFields(SaveDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var mappedPaths = new HashSet<string>(
            schemaService.GetAll().Select(f => f.Path),
            StringComparer.Ordinal);

        var discovered = new List<FieldDefinition>();

        foreach (var variable in document.Variables)
        {
            var path = $"variable:{variable.Key}";
            if (mappedPaths.Contains(path))
                continue;

            discovered.Add(new FieldDefinition
            {
                Id = $"discovered.var.{variable.Key}",
                Path = path,
                Label = GenerateLabel(variable.Key),
                Group = FieldGroup.Advanced,
                Type = InferFieldType(variable.Value),
                Source = FieldSource.Variable,
                Description = $"Variable: {variable.Key}"
            });
        }

        foreach (var entity in document.EntityUpdates)
        {
            var path = $"entity:{entity.NameInDatabase}.{entity.FieldName}";
            if (mappedPaths.Contains(path))
                continue;

            discovered.Add(new FieldDefinition
            {
                Id = $"discovered.entity.{entity.NameInDatabase}.{entity.FieldName}",
                Path = path,
                Label = GenerateLabel(entity.FieldName),
                Group = FieldGroup.Advanced,
                Type = FieldType.String,
                Source = FieldSource.EntityUpdate,
                Description = $"Entity: {entity.NameInDatabase}.{entity.FieldName}"
            });
        }

        return discovered;
    }

    // strips namespace prefix (text before last dot), replaces underscores
    // with spaces, and inserts spaces before PascalCase transitions
    internal static string GenerateLabel(string key)
    {
        // strip namespace prefix (everything up to and including the last dot)
        var lastDot = key.LastIndexOf('.');
        var name = lastDot >= 0 ? key[(lastDot + 1)..] : key;

        // replace underscores with spaces
        name = name.Replace('_', ' ');

        // insert spaces before PascalCase transitions
        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && name[i - 1] != ' ')
            {
                // insert space before uppercase letter following a lowercase or digit
                if (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]))
                    sb.Append(' ');
                // insert space before uppercase followed by lowercase (e.g. "USPVote" → "USP Vote")
                else if (i + 1 < name.Length && char.IsLower(name[i + 1]))
                    sb.Append(' ');
            }
            sb.Append(c);
        }

        return sb.ToString();
    }

    internal static FieldType InferFieldType(LuaValue value) => value switch
    {
        LuaValue.Bool => FieldType.Bool,
        LuaValue.Int => FieldType.Int,
        LuaValue.Num => FieldType.Decimal,
        LuaValue.Str => FieldType.String,
        _ => FieldType.String
    };
}
