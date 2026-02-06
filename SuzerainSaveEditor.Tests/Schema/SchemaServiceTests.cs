using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Tests.Schema;

public sealed class SchemaServiceTests
{
    private readonly SchemaService _service = new();

    [Fact]
    public void GetAll_ReturnsNonEmptyList()
    {
        var fields = _service.GetAll();

        Assert.NotEmpty(fields);
    }

    [Fact]
    public void GetAll_AllFieldsHaveRequiredProperties()
    {
        var fields = _service.GetAll();

        foreach (var field in fields)
        {
            Assert.False(string.IsNullOrWhiteSpace(field.Id), $"Field has empty Id");
            Assert.False(string.IsNullOrWhiteSpace(field.Path), $"Field {field.Id} has empty Path");
            Assert.False(string.IsNullOrWhiteSpace(field.Label), $"Field {field.Id} has empty Label");
        }
    }

    [Fact]
    public void GetAll_AllFieldsHaveUniqueIds()
    {
        var fields = _service.GetAll();
        var ids = fields.Select(f => f.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void GetAll_ContainsFieldsFromAllThreeGroups()
    {
        var fields = _service.GetAll();
        var groups = fields.Select(f => f.Group).Distinct().ToList();

        Assert.Contains(FieldGroup.General, groups);
        Assert.Contains(FieldGroup.Sordland, groups);
        Assert.Contains(FieldGroup.Rizia, groups);
    }

    [Fact]
    public void GetAll_ContainsFieldsFromAllThreeSources()
    {
        var fields = _service.GetAll();
        var sources = fields.Select(f => f.Source).Distinct().ToList();

        Assert.Contains(FieldSource.Variable, sources);
        Assert.Contains(FieldSource.EntityUpdate, sources);
        Assert.Contains(FieldSource.Metadata, sources);
    }

    [Fact]
    public void GetByGroup_General_ReturnsOnlyGeneralFields()
    {
        var fields = _service.GetByGroup(FieldGroup.General);

        Assert.NotEmpty(fields);
        Assert.All(fields, f => Assert.Equal(FieldGroup.General, f.Group));
    }

    [Fact]
    public void GetByGroup_Sordland_ReturnsOnlySordlandFields()
    {
        var fields = _service.GetByGroup(FieldGroup.Sordland);

        Assert.NotEmpty(fields);
        Assert.All(fields, f => Assert.Equal(FieldGroup.Sordland, f.Group));
    }

    [Fact]
    public void GetByGroup_Rizia_ReturnsOnlyRiziaFields()
    {
        var fields = _service.GetByGroup(FieldGroup.Rizia);

        Assert.NotEmpty(fields);
        Assert.All(fields, f => Assert.Equal(FieldGroup.Rizia, f.Group));
    }

    [Fact]
    public void GetById_KnownId_ReturnsCorrectField()
    {
        var field = _service.GetById("meta.campaignName");

        Assert.NotNull(field);
        Assert.Equal("Campaign Name", field.Label);
        Assert.Equal("meta:campaignName", field.Path);
        Assert.Equal(FieldGroup.General, field.Group);
        Assert.Equal(FieldType.String, field.Type);
        Assert.Equal(FieldSource.Metadata, field.Source);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var field = _service.GetById("nonexistent.field");

        Assert.Null(field);
    }

    [Fact]
    public void GetById_VariableField_HasCorrectPathPrefix()
    {
        var field = _service.GetById("sordland.governmentBudget");

        Assert.NotNull(field);
        Assert.StartsWith("variable:", field.Path);
        Assert.Equal(FieldSource.Variable, field.Source);
    }

    [Fact]
    public void GetById_EntityField_HasCorrectPathPrefix()
    {
        var field = _service.GetById("rizia.entityWehlenRelations");

        Assert.NotNull(field);
        Assert.StartsWith("entity:", field.Path);
        Assert.Equal(FieldSource.EntityUpdate, field.Source);
    }

    [Fact]
    public void GetById_FieldWithMinMax_HasConstraints()
    {
        var field = _service.GetById("meta.turnNo");

        Assert.NotNull(field);
        Assert.Equal(1, field.Min);
        Assert.Equal(20, field.Max);
    }

    [Fact]
    public void GetById_FieldWithDescription_HasDescription()
    {
        var field = _service.GetById("sordland.governmentBudget");

        Assert.NotNull(field);
        Assert.False(string.IsNullOrWhiteSpace(field.Description));
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        var all = _service.GetAll();
        var result = _service.Search("");

        Assert.Equal(all.Count, result.Count);
    }

    [Fact]
    public void Search_WhitespaceQuery_ReturnsAll()
    {
        var all = _service.GetAll();
        var result = _service.Search("   ");

        Assert.Equal(all.Count, result.Count);
    }

    [Fact]
    public void Search_ByLabel_FindsMatchingFields()
    {
        var results = _service.Search("budget");

        Assert.NotEmpty(results);
        Assert.All(results, f =>
            Assert.True(
                f.Label.Contains("budget", StringComparison.OrdinalIgnoreCase) ||
                f.Id.Contains("budget", StringComparison.OrdinalIgnoreCase) ||
                (f.Description?.Contains("budget", StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    [Fact]
    public void Search_CaseInsensitive_FindsMatches()
    {
        var lower = _service.Search("rumburg");
        var upper = _service.Search("RUMBURG");
        var mixed = _service.Search("Rumburg");

        Assert.Equal(lower.Count, upper.Count);
        Assert.Equal(lower.Count, mixed.Count);
    }

    [Fact]
    public void Search_ById_FindsMatch()
    {
        var results = _service.Search("meta.campaignName");

        Assert.Contains(results, f => f.Id == "meta.campaignName");
    }

    [Fact]
    public void Search_ByDescription_FindsMatch()
    {
        var results = _service.Search("diplomatic");

        Assert.NotEmpty(results);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var results = _service.Search("xyznonexistent123");

        Assert.Empty(results);
    }

    [Fact]
    public void GetAll_PathPrefixesMatchSource()
    {
        var fields = _service.GetAll();

        foreach (var field in fields)
        {
            var expectedPrefix = field.Source switch
            {
                FieldSource.Variable => "variable:",
                FieldSource.EntityUpdate => "entity:",
                FieldSource.Metadata => "meta:",
                _ => throw new InvalidOperationException()
            };
            Assert.True(field.Path.StartsWith(expectedPrefix, StringComparison.Ordinal),
                $"Field {field.Id} with source {field.Source} has path '{field.Path}' that doesn't start with '{expectedPrefix}'");
        }
    }
}
