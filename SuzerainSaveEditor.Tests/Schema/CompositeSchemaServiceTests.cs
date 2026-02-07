using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Tests.Schema;

public sealed class CompositeSchemaServiceTests
{
    private readonly ISchemaService _baseSchema = new SchemaService();

    private static readonly FieldDefinition DiscoveredBool = new()
    {
        Id = "discovered.var.Custom.Flag",
        Path = "variable:Custom.Flag",
        Label = "Flag",
        Group = FieldGroup.Advanced,
        Type = FieldType.Bool,
        Source = FieldSource.Variable,
        Description = "Variable: Custom.Flag"
    };

    private static readonly FieldDefinition DiscoveredEntity = new()
    {
        Id = "discovered.entity.Custom_Ent.Score",
        Path = "entity:Custom_Ent.Score",
        Label = "Score",
        Group = FieldGroup.Advanced,
        Type = FieldType.String,
        Source = FieldSource.EntityUpdate,
        Description = "Entity: Custom_Ent.Score"
    };

    private static readonly IReadOnlyList<FieldDefinition> DiscoveredFields = [DiscoveredBool, DiscoveredEntity];

    private CompositeSchemaService CreateComposite(IReadOnlyList<FieldDefinition>? discovered = null) =>
        new(_baseSchema, discovered ?? DiscoveredFields);

    [Fact]
    public void GetAll_IncludesBothSchemaAndDiscovered()
    {
        var composite = CreateComposite();

        var all = composite.GetAll();

        Assert.Equal(_baseSchema.GetAll().Count + 2, all.Count);
    }

    [Fact]
    public void GetById_FindsSchemaField()
    {
        var composite = CreateComposite();
        var schemaField = _baseSchema.GetAll()[0];

        var found = composite.GetById(schemaField.Id);

        Assert.NotNull(found);
        Assert.Equal(schemaField.Id, found.Id);
    }

    [Fact]
    public void GetById_FindsDiscoveredField()
    {
        var composite = CreateComposite();

        var found = composite.GetById("discovered.var.Custom.Flag");

        Assert.NotNull(found);
        Assert.Equal(DiscoveredBool.Id, found.Id);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var composite = CreateComposite();

        Assert.Null(composite.GetById("nonexistent.field"));
    }

    [Fact]
    public void GetByGroup_Advanced_ReturnsOnlyDiscovered()
    {
        var composite = CreateComposite();

        var advanced = composite.GetByGroup(FieldGroup.Advanced);

        Assert.Equal(2, advanced.Count);
        Assert.All(advanced, f => Assert.Equal(FieldGroup.Advanced, f.Group));
    }

    [Fact]
    public void GetByGroup_Sordland_ReturnsOnlySchemaFields()
    {
        var composite = CreateComposite();

        var sordland = composite.GetByGroup(FieldGroup.Sordland);
        var baseSordland = _baseSchema.GetByGroup(FieldGroup.Sordland);

        Assert.Equal(baseSordland.Count, sordland.Count);
        Assert.All(sordland, f => Assert.Equal(FieldGroup.Sordland, f.Group));
    }

    [Fact]
    public void GetByGroup_Empty_ReturnsEmptyList()
    {
        // composite with no discovered fields — Advanced group should be empty
        var composite = CreateComposite(discovered: []);

        var advanced = composite.GetByGroup(FieldGroup.Advanced);

        Assert.Empty(advanced);
    }

    [Fact]
    public void Search_MatchesDiscoveredFieldLabel()
    {
        var composite = CreateComposite();

        var results = composite.Search("Flag");

        Assert.Contains(results, f => f.Id == DiscoveredBool.Id);
    }

    [Fact]
    public void Search_MatchesDiscoveredFieldDescription()
    {
        var composite = CreateComposite();

        var results = composite.Search("Custom_Ent");

        Assert.Contains(results, f => f.Id == DiscoveredEntity.Id);
    }

    [Fact]
    public void Search_MatchesDiscoveredFieldId()
    {
        var composite = CreateComposite();

        var results = composite.Search("discovered.var.Custom.Flag");

        Assert.Contains(results, f => f.Id == DiscoveredBool.Id);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        var composite = CreateComposite();

        var results = composite.Search("");

        Assert.Equal(composite.GetAll().Count, results.Count);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var composite = CreateComposite();

        var results = composite.Search("zzz_no_match_zzz");

        Assert.Empty(results);
    }

    [Fact]
    public void Search_CaseInsensitive()
    {
        var composite = CreateComposite();

        var results = composite.Search("flag");

        Assert.Contains(results, f => f.Id == DiscoveredBool.Id);
    }

    [Fact]
    public void Constructor_NullBaseSchema_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompositeSchemaService(null!, DiscoveredFields));
    }

    [Fact]
    public void Constructor_NullDiscoveredFields_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompositeSchemaService(_baseSchema, null!));
    }
}
