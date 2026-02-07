using System.Text.Json.Nodes;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class FieldDiscoveryServiceTests
{
    private readonly ISchemaService _schema = new SchemaService();
    private readonly FieldDiscoveryService _service;

    public FieldDiscoveryServiceTests()
    {
        _service = new FieldDiscoveryService(_schema);
    }

    private static SaveDocument CreateDocument(
        IReadOnlyList<LuaVariable>? variables = null,
        IReadOnlyList<EntityUpdate>? entities = null) => new()
    {
        Metadata = new SaveMetadata(
            SaveFileType: 1,
            CampaignName: "Test",
            CurrentStoryPack: "test",
            TurnNo: 1,
            SaveFileName: "test.json",
            SceneBuildIndex: 0,
            LastModified: "2024-01-01",
            Version: "1.0",
            IsVersionMismatched: false,
            IsTorporModeOn: false,
            Notes: ""),
        WarSaveData = new JsonObject(),
        Variables = variables ?? [],
        EntityUpdates = entities ?? []
    };

    [Fact]
    public void DiscoverFields_UnmappedVariable_CreatesSyntheticField()
    {
        var doc = CreateDocument(variables: [
            new LuaVariable("Custom.MyTestVar", new LuaValue.Bool(true))
        ]);

        var discovered = _service.DiscoverFields(doc);

        Assert.Single(discovered);
        var field = discovered[0];
        Assert.Equal("discovered.var.Custom.MyTestVar", field.Id);
        Assert.Equal("variable:Custom.MyTestVar", field.Path);
        Assert.Equal(FieldGroup.Advanced, field.Group);
        Assert.Equal(FieldType.Bool, field.Type);
        Assert.Equal(FieldSource.Variable, field.Source);
        Assert.Equal("Variable: Custom.MyTestVar", field.Description);
    }

    [Fact]
    public void DiscoverFields_MappedVariable_IsExcluded()
    {
        // use a variable that exists in the real schema
        var schemaField = _schema.GetAll().First(f => f.Source == FieldSource.Variable);
        var key = schemaField.Path["variable:".Length..];

        var doc = CreateDocument(variables: [
            new LuaVariable(key, new LuaValue.Bool(true))
        ]);

        var discovered = _service.DiscoverFields(doc);

        Assert.Empty(discovered);
    }

    [Fact]
    public void DiscoverFields_MixOfMappedAndUnmapped_OnlyReturnsUnmapped()
    {
        var schemaField = _schema.GetAll().First(f => f.Source == FieldSource.Variable);
        var mappedKey = schemaField.Path["variable:".Length..];

        var doc = CreateDocument(variables: [
            new LuaVariable(mappedKey, new LuaValue.Bool(true)),
            new LuaVariable("Custom.Unmapped1", new LuaValue.Int(42)),
            new LuaVariable("Custom.Unmapped2", new LuaValue.Str("hello"))
        ]);

        var discovered = _service.DiscoverFields(doc);

        Assert.Equal(2, discovered.Count);
        Assert.All(discovered, f => Assert.StartsWith("discovered.var.", f.Id));
    }

    [Fact]
    public void DiscoverFields_UnmappedEntity_CreatesSyntheticField()
    {
        var doc = CreateDocument(entities: [
            new EntityUpdate("Custom_Entity", "TestField", "someValue")
        ]);

        var discovered = _service.DiscoverFields(doc);

        Assert.Single(discovered);
        var field = discovered[0];
        Assert.Equal("discovered.entity.Custom_Entity.TestField", field.Id);
        Assert.Equal("entity:Custom_Entity.TestField", field.Path);
        Assert.Equal(FieldGroup.Advanced, field.Group);
        Assert.Equal(FieldType.String, field.Type);
        Assert.Equal(FieldSource.EntityUpdate, field.Source);
        Assert.Equal("Entity: Custom_Entity.TestField", field.Description);
    }

    [Fact]
    public void DiscoverFields_MappedEntity_IsExcluded()
    {
        var schemaField = _schema.GetAll().FirstOrDefault(f => f.Source == FieldSource.EntityUpdate);
        if (schemaField is null) return; // skip if no entity fields in schema

        var path = schemaField.Path["entity:".Length..];
        var lastDot = path.LastIndexOf('.');
        var name = path[..lastDot];
        var fieldName = path[(lastDot + 1)..];

        var doc = CreateDocument(entities: [
            new EntityUpdate(name, fieldName, "test")
        ]);

        var discovered = _service.DiscoverFields(doc);

        Assert.Empty(discovered);
    }

    [Fact]
    public void DiscoverFields_AllFieldsHaveGroupAdvanced()
    {
        var doc = CreateDocument(
            variables: [
                new LuaVariable("Custom.Var1", new LuaValue.Bool(true)),
                new LuaVariable("Custom.Var2", new LuaValue.Int(5))
            ],
            entities: [
                new EntityUpdate("Custom_Ent", "Field1", "val")
            ]);

        var discovered = _service.DiscoverFields(doc);

        Assert.Equal(3, discovered.Count);
        Assert.All(discovered, f => Assert.Equal(FieldGroup.Advanced, f.Group));
    }

    [Fact]
    public void DiscoverFields_EmptyDocument_ReturnsEmpty()
    {
        var doc = CreateDocument();

        var discovered = _service.DiscoverFields(doc);

        Assert.Empty(discovered);
    }

    [Fact]
    public void DiscoverFields_NullDocument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _service.DiscoverFields(null!));
    }

    // InferFieldType tests

    [Fact]
    public void InferFieldType_Bool_ReturnsBool()
    {
        Assert.Equal(FieldType.Bool, FieldDiscoveryService.InferFieldType(new LuaValue.Bool(true)));
    }

    [Fact]
    public void InferFieldType_Int_ReturnsInt()
    {
        Assert.Equal(FieldType.Int, FieldDiscoveryService.InferFieldType(new LuaValue.Int(42)));
    }

    [Fact]
    public void InferFieldType_Num_ReturnsDecimal()
    {
        Assert.Equal(FieldType.Decimal, FieldDiscoveryService.InferFieldType(new LuaValue.Num("-1E+09")));
    }

    [Fact]
    public void InferFieldType_Str_ReturnsString()
    {
        Assert.Equal(FieldType.String, FieldDiscoveryService.InferFieldType(new LuaValue.Str("hello")));
    }

    // GenerateLabel tests

    [Fact]
    public void GenerateLabel_StripsNamespacePrefix()
    {
        Assert.Equal("Government Budget", FieldDiscoveryService.GenerateLabel("BaseGame.GovernmentBudget"));
    }

    [Fact]
    public void GenerateLabel_ReplacesUnderscoresWithSpaces()
    {
        Assert.Equal("My Test Var", FieldDiscoveryService.GenerateLabel("Custom.My_Test_Var"));
    }

    [Fact]
    public void GenerateLabel_InsertsPascalCaseSpaces()
    {
        Assert.Equal("Government Budget", FieldDiscoveryService.GenerateLabel("GovernmentBudget"));
    }

    [Fact]
    public void GenerateLabel_HandlesAcronyms()
    {
        // "USPVote" → "USP Vote"
        Assert.Equal("USP Vote", FieldDiscoveryService.GenerateLabel("Namespace.USPVote"));
    }

    [Fact]
    public void GenerateLabel_NoNamespace_JustSplits()
    {
        Assert.Equal("Simple Key", FieldDiscoveryService.GenerateLabel("SimpleKey"));
    }

    [Fact]
    public void GenerateLabel_DeepNamespace_StripsAll()
    {
        Assert.Equal("Final Part", FieldDiscoveryService.GenerateLabel("A.B.C.FinalPart"));
    }

    [Fact]
    public void GenerateLabel_MixedUnderscoreAndPascalCase()
    {
        Assert.Equal("Economy Reform Status", FieldDiscoveryService.GenerateLabel("BaseGame.Economy_ReformStatus"));
    }

    [Fact]
    public void GenerateLabel_NumbersPreserved()
    {
        Assert.Equal("Phase2 Result", FieldDiscoveryService.GenerateLabel("Custom.Phase2Result"));
    }

    // type inference in DiscoverFields

    [Fact]
    public void DiscoverFields_InfersCorrectTypes()
    {
        var doc = CreateDocument(variables: [
            new LuaVariable("Custom.BoolVar", new LuaValue.Bool(false)),
            new LuaVariable("Custom.IntVar", new LuaValue.Int(10)),
            new LuaVariable("Custom.NumVar", new LuaValue.Num("3.14")),
            new LuaVariable("Custom.StrVar", new LuaValue.Str("text"))
        ]);

        var discovered = _service.DiscoverFields(doc);

        Assert.Equal(4, discovered.Count);
        Assert.Equal(FieldType.Bool, discovered.First(f => f.Id.EndsWith("BoolVar")).Type);
        Assert.Equal(FieldType.Int, discovered.First(f => f.Id.EndsWith("IntVar")).Type);
        Assert.Equal(FieldType.Decimal, discovered.First(f => f.Id.EndsWith("NumVar")).Type);
        Assert.Equal(FieldType.String, discovered.First(f => f.Id.EndsWith("StrVar")).Type);
    }
}
