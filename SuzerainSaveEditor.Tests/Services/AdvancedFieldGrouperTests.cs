using SuzerainSaveEditor.Core.Schema;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class AdvancedFieldGrouperTests
{
    private static FieldDefinition MakeField(string path, FieldSource source = FieldSource.Variable) => new()
    {
        Id = $"test.{path}",
        Path = path,
        Label = "Test",
        Group = FieldGroup.Advanced,
        Type = FieldType.Bool,
        Source = source,
        Description = "Test field"
    };

    // ClassifyField — turn-prefixed variables

    [Theory]
    [InlineData("variable:GameCondition.Turn01_A_PoliticalOverview", "GameCondition", "Turn01")]
    [InlineData("variable:GameCondition.Turn02_FPnT_DiplomacyOverview", "GameCondition", "Turn02")]
    [InlineData("variable:GameCondition.Turn05_Personal_Ball", "GameCondition", "Turn05")]
    [InlineData("variable:GameCondition.Turn11_Decision_FinalChoice", "GameCondition", "Turn11")]
    public void ClassifyField_TurnPrefixed_ReturnsTurnSubCategory(
        string path, string expectedNamespace, string expectedSubCategory)
    {
        var field = MakeField(path);
        var (ns, subCat, nsLabel, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal(expectedNamespace, ns);
        Assert.Equal(expectedSubCategory, subCat);
        Assert.Equal("Turns", nsLabel);
        Assert.NotNull(subCatLabel);
        Assert.StartsWith("Turn ", subCatLabel);
    }

    // ClassifyField — dot-namespaced variables with sub-categories

    [Fact]
    public void ClassifyField_BaseGame_WithSubCategory()
    {
        var field = MakeField("variable:BaseGame.Situation_Economy_Stimulus");
        var (ns, subCat, nsLabel, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("BaseGame", ns);
        Assert.Equal("Situation", subCat);
        Assert.Equal("Base Game", nsLabel);
        Assert.Equal("Situation", subCatLabel);
    }

    [Fact]
    public void ClassifyField_RiziaDLC_WithSubCategory()
    {
        var field = MakeField("variable:RiziaDLC.Decision_Militarization_Level");
        var (ns, subCat, nsLabel, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("RiziaDLC", ns);
        Assert.Equal("Decision", subCat);
        Assert.Equal("Rizia DLC", nsLabel);
        Assert.Equal("Decision", subCatLabel);
    }

    [Fact]
    public void ClassifyField_BaseGameSupport_NewsSubCategory()
    {
        var field = MakeField("variable:BaseGameSupport.News_Turn03_ST_Gasom");
        var (ns, subCat, nsLabel, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("BaseGameSupport", ns);
        Assert.Equal("News", subCat);
        Assert.Equal("Base Game Support", nsLabel);
        Assert.Equal("News", subCatLabel);
    }

    // ClassifyField — dot-namespaced without sub-category (no underscore after dot)

    [Fact]
    public void ClassifyField_StandaloneVar_NoSubCategory()
    {
        var field = MakeField("variable:BaseGame.GovernmentBudget");
        var (ns, subCat, nsLabel, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("BaseGame", ns);
        Assert.Null(subCat);
        Assert.Equal("Base Game", nsLabel);
        Assert.Null(subCatLabel);
    }

    [Fact]
    public void ClassifyField_BaseGameSetup_NoSubCategory()
    {
        var field = MakeField("variable:BaseGameSetup.CurrentTurn");
        var (ns, subCat, nsLabel, _) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("BaseGameSetup", ns);
        Assert.Null(subCat);
        Assert.Equal("Base Game Setup", nsLabel);
    }

    // ClassifyField — underscore-namespaced variables

    [Fact]
    public void ClassifyField_OpinionVariable_GroupsByPrefix()
    {
        var field = MakeField("variable:Opinion_OldGuard");
        var (ns, subCat, nsLabel, _) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("Opinion", ns);
        Assert.Null(subCat);
        Assert.Equal("Opinions", nsLabel);
    }

    [Fact]
    public void ClassifyField_RelationsVariable_GroupsByPrefix()
    {
        var field = MakeField("variable:Relations_Rumburg");
        var (ns, subCat, nsLabel, _) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("Relations", ns);
        Assert.Null(subCat);
        Assert.Equal("Relations", nsLabel);
    }

    // ClassifyField — entity paths

    [Fact]
    public void ClassifyField_EntityPath_GroupsByNameInDatabase()
    {
        var field = MakeField("entity:Economy_Budget.ProgressPercentage", FieldSource.EntityUpdate);
        var (ns, subCat, nsLabel, _) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.StartsWith("entity:", ns);
        Assert.Null(subCat);
        Assert.StartsWith("Entity:", nsLabel);
    }

    // ClassifyField — fallback

    [Fact]
    public void ClassifyField_NoDelimiter_ReturnsOther()
    {
        var field = MakeField("variable:StandaloneVariable");
        var (ns, _, _, _) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("Other", ns);
    }

    // GroupFieldsHierarchical

    [Fact]
    public void GroupFieldsHierarchical_EmptyInput_ReturnsEmpty()
    {
        var result = AdvancedFieldGrouper.GroupFieldsHierarchical([]);
        Assert.Empty(result);
    }

    [Fact]
    public void GroupFieldsHierarchical_SmallNamespace_NoChildren()
    {
        // below SubCategoryThreshold — should stay flat
        var fields = new List<FieldDefinition>
        {
            MakeField("variable:BaseGameSetup.CurrentTurn"),
            MakeField("variable:BaseGameSetup.PrologueDone")
        };

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        Assert.Single(result);
        Assert.Equal("BaseGameSetup", result[0].Key);
        Assert.Empty(result[0].Children);
        Assert.Equal(2, result[0].Fields.Count);
    }

    [Fact]
    public void GroupFieldsHierarchical_LargeNamespace_HasChildren()
    {
        // create 35 fields in BaseGame with sub-categories (above threshold of 30)
        var fields = new List<FieldDefinition>();
        for (var i = 0; i < 20; i++)
            fields.Add(MakeField($"variable:BaseGame.Situation_Event{i}"));
        for (var i = 0; i < 15; i++)
            fields.Add(MakeField($"variable:BaseGame.Policy_Reform{i}"));

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        Assert.Single(result);
        var baseGame = result[0];
        Assert.Equal("BaseGame", baseGame.Key);
        Assert.Equal(2, baseGame.Children.Count);
        Assert.Empty(baseGame.Fields); // fields moved to children

        // children ordered by count descending
        Assert.Equal("Situation", baseGame.Children[0].Label);
        Assert.Equal(20, baseGame.Children[0].Fields.Count);
        Assert.Equal("Policy", baseGame.Children[1].Label);
        Assert.Equal(15, baseGame.Children[1].Fields.Count);
    }

    [Fact]
    public void GroupFieldsHierarchical_UncategorizedFieldsInOtherChild()
    {
        var fields = new List<FieldDefinition>();
        for (var i = 0; i < 25; i++)
            fields.Add(MakeField($"variable:BaseGame.Situation_Event{i}"));
        // add some uncategorized fields (no underscore after dot)
        for (var i = 0; i < 6; i++)
            fields.Add(MakeField($"variable:BaseGame.StandaloneVar{i}"));

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        Assert.Single(result);
        var baseGame = result[0];
        // should have Situation + Other children
        Assert.Equal(2, baseGame.Children.Count);
        var otherChild = baseGame.Children.FirstOrDefault(c => c.Label == "Other");
        Assert.NotNull(otherChild);
        Assert.Equal(6, otherChild.Fields.Count);
    }

    [Fact]
    public void GroupFieldsHierarchical_TurnsPrefixed_GroupedUnderTurns()
    {
        var fields = new List<FieldDefinition>
        {
            MakeField("variable:GameCondition.Turn01_A_Event"),
            MakeField("variable:GameCondition.Turn01_EnT_Event"),
            MakeField("variable:GameCondition.Turn02_A_Event")
        };

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        // GameCondition should be flat (only 3 fields, below threshold)
        // but turns are sub-categories
        var gc = result.FirstOrDefault(c => c.Key == "GameCondition");
        Assert.NotNull(gc);
        Assert.Equal("Turns", gc.Label);
    }

    [Fact]
    public void GroupFieldsHierarchical_MixedNamespaces_OrderedBySortOrder()
    {
        var fields = new List<FieldDefinition>
        {
            MakeField("variable:RiziaDLC.Something_One"),
            MakeField("variable:BaseGame.Something_Two"),
            MakeField("variable:GameCondition.Turn01_A_Event"),
            MakeField("variable:Opinion_Test")
        };

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);
        var keys = result.Select(c => c.Key).ToList();

        // BaseGame (10) < RiziaDLC (20) < GameCondition (40) < Opinion (500)
        var baseIdx = keys.IndexOf("BaseGame");
        var riziaIdx = keys.IndexOf("RiziaDLC");
        var gcIdx = keys.IndexOf("GameCondition");
        var opinionIdx = keys.IndexOf("Opinion");

        Assert.True(baseIdx < riziaIdx);
        Assert.True(riziaIdx < gcIdx);
        Assert.True(gcIdx < opinionIdx);
    }

    [Fact]
    public void GroupFieldsHierarchical_EntityFields_Included()
    {
        var fields = new List<FieldDefinition>
        {
            MakeField("entity:SomeEntity.Field", FieldSource.EntityUpdate),
            MakeField("variable:BaseGame.Something_One")
        };

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        Assert.Equal(2, result.Count);
        var entityNode = result.FirstOrDefault(c => c.Key.StartsWith("entity:"));
        Assert.NotNull(entityNode);
        Assert.Single(entityNode.Fields);
    }
}
