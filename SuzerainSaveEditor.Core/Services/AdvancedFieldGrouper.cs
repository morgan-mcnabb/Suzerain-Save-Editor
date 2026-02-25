using System.Text.RegularExpressions;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Core.Services;

// groups advanced (discovered) field definitions into a hierarchical tree
public static partial class AdvancedFieldGrouper
{
    [GeneratedRegex(@"^GameCondition\.Turn(\d{2})_", RegexOptions.Compiled)]
    private static partial Regex TurnPatternGenerated();

    [GeneratedRegex(@"^Turn\d{2}$", RegexOptions.Compiled)]
    private static partial Regex TurnSubCategoryPatternGenerated();

    private static readonly Regex TurnPatternRegex = TurnPatternGenerated();
    private static readonly Regex TurnSubCategoryRegex = TurnSubCategoryPatternGenerated();

    // minimum fields before a namespace gets sub-categorized
    private const int SubCategoryThreshold = 30;

    private static readonly Dictionary<string, string> NamespaceLabels = new(StringComparer.Ordinal)
    {
        ["BaseGame"] = "Base Game",
        ["BaseGameSetup"] = "Base Game Setup",
        ["BaseGameSupport"] = "Base Game Support",
        ["BaseGameIsolated"] = "Base Game Isolated",
        ["BaseGameUI"] = "Base Game UI",
        ["BaseGameText"] = "Base Game Text",
        ["Turns"] = "Turns",
        ["GameCondition"] = "Game Condition",
        ["RiziaDLC"] = "Rizia DLC",
        ["RiziaDLCSetup"] = "Rizia DLC Setup",
        ["RiziaDLCText"] = "Rizia DLC Text",
        ["RiziaDLCSupport"] = "Rizia DLC Support",
        ["RiziaDLCIsolated"] = "Rizia DLC Isolated",
        ["RiziaDLCUI"] = "Rizia DLC UI",
        ["SharedSupport"] = "Shared Support",
        ["Opinion"] = "Opinions",
        ["Relations"] = "Relations",
        ["entity:Position"] = "Entity: Positions",
        ["entity:CharacterCustomization"] = "Entity: Character Customization",
        ["entity:Rizia"] = "Entity: Rizia",
        ["entity:Page"] = "Entity: Page"
    };

    // namespace sort order — story-relevant namespaces first, support/meta last
    private static readonly Dictionary<string, int> NamespaceSortOrder = new(StringComparer.Ordinal)
    {
        ["BaseGame"] = 10,
        ["BaseGameIsolated"] = 11,
        ["BaseGameSupport"] = 12,
        ["BaseGameSetup"] = 13,
        ["BaseGameUI"] = 14,
        ["BaseGameText"] = 15,
        ["RiziaDLC"] = 20,
        ["RiziaDLCIsolated"] = 21,
        ["RiziaDLCSupport"] = 22,
        ["RiziaDLCSetup"] = 23,
        ["RiziaDLCUI"] = 24,
        ["RiziaDLCText"] = 25,
        ["SharedSupport"] = 30,
        ["GameCondition"] = 35,
        ["Turns"] = 40
    };

