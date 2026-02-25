using SuzerainSaveEditor.App.ViewModels;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Tests.ViewModels;

public sealed class FieldFilterServiceTests
{
    private static FieldViewModel MakeVm(
        string id, string label, string? description = null) =>
        new(id, label, description, FieldType.Bool, "False");

    [Fact]
    public void FilterGroup_EmptyQuery_ReturnsAllFields()
    {
        var fields = new List<FieldViewModel>
        {
            MakeVm("f1", "Economy"),
            MakeVm("f2", "Military")
        };

        var result = FieldFilterService.FilterGroup("", fields);

        Assert.Equal(2, result.Count);
        Assert.Same(fields, result);
    }

    [Fact]
    public void FilterGroup_MatchesLabelCaseInsensitively()
    {
        var fields = new List<FieldViewModel>
        {
            MakeVm("f1", "Economy Stimulus"),
            MakeVm("f2", "Military Border")
        };

        var result = FieldFilterService.FilterGroup("economy", fields);

        Assert.Single(result);
        Assert.Equal("f1", result[0].FieldId);
    }

    [Fact]
    public void FilterGroup_MatchesFieldId()
    {
        var fields = new List<FieldViewModel>
        {
            MakeVm("economy_var", "Some Label"),
            MakeVm("military_var", "Other Label")
        };

        var result = FieldFilterService.FilterGroup("economy", fields);

        Assert.Single(result);
        Assert.Equal("economy_var", result[0].FieldId);
    }

    [Fact]
    public void FilterGroup_MatchesDescription()
    {
        var fields = new List<FieldViewModel>
        {
            MakeVm("f1", "Alpha", "controls the economy budget"),
            MakeVm("f2", "Beta", "sets military strength")
        };

        var result = FieldFilterService.FilterGroup("economy", fields);

        Assert.Single(result);
        Assert.Equal("f1", result[0].FieldId);
    }

    [Fact]
    public void FilterGroup_NoMatches_ReturnsEmpty()
    {
        var fields = new List<FieldViewModel>
        {
            MakeVm("f1", "Economy"),
            MakeVm("f2", "Military")
        };

        var result = FieldFilterService.FilterGroup("zzz_no_match", fields);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterCategoryTree_EnteringSearch_SavesExpansionStates()
    {
        var service = new FieldFilterService();
        var (nodes, lookup) = CreateSimpleTree();

        Assert.False(service.HasSavedExpansionStates);

        service.FilterCategoryTree("search", nodes, lookup);

        Assert.True(service.HasSavedExpansionStates);
    }

    [Fact]
    public void FilterCategoryTree_ClearingSearch_RestoresExpansionStates()
    {
        var service = new FieldFilterService();
        var (nodes, lookup) = CreateSimpleTree();

        // set a known expansion state
        nodes[0].IsExpanded = true;

        // enter search mode — saves the state
        service.FilterCategoryTree("search", nodes, lookup);

        // search may have changed expansion — now clear search
        var result = service.FilterCategoryTree("", nodes, lookup);

        // expansion updates should restore the saved state
        Assert.NotNull(result.ExpansionUpdates);
        Assert.True(result.ExpansionUpdates![nodes[0].Key]);
        Assert.False(service.HasSavedExpansionStates);
    }

    [Fact]
    public void Reset_ClearsSavedStates()
    {
        var service = new FieldFilterService();
        var (nodes, lookup) = CreateSimpleTree();

        // enter search to save states
        service.FilterCategoryTree("search", nodes, lookup);
        Assert.True(service.HasSavedExpansionStates);

        service.Reset();

        Assert.False(service.HasSavedExpansionStates);
    }

    private static (List<CategoryNodeViewModel> nodes, Dictionary<string, CategoryNodeViewModel> lookup)
        CreateSimpleTree()
    {
        var field = MakeVm("f1", "Economy Stimulus");
        var leaf = new CategoryNodeViewModel("BaseGame.Situation", "Situation", 0,
            [field], []);
        var parent = new CategoryNodeViewModel("BaseGame", "Base Game", 10,
            [], [leaf]);
        leaf.Parent = parent;

        var nodes = new List<CategoryNodeViewModel> { parent };
        var lookup = new Dictionary<string, CategoryNodeViewModel>(StringComparer.Ordinal)
        {
            [parent.Key] = parent,
            [leaf.Key] = leaf
        };

        return (nodes, lookup);
    }
}
