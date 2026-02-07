namespace SuzerainSaveEditor.Core.Schema;

// combines the base schema with dynamically discovered fields into a single service
public sealed class CompositeSchemaService : ISchemaService
{
    private readonly IReadOnlyList<FieldDefinition> _allFields;
    private readonly Dictionary<string, FieldDefinition> _byId;
    private readonly Dictionary<FieldGroup, List<FieldDefinition>> _byGroup;

    public CompositeSchemaService(ISchemaService baseSchema, IReadOnlyList<FieldDefinition> discoveredFields)
    {
        ArgumentNullException.ThrowIfNull(baseSchema);
        ArgumentNullException.ThrowIfNull(discoveredFields);

        _allFields = baseSchema.GetAll().Concat(discoveredFields).ToList();
        _byId = _allFields.ToDictionary(f => f.Id);
        _byGroup = _allFields
            .GroupBy(f => f.Group)
            .ToDictionary(g => g.Key, g => g.ToList());
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

        return _allFields
            .Where(f => f.Label.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || f.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || (f.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }
}