    // classifies a field into namespace + sub-category
    public static (string Namespace, string? SubCategory, string NamespaceLabel, string? SubCategoryLabel)
        ClassifyField(FieldDefinition field)
    {
        var key = ExtractKey(field);

        // turn-prefixed: GameCondition.TurnXX_... → unified Turns namespace
        var turnMatch = TurnPatternRegex.Match(key);
        if (turnMatch.Success)
        {
            var turnNum = turnMatch.Groups[1].Value;
            return ("Turns", $"Turn{turnNum}", "Turns", $"Turn {int.Parse(turnNum)}");
        }

        // entity path — group by first underscore prefix of nameInDatabase
        if (field.Path.StartsWith("entity:", StringComparison.Ordinal))
        {
            var entityKey = field.Path["entity:".Length..];
            var lastDot = entityKey.LastIndexOf('.');
            var nameInDb = lastDot >= 0 ? entityKey[..lastDot] : entityKey;

            var entityUs = nameInDb.IndexOf('_');
            if (entityUs >= 0)
            {
                var prefix = nameInDb[..entityUs];

                // TurnXX entities → unified Turns namespace
                if (TurnSubCategoryRegex.IsMatch(prefix))
                {
                    var turnNum = prefix[4..]; // "01" from "Turn01"
                    return ("Turns", $"Turn{turnNum}", "Turns", $"Turn {int.Parse(turnNum)}");
                }

                // non-turn entities → group by prefix
                var nsKey = $"entity:{prefix}";
                var nsLabel = NamespaceLabels.GetValueOrDefault(nsKey)
                              ?? $"Entity: {FieldDiscoveryService.GenerateLabel(prefix)}";
                var remainder = nameInDb[(entityUs + 1)..];
                var subLabel = FieldDiscoveryService.GenerateLabel(remainder);
                return (nsKey, nameInDb, nsLabel, subLabel);
            }

            // no underscore — standalone entity namespace
            var label = FieldDiscoveryService.GenerateLabel(nameInDb);
            return ($"entity:{nameInDb}", null, $"Entity: {label}", null);
        }

        // dot-namespaced: Namespace.SubCategory_Detail_...
        var firstDot = key.IndexOf('.');
        if (firstDot >= 0)
        {
            var ns = key[..firstDot];
            var afterDot = key[(firstDot + 1)..];
            var nsLabel = NamespaceLabels.GetValueOrDefault(ns)
                          ?? FieldDiscoveryService.GenerateLabel(ns);

            // extract sub-category: first underscore segment, normalize TurnXX → Turns
            var firstUnderscore = afterDot.IndexOf('_');
            if (firstUnderscore >= 0)
            {
                var subCat = afterDot[..firstUnderscore];
                if (TurnSubCategoryRegex.IsMatch(subCat))
                {
                    var turnNum = subCat[4..]; // "01" from "Turn01"
                    return ("Turns", $"Turn{turnNum}", "Turns", $"Turn {int.Parse(turnNum)}");
                }
                var subLabel = FieldDiscoveryService.GenerateLabel(subCat);
                return (ns, subCat, nsLabel, subLabel);
            }

            // no underscore — standalone variable (e.g. BaseGame.GovernmentBudget)
            return (ns, null, nsLabel, null);
        }

        // underscore-namespaced (e.g. Opinion_OldGuard)
        var firstUs = key.IndexOf('_');
        if (firstUs >= 0)
        {
            var ns = key[..firstUs];
            var nsLabel = NamespaceLabels.GetValueOrDefault(ns)
                          ?? FieldDiscoveryService.GenerateLabel(ns);
            return (ns, null, nsLabel, null);
        }

        // fallback
        return ("Other", null, "Other", null);
    }

