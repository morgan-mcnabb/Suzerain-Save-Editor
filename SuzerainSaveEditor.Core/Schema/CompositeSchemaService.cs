namespace SuzerainSaveEditor.Core.Schema;

// combines the base schema with dynamically discovered fields into a single service
public sealed class CompositeSchemaService : ISchemaService
{
    private readonly IReadOnlyList<FieldDefinition> _allFields;
    private readonly Dictionary<string, FieldDefinition> _byId;
    private readonly Dictionary<FieldGroup, List<FieldDefinition>> _byGroup;
    private readonly Dictionary<string, string> _searchIndex;

    public CompositeSchemaService(ISchemaService baseSchema, IReadOnlyList<FieldDefinition> discoveredFields)
    {
        ArgumentNullException.ThrowIfNull(baseSchema);
        ArgumentNullException.ThrowIfNull(discoveredFields);

        _allFields = baseSchema.GetAll().Concat(discoveredFields).ToList();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var discoveredDuplicates = new List<string>();
        foreach (var f in discoveredFields)
        {
            if (!seen.Add(f.Id))
                discoveredDuplicates.Add(f.Id);
        }
        if (discoveredDuplicates.Count > 0)
            throw new InvalidOperationException(
                $"Duplicate field IDs within discovered fields: {string.Join(", ", discoveredDuplicates)}");

        // detect duplicate field IDs between base schema and discovered fields
        var baseIds = new HashSet<string>(baseSchema.GetAll().Select(f => f.Id));
        var duplicates = discoveredFields.Where(f => baseIds.Contains(f.Id)).Select(f => f.Id).ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                $"Duplicate field IDs detected between base schema and discovered fields: {string.Join(", ", duplicates)}");

        _byId = _allFields.ToDictionary(f => f.Id);
        _byGroup = _allFields
            .GroupBy(f => f.Group)
            .ToDictionary(g => g.Key, g => g.ToList());
        _searchIndex = FieldSearchIndex.Build(_allFields);
    }

    public IReadOnlyList<FieldDefinition> GetAll() => _allFields;

    public IReadOnlyList<FieldDefinition> GetByGroup(FieldGroup group) =>
        _byGroup.TryGetValue(group, out var list) ? list : [];

    public FieldDefinition? GetById(string id) =>
        _byId.GetValueOrDefault(id);

    public IReadOnlyList<FieldDefinition> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _allFields;

        return FieldSearchIndex.Search(_allFields, _searchIndex, query);
    }
}
