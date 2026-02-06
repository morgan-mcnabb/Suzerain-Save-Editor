using SuzerainSaveEditor.Core.Parsing;
using SuzerainSaveEditor.Core.Schema;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Schema;

public sealed class SchemaIntegrationTests
{
    private readonly SchemaService _schema = new();
    private readonly FieldResolver _resolver = new();
    private readonly JsonSaveParser _parser = new();

    private static string GetExampleSaveFilePath()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "example_save-file.json")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.NotNull(dir);
        return Path.Combine(dir!, "example_save-file.json");
    }

    [Fact]
    public void AllSchemaFields_CanBeReadFromExampleSave()
    {
        var doc = _parser.Parse(File.ReadAllText(GetExampleSaveFilePath()));
        var fields = _schema.GetAll();
        var unresolved = new List<string>();

        foreach (var field in fields)
        {
            var value = _resolver.ReadValue(doc, field);
            if (value is null)
                unresolved.Add($"{field.Id} ({field.Path})");
        }

        // all metadata and entity fields should resolve; some variables may not exist in every save
        var metaAndEntityUnresolved = unresolved
            .Where(u => u.Contains("meta:") || u.Contains("entity:"))
            .ToList();
        Assert.Empty(metaAndEntityUnresolved);
    }

    [Fact]
    public void ReadValue_MetaCampaignName_MatchesExpected()
    {
        var doc = _parser.Parse(File.ReadAllText(GetExampleSaveFilePath()));
        var field = _schema.GetById("meta.campaignName")!;

        var value = _resolver.ReadValue(doc, field);

        Assert.Equal("KING", value);
    }

    [Fact]
    public void ReadValue_MetaTurnNo_MatchesExpected()
    {
        var doc = _parser.Parse(File.ReadAllText(GetExampleSaveFilePath()));
        var field = _schema.GetById("meta.turnNo")!;

        var value = _resolver.ReadValue(doc, field);

        Assert.Equal("11", value);
    }

    [Fact]
    public void ReadValue_SordlandGovernmentBudget_ReturnsNonNull()
    {
        var doc = _parser.Parse(File.ReadAllText(GetExampleSaveFilePath()));
        var field = _schema.GetById("sordland.governmentBudget")!;

        var value = _resolver.ReadValue(doc, field);

        Assert.NotNull(value);
        Assert.True(int.TryParse(value, out _), $"Expected integer value, got '{value}'");
    }

    [Fact]
    public void ReadValue_EntityRelations_ReturnsNonNull()
    {
        var doc = _parser.Parse(File.ReadAllText(GetExampleSaveFilePath()));
        var field = _schema.GetById("rizia.entityWehlenRelations")!;

        var value = _resolver.ReadValue(doc, field);

        Assert.NotNull(value);
    }

    [Fact]
    public void ReadValue_EntityHodComposition_ReturnsNonNull()
    {
        var doc = _parser.Parse(File.ReadAllText(GetExampleSaveFilePath()));
        var field = _schema.GetById("rizia.hodComposition")!;

        var value = _resolver.ReadValue(doc, field);

        Assert.NotNull(value);
        Assert.Contains("HODComposition", value);
    }

    [Fact]
    public void WriteValue_ThenSerialize_PreservesRoundTrip()
    {
        var original = File.ReadAllText(GetExampleSaveFilePath());
        var doc = _parser.Parse(original);
        var field = _schema.GetById("meta.notes")!;

        // write a new value
        var modified = _resolver.WriteValue(doc, field, "edited");
        Assert.Equal("edited", _resolver.ReadValue(modified, field));

        // write the original value back
        var restored = _resolver.WriteValue(modified, field, "");
        var serialized = _parser.Serialize(restored);

        Assert.Equal(original, serialized);
    }

    [Fact]
    public void WriteValue_Variable_ThenSerialize_ChangesOnlyThatVariable()
    {
        var original = File.ReadAllText(GetExampleSaveFilePath());
        var doc = _parser.Parse(original);
        var field = _schema.GetById("sordland.governmentBudget")!;
        var originalValue = _resolver.ReadValue(doc, field)!;

        // modify the variable
        var modified = _resolver.WriteValue(doc, field, "99");
        Assert.Equal("99", _resolver.ReadValue(modified, field));

        // other variables should be unchanged
        Assert.Equal(doc.Variables.Count, modified.Variables.Count);

        // restore and verify round-trip
        var restored = _resolver.WriteValue(modified, field, originalValue);
        var serialized = _parser.Serialize(restored);
        Assert.Equal(original, serialized);
    }
}