    // groups fields into a hierarchical tree of FieldCategory nodes
    public static IReadOnlyList<FieldCategory> GroupFieldsHierarchical(IReadOnlyList<FieldDefinition> fields)
    {
        // first pass: classify all fields into (namespace, subCategory) buckets
        var namespaces = new Dictionary<string, NamespaceBucket>(StringComparer.Ordinal);

        foreach (var field in fields)
        {
            var (ns, subCat, nsLabel, subLabel) = ClassifyField(field);

            if (!namespaces.TryGetValue(ns, out var bucket))
            {
                bucket = new NamespaceBucket(nsLabel);
                namespaces[ns] = bucket;
            }

            if (subCat is not null && subLabel is not null)
            {
                if (!bucket.SubCategories.TryGetValue(subCat, out var subBucket))
                {
                    subBucket = new SubCategoryBucket(subLabel);
                    bucket.SubCategories[subCat] = subBucket;
                }
                subBucket.Fields.Add(field);
            }
            else
            {
                bucket.UncategorizedFields.Add(field);
            }
        }

        // second pass: build tree, deciding whether each namespace gets children
        var result = new List<FieldCategory>();

        foreach (var (nsKey, bucket) in namespaces)
        {
            var totalFields = bucket.UncategorizedFields.Count
                              + bucket.SubCategories.Values.Sum(s => s.Fields.Count);
            var sortOrder = NamespaceSortOrder.GetValueOrDefault(nsKey, 500);

            // if the namespace is small or has no sub-categories, keep it flat
            if (totalFields <= SubCategoryThreshold || bucket.SubCategories.Count == 0)
            {
                var allFields = new List<FieldDefinition>(bucket.UncategorizedFields);
                foreach (var sub in bucket.SubCategories.Values)
                    allFields.AddRange(sub.Fields);

                result.Add(new FieldCategory(nsKey, bucket.Label, sortOrder, allFields, []));
                continue;
            }

            // build sub-category children
            // turns: sort chronologically by key; others: sort by count descending
            var children = new List<FieldCategory>();
            var childSortIndex = 0;
            var orderedSubs = nsKey == "Turns"
                ? bucket.SubCategories.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                : bucket.SubCategories.OrderByDescending(kvp => kvp.Value.Fields.Count);

            foreach (var (subKey, subBucket) in orderedSubs)
            {
                // for Turns: add 3rd-level source grouping when multiple sources exist
                if (nsKey == "Turns")
                {
                    var sourceGroups = subBucket.Fields
                        .GroupBy(f => DeriveSourceKey(f))
                        .ToList();

                    if (sourceGroups.Count > 1)
                    {
                        var grandChildren = sourceGroups
                            .OrderBy(g => NamespaceSortOrder.GetValueOrDefault(g.Key, 500))
                            .ThenBy(g => g.Key, StringComparer.Ordinal)
                            .Select((g, idx) =>
                            {
                                var sourceLabel = NamespaceLabels.GetValueOrDefault(g.Key)
                                                  ?? FieldDiscoveryService.GenerateLabel(g.Key);
                                return new FieldCategory(
                                    $"Turns.{subKey}.{g.Key}",
                                    sourceLabel,
                                    idx,
                                    g.ToList(),
                                    []);
                            })
                            .ToList();

                        children.Add(new FieldCategory(
                            $"{nsKey}.{subKey}",
                            subBucket.Label,
                            childSortIndex++,
                            [],
                            grandChildren));
                        continue;
                    }
                }

                children.Add(new FieldCategory(
                    $"{nsKey}.{subKey}",
                    subBucket.Label,
                    childSortIndex++,
                    subBucket.Fields,
                    []));
            }

            // uncategorized fields go into an "Other" child if sub-categories exist
            if (bucket.UncategorizedFields.Count > 0)
            {
                children.Add(new FieldCategory(
                    $"{nsKey}._other",
                    "Other",
                    childSortIndex,
                    bucket.UncategorizedFields,
                    []));
            }

            result.Add(new FieldCategory(nsKey, bucket.Label, sortOrder, [], children));
        }

        return result.OrderBy(c => c.SortOrder).ThenBy(c => c.Key, StringComparer.Ordinal).ToList();
    }

    // normalizes related namespaces into a single source group for turn sub-categories
    private static readonly Dictionary<string, string> SourceKeyNormalization = new(StringComparer.Ordinal)
    {
        ["BaseGameIsolated"] = "BaseGame",
        ["BaseGameSetup"] = "BaseGame",
        ["BaseGameSupport"] = "BaseGame",
        ["BaseGameUI"] = "BaseGame",
        ["BaseGameText"] = "BaseGame",
        ["RiziaDLCIsolated"] = "RiziaDLC",
        ["RiziaDLCSetup"] = "RiziaDLC",
        ["RiziaDLCSupport"] = "RiziaDLC",
        ["RiziaDLCUI"] = "RiziaDLC",
        ["RiziaDLCText"] = "RiziaDLC"
    };

    // derives the original namespace key for source grouping within turns
    private static string DeriveSourceKey(FieldDefinition field)
    {
        if (field.Path.StartsWith("entity:", StringComparison.Ordinal))
            return "Entities";

        var key = ExtractKey(field);
        var dot = key.IndexOf('.');
        var rawKey = dot >= 0 ? key[..dot] : "Other";
        return SourceKeyNormalization.GetValueOrDefault(rawKey, rawKey);
    }

    private static string ExtractKey(FieldDefinition field)
    {
        if (field.Path.StartsWith("variable:", StringComparison.Ordinal))
            return field.Path["variable:".Length..];
        if (field.Path.StartsWith("entity:", StringComparison.Ordinal))
            return field.Path["entity:".Length..];
        return field.Id;
    }

    private sealed class NamespaceBucket(string label)
    {
        public string Label { get; } = label;
        public Dictionary<string, SubCategoryBucket> SubCategories { get; } = new(StringComparer.Ordinal);
        public List<FieldDefinition> UncategorizedFields { get; } = [];
    }

    private sealed class SubCategoryBucket(string label)
    {
        public string Label { get; } = label;
        public List<FieldDefinition> Fields { get; } = [];
    }
}
