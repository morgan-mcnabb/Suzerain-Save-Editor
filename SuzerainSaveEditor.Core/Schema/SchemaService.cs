using System.Reflection;
using System.Text.Json;

namespace SuzerainSaveEditor.Core.Schema;

// loads field definitions from the embedded schema.json resource
public sealed class SchemaService : ISchemaService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IReadOnlyList<FieldDefinition> _fields;
    private readonly Dictionary<string, FieldDefinition> _byId;
    private readonly Dictionary<FieldGroup, List<FieldDefinition>> _byGroup;

    public SchemaService()
    {
        _fields = LoadEmbeddedSchema();
        _byId = _fields.ToDictionary(f => f.Id);
        _byGroup = _fields
            .GroupBy(f => f.Group)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public IReadOnlyList<FieldDefinition> GetAll() => _fields;

    public IReadOnlyList<FieldDefinition> GetByGroup(FieldGroup group) =>
        _byGroup.TryGetValue(group, out var list) ? list : [];

    public FieldDefinition? GetById(string id) =>
        _byId.GetValueOrDefault(id);

    public IReadOnlyList<FieldDefinition> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _fields;

        return _fields
            .Where(f => f.Label.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || f.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || (f.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    private static IReadOnlyList<FieldDefinition> LoadEmbeddedSchema()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("schema.json", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Embedded schema.json resource not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        return JsonSerializer.Deserialize<List<FieldDefinition>>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize schema.json.");
    }
}
