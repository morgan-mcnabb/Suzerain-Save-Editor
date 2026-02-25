using System.Text;
using System.Text.RegularExpressions;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Core.Services;

// discovers unmapped fields in a save document and generates synthetic field definitions
public sealed partial class FieldDiscoveryService(ISchemaService schemaService) : IFieldDiscoveryService
{
    [GeneratedRegex(@"^GameCondition\.Turn(\d{2})_", RegexOptions.Compiled)]
    private static partial Regex TurnPrefixRegex();

    // category abbreviation codes found after the turn prefix
    private static readonly Dictionary<string, string> CategoryNames = new(StringComparer.Ordinal)
    {
        ["LateBrief"] = "Late Brief",
        ["Decision"] = "Decisions",
        ["Personal"] = "Personal",
        ["FPnT"] = "Foreign Policy & Treaties",
        ["Bill"] = "Legislation",
        ["EnT"] = "Economy & Trade",
        ["InT"] = "Internal Affairs",
        ["SnO"] = "Security & Order",
        ["A"] = "General"
    };

    // sorted longest-first to avoid ambiguous prefix matches
    private static readonly string[] CategoryPrefixes = CategoryNames.Keys
        .OrderByDescending(k => k.Length)
        .ToArray();

    public IReadOnlyList<FieldDefinition> DiscoverFields(SaveDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var mappedPaths = new HashSet<string>(
            schemaService.GetAll().Select(f => f.Path),
            StringComparer.Ordinal);

        var discovered = new List<FieldDefinition>();
        var emittedPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var variable in document.Variables)
        {
            var path = $"variable:{variable.Key}";
            if (mappedPaths.Contains(path) || !emittedPaths.Add(path))
                continue;

            var (label, description) = GenerateAdvancedLabelAndDescription(variable.Key);
            discovered.Add(new FieldDefinition
            {
                Id = $"discovered.var.{variable.Key}",
                Path = path,
                Label = label,
                Group = FieldGroup.Advanced,
                Type = InferFieldType(variable.Value),
                Source = FieldSource.Variable,
                Description = description
            });
        }

        foreach (var entity in document.EntityUpdates)
        {
            var path = $"entity:{entity.NameInDatabase}.{entity.FieldName}";
            if (mappedPaths.Contains(path) || !emittedPaths.Add(path))
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

    // generates a human-readable label and rich description for discovered variables
    internal static (string Label, string Description) GenerateAdvancedLabelAndDescription(string key)
    {
        var turnMatch = TurnPrefixRegex().Match(key);
        if (turnMatch.Success)
        {
            var turnNum = turnMatch.Groups[1].Value;
            var afterTurn = key[turnMatch.Length..];

            string? categoryLabel = null;
            var eventPart = afterTurn;

            foreach (var prefix in CategoryPrefixes)
            {
                if (afterTurn.StartsWith(prefix + "_", StringComparison.Ordinal))
                {
                    categoryLabel = CategoryNames[prefix];
                    eventPart = afterTurn[(prefix.Length + 1)..];
                    break;
                }
            }

            var label = GenerateLabel(eventPart);
            var descParts = $"Turn {turnNum}";
            if (categoryLabel is not null)
                descParts += $" > {categoryLabel}";
            var description = $"{descParts} | {key}";

            return (label, description);
        }

        // non-turn variable: strip namespace prefix for label
        // for dot-namespaced keys, GenerateLabel already strips the namespace
        // for underscore-namespaced keys (e.g. Opinion_OldGuard), strip the first segment
        var lastDot = key.LastIndexOf('.');
        if (lastDot < 0)
        {
            var firstUnderscore = key.IndexOf('_');
            if (firstUnderscore >= 0)
            {
                var afterPrefix = key[(firstUnderscore + 1)..];
                var label = GenerateLabel(afterPrefix);
                return (label, $"Variable: {key}");
            }
        }

        var generatedLabel = GenerateLabel(key);
        return (generatedLabel, $"Variable: {key}");
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
