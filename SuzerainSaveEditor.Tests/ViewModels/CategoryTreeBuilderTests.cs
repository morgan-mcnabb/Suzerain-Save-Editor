using SuzerainSaveEditor.App.ViewModels;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Tests.ViewModels;

public sealed class CategoryTreeBuilderTests
{
    private static FieldDefinition MakeDef(string id, string path) => new()
    {
        Id = id,
        Path = path,
        Label = id,
        Group = FieldGroup.Advanced,
        Type = FieldType.Bool,
        Source = FieldSource.Variable
    };

    private static FieldViewModel MakeVm(string id) =>
        new(id, id, null, FieldType.Bool, "False");

    private static (List<FieldViewModel> fields, Dictionary<string, FieldViewModel> lookup, StubSchemaService schema)
        SetupFields(params FieldDefinition[] defs)
    {
        var fields = new List<FieldViewModel>();
        var lookup = new Dictionary<string, FieldViewModel>(StringComparer.Ordinal);
        var schema = new StubSchemaService(defs);

        foreach (var def in defs)
        {
            var vm = MakeVm(def.Id);
            fields.Add(vm);
            lookup[def.Id] = vm;
        }

        return (fields, lookup, schema);
    }

    [Fact]
    public void Build_EmptyFields_ReturnsEmptyResult()
    {
        var (fields, lookup, schema) = SetupFields();

        var result = CategoryTreeBuilder.Build(fields, schema, lookup);

        Assert.Empty(result.RootNodes);
        Assert.Empty(result.NodeLookup);
    }

    [Fact]
    public void Build_SingleField_SingleRootNode()
    {
        var def = MakeDef("f1", "variable:BaseGame.GovernmentBudget");
        var (fields, lookup, schema) = SetupFields(def);

        var result = CategoryTreeBuilder.Build(fields, schema, lookup);

        Assert.Single(result.RootNodes);
        Assert.Equal("BaseGame", result.RootNodes[0].Key);
        Assert.Equal(1, result.RootNodes[0].TotalCount);
    }

    [Fact]
    public void Build_MultipleNamespaces_MultipleRoots()
    {
        var defs = new[]
        {
            MakeDef("f1", "variable:BaseGame.GovernmentBudget"),
            MakeDef("f2", "variable:RiziaDLC.SomeFlag")
        };
        var (fields, lookup, schema) = SetupFields(defs);

        var result = CategoryTreeBuilder.Build(fields, schema, lookup);

        Assert.Equal(2, result.RootNodes.Count);
        var keys = result.RootNodes.Select(n => n.Key).OrderBy(k => k).ToList();
        Assert.Contains("BaseGame", keys);
        Assert.Contains("RiziaDLC", keys);
    }

    [Fact]
    public void Build_NodeLookupContainsAllNodesIncludingChildren()
    {
        // create enough fields under one namespace to trigger sub-categorization
        var defs = new List<FieldDefinition>();
        for (var i = 0; i < 35; i++)
            defs.Add(MakeDef($"f{i}", $"variable:BaseGame.Sub{i % 3}_{i}_Detail"));

        var (fields, lookup, schema) = SetupFields(defs.ToArray());

        var result = CategoryTreeBuilder.Build(fields, schema, lookup);

        // node lookup should contain root + all children
        Assert.True(result.NodeLookup.Count > result.RootNodes.Count);

        // every root node should be in the lookup
        foreach (var root in result.RootNodes)
            Assert.True(result.NodeLookup.ContainsKey(root.Key));

        // every child should be in the lookup
        foreach (var root in result.RootNodes)
        {
            foreach (var child in root.Children)
                Assert.True(result.NodeLookup.ContainsKey(child.Key));
        }
    }

    [Fact]
    public void Build_ChildNodesHaveCorrectParentReferences()
    {
        // create enough fields to trigger sub-categorization
        var defs = new List<FieldDefinition>();
        for (var i = 0; i < 35; i++)
            defs.Add(MakeDef($"f{i}", $"variable:BaseGame.Sub{i % 3}_{i}_Detail"));

        var (fields, lookup, schema) = SetupFields(defs.ToArray());

        var result = CategoryTreeBuilder.Build(fields, schema, lookup);

        foreach (var root in result.RootNodes)
        {
            // root nodes should have null parent
            Assert.Null(root.Parent);

            foreach (var child in root.Children)
            {
                // children should reference their parent
                Assert.Same(root, child.Parent);
            }
        }
    }

    private sealed class StubSchemaService : ISchemaService
    {
        private readonly Dictionary<string, FieldDefinition> _defs;
        private readonly List<FieldDefinition> _all;

        public StubSchemaService(IEnumerable<FieldDefinition> defs)
        {
            _all = defs.ToList();
            _defs = _all.ToDictionary(d => d.Id, StringComparer.Ordinal);
        }

        public IReadOnlyList<FieldDefinition> GetAll() => _all;
        public IReadOnlyList<FieldDefinition> GetByGroup(FieldGroup group) =>
            _all.Where(d => d.Group == group).ToList();
        public FieldDefinition? GetById(string id) =>
            _defs.GetValueOrDefault(id);
        public IReadOnlyList<FieldDefinition> Search(string query) => [];
    }
}
