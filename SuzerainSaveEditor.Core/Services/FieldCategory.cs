using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Core.Services;

// represents a node in the hierarchical field grouping tree
public sealed record FieldCategory(
    string Key,
    string Label,
    int SortOrder,
    IReadOnlyList<FieldDefinition> Fields,
    IReadOnlyList<FieldCategory> Children);
