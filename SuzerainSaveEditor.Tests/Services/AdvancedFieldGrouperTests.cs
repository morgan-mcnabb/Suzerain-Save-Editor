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

    // ClassifyField — all turn fields route to unified "Turns" namespace

    [Theory]
    [InlineData("variable:GameCondition.Turn01_A_PoliticalOverview", "Turn01")]
    [InlineData("variable:GameCondition.Turn02_FPnT_DiplomacyOverview", "Turn02")]
    [InlineData("variable:GameCondition.Turn05_Personal_Ball", "Turn05")]
    [InlineData("variable:GameCondition.Turn11_Decision_FinalChoice", "Turn11")]
    public void ClassifyField_GameConditionTurn_RoutesToUnifiedTurns(
        string path, string expectedSubCategory)
    {
        var field = MakeField(path);
        var (ns, subCat, nsLabel, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("Turns", ns);
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

    // ClassifyField — entity paths (grouped by prefix)

    [Fact]
    public void ClassifyField_EntityPath_GroupsByPrefix()
    {
        var field = MakeField("entity:Economy_Budget.ProgressPercentage", FieldSource.EntityUpdate);
        var (ns, subCat, nsLabel, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("entity:Economy", ns);
        Assert.Equal("Economy_Budget", subCat);
        Assert.Equal("Entity: Economy", nsLabel);
        Assert.Equal("Budget", subCatLabel);
    }

    [Fact]
    public void ClassifyField_EntityPath_TurnEntity_RoutesToUnifiedTurns()
    {
        var field = MakeField("entity:Turn01_E_Infrastructure.SomeField", FieldSource.EntityUpdate);
        var (ns, subCat, nsLabel, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("Turns", ns);
        Assert.Equal("Turn01", subCat);
        Assert.Equal("Turns", nsLabel);
        Assert.Equal("Turn 1", subCatLabel);
    }

    [Fact]
    public void ClassifyField_EntityPath_PositionEntity_GroupedByPrefix()
    {
        var field = MakeField("entity:Position_AgricultureMinister.SomeField", FieldSource.EntityUpdate);
        var (ns, subCat, nsLabel, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("entity:Position", ns);
        Assert.Equal("Position_AgricultureMinister", subCat);
        Assert.Equal("Entity: Positions", nsLabel);
        Assert.Equal("Agriculture Minister", subCatLabel);
    }

    [Fact]
    public void ClassifyField_EntityPath_NoUnderscore_StandaloneNamespace()
    {
        var field = MakeField("entity:SomeEntity.Field", FieldSource.EntityUpdate);
        var (ns, subCat, nsLabel, _) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("entity:SomeEntity", ns);
        Assert.Null(subCat);
        Assert.Equal("Entity: Some Entity", nsLabel);
    }

    [Fact]
    public void ClassifyField_EntityPath_MultipleTurnEntities_AllRouteToUnifiedTurns()
    {
        var field1 = MakeField("entity:Turn01_Start_Inauguration.Value", FieldSource.EntityUpdate);
        var field2 = MakeField("entity:Turn01_E_Infrastructure.Value", FieldSource.EntityUpdate);

        var (ns1, subCat1, _, _) = AdvancedFieldGrouper.ClassifyField(field1);
        var (ns2, subCat2, _, _) = AdvancedFieldGrouper.ClassifyField(field2);

        Assert.Equal("Turns", ns1);
        Assert.Equal("Turns", ns2);
        Assert.Equal("Turn01", subCat1);
        Assert.Equal("Turn01", subCat2);
    }

    // ClassifyField — variable TurnXX fields route to unified Turns

    [Theory]
    [InlineData("variable:BaseGame.Turn01_Event_Something", "Turn01", "Turn 1")]
    [InlineData("variable:BaseGame.Turn02_Event_Something", "Turn02", "Turn 2")]
    [InlineData("variable:BaseGame.Turn11_Event_Something", "Turn11", "Turn 11")]
    public void ClassifyField_VariableTurn_RoutesToUnifiedTurns(
        string path, string expectedSubCat, string expectedSubLabel)
    {
        var field = MakeField(path);
        var (ns, subCat, nsLabel, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("Turns", ns);
        Assert.Equal(expectedSubCat, subCat);
        Assert.Equal("Turns", nsLabel);
        Assert.Equal(expectedSubLabel, subCatLabel);
    }

    [Fact]
    public void ClassifyField_VariableTurn_NonTurnSubCategory_NotNormalized()
    {
        // "Taurus" starts with "T" but is not TurnXX — should not be normalized
        var field = MakeField("variable:BaseGame.Taurus_SomeEvent");
        var (ns, subCat, _, subCatLabel) = AdvancedFieldGrouper.ClassifyField(field);

        Assert.Equal("BaseGame", ns);
        Assert.Equal("Taurus", subCat);
        Assert.Equal("Taurus", subCatLabel);
    }

    [Fact]
    public void ClassifyField_VariableTurn_DifferentNamespaces_AllRouteToUnifiedTurns()
    {
        var paths = new[]
        {
            ("variable:BaseGameIsolated.Turn01_Event", "Turn01"),
            ("variable:RiziaDLC.Turn05_Decision", "Turn05"),
            ("variable:RiziaDLCIsolated.Turn08_Scene", "Turn08")
        };

        foreach (var (path, expectedSubCat) in paths)
        {
            var field = MakeField(path);
            var (ns, subCat, nsLabel, _) = AdvancedFieldGrouper.ClassifyField(field);
            Assert.Equal("Turns", ns);
            Assert.Equal(expectedSubCat, subCat);
            Assert.Equal("Turns", nsLabel);
        }
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

        var turns = result.FirstOrDefault(c => c.Key == "Turns");
        Assert.NotNull(turns);
        Assert.Equal("Turns", turns.Label);
        Assert.Equal(3, turns.Fields.Count); // below threshold, flat
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

        // BaseGame (10) < RiziaDLC (20) < Turns (40) < Opinion (500)
        var baseIdx = keys.IndexOf("BaseGame");
        var riziaIdx = keys.IndexOf("RiziaDLC");
        var turnsIdx = keys.IndexOf("Turns");
        var opinionIdx = keys.IndexOf("Opinion");

        Assert.True(baseIdx < riziaIdx);
        Assert.True(riziaIdx < turnsIdx);
        Assert.True(turnsIdx < opinionIdx);
    }

    [Fact]
    public void GroupFieldsHierarchical_EntityFields_StandaloneEntity_Included()
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

    [Fact]
    public void GroupFieldsHierarchical_EntitiesGroupedByPrefix()
    {
        // multiple Position entities should share one parent node
        var fields = new List<FieldDefinition>();
        for (var i = 0; i < 35; i++)
            fields.Add(MakeField($"entity:Position_Minister{i}.SomeField", FieldSource.EntityUpdate));

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        Assert.Single(result);
        var positionNode = result[0];
        Assert.Equal("entity:Position", positionNode.Key);
        Assert.Equal("Entity: Positions", positionNode.Label);
        // above threshold with 35 sub-categories → should have children
        Assert.NotEmpty(positionNode.Children);
        Assert.Equal(35, positionNode.Children.Sum(c => c.Fields.Count));
    }

    [Fact]
    public void GroupFieldsHierarchical_TurnEntities_MergedUnderUnifiedTurns()
    {
        var fields = new List<FieldDefinition>
        {
            MakeField("entity:Turn01_E_Infrastructure.Field1", FieldSource.EntityUpdate),
            MakeField("entity:Turn01_Start_Inauguration.Field1", FieldSource.EntityUpdate),
            MakeField("entity:Turn01_WonTheElection.Field1", FieldSource.EntityUpdate)
        };

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        // all Turn01 entities merge into unified Turns namespace
        Assert.Single(result);
        var turnsNode = result[0];
        Assert.Equal("Turns", turnsNode.Key);
        Assert.Equal("Turns", turnsNode.Label);
        Assert.Equal(3, turnsNode.Fields.Count); // below threshold, flat
    }

    [Fact]
    public void GroupFieldsHierarchical_VariableTurns_AllRouteToUnifiedTurns()
    {
        // Turn01..Turn11 fields from BaseGame all route to unified Turns namespace
        var fields = new List<FieldDefinition>();
        for (var turn = 1; turn <= 11; turn++)
            for (var i = 0; i < 3; i++)
                fields.Add(MakeField($"variable:BaseGame.Turn{turn:D2}_Event{i}"));

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        Assert.Single(result);
        var turns = result[0];
        Assert.Equal("Turns", turns.Key);
        // 33 fields across 11 turn sub-categories → has children, sorted chronologically
        Assert.Equal(11, turns.Children.Count);
        Assert.Equal("Turn 1", turns.Children[0].Label);
        Assert.Equal(3, turns.Children[0].Fields.Count);
        Assert.Equal("Turn 11", turns.Children[10].Label);
    }

    [Fact]
    public void GroupFieldsHierarchical_VariableTurns_SplitFromOriginalNamespace()
    {
        var fields = new List<FieldDefinition>();
        // 20 turn fields → route to unified Turns
        for (var turn = 1; turn <= 4; turn++)
            for (var i = 0; i < 5; i++)
                fields.Add(MakeField($"variable:BaseGame.Turn{turn:D2}_Event{i}"));
        // 15 policy fields → stay in BaseGame
        for (var i = 0; i < 15; i++)
            fields.Add(MakeField($"variable:BaseGame.Policy_Reform{i}"));

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        // should have 2 nodes: BaseGame (policy) and Turns
        Assert.Equal(2, result.Count);
        var baseGame = result.FirstOrDefault(c => c.Key == "BaseGame");
        Assert.NotNull(baseGame);
        Assert.Equal(15, baseGame.Fields.Count); // policy fields, below threshold → flat

        var turns = result.FirstOrDefault(c => c.Key == "Turns");
        Assert.NotNull(turns);
        Assert.Equal(20, turns.Fields.Count); // 4 turns × 5 fields, below threshold → flat
    }

    [Fact]
    public void GroupFieldsHierarchical_UnifiedTurns_MixesAllSources()
    {
        // fields from GameCondition, BaseGame, and entities all merge into one Turns node
        var fields = new List<FieldDefinition>();
        for (var i = 0; i < 15; i++)
            fields.Add(MakeField($"variable:GameCondition.Turn01_A_Event{i}"));
        for (var i = 0; i < 10; i++)
            fields.Add(MakeField($"variable:BaseGame.Turn01_Something{i}"));
        for (var i = 0; i < 6; i++)
            fields.Add(MakeField($"entity:Turn01_Entity{i}.Field", FieldSource.EntityUpdate));

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        // all 31 fields in one "Turns" node, Turn01 has 3 source children
        var turns = result.FirstOrDefault(c => c.Key == "Turns");
        Assert.NotNull(turns);
        Assert.Single(turns.Children); // Turn01 only
        var turn01 = turns.Children[0];
        Assert.Equal("Turn 1", turn01.Label);
        Assert.Empty(turn01.Fields); // fields moved to source children
        Assert.Equal(3, turn01.Children.Count);

        // source children ordered by NamespaceSortOrder: BaseGame(10), GameCondition(35), Entities(500)
        Assert.Equal("Base Game", turn01.Children[0].Label);
        Assert.Equal(10, turn01.Children[0].Fields.Count);
        Assert.Equal("Game Condition", turn01.Children[1].Label);
        Assert.Equal(15, turn01.Children[1].Fields.Count);
        Assert.Equal("Entities", turn01.Children[2].Label);
        Assert.Equal(6, turn01.Children[2].Fields.Count);
    }

    [Fact]
    public void GroupFieldsHierarchical_TurnChildren_SingleSourceStaysFlat()
    {
        // turn fields all from one source — no 3rd level
        var fields = new List<FieldDefinition>();
        for (var turn = 1; turn <= 11; turn++)
            for (var i = 0; i < 4; i++)
                fields.Add(MakeField($"variable:BaseGame.Turn{turn:D2}_Event{i}"));

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        var turns = result.FirstOrDefault(c => c.Key == "Turns");
        Assert.NotNull(turns);
        Assert.Equal(11, turns.Children.Count);
        // each Turn child has fields directly (no grandchildren)
        foreach (var turn in turns.Children)
        {
            Assert.Equal(4, turn.Fields.Count);
            Assert.Empty(turn.Children);
        }
    }

    [Fact]
    public void GroupFieldsHierarchical_TurnChildren_MultipleSourcesGet3rdLevel()
    {
        // Turn01 has fields from BaseGame + GameCondition + entities → 3 source groups
        var fields = new List<FieldDefinition>();
        for (var i = 0; i < 12; i++)
            fields.Add(MakeField($"variable:BaseGame.Turn01_Event{i}"));
        for (var i = 0; i < 15; i++)
            fields.Add(MakeField($"variable:GameCondition.Turn01_A_Event{i}"));
        for (var i = 0; i < 5; i++)
            fields.Add(MakeField($"entity:Turn01_Entity{i}.Field", FieldSource.EntityUpdate));
        // Turn02 has fields from only BaseGame → stays flat
        for (var i = 0; i < 8; i++)
            fields.Add(MakeField($"variable:BaseGame.Turn02_Event{i}"));

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        var turns = result.FirstOrDefault(c => c.Key == "Turns");
        Assert.NotNull(turns);
        Assert.Equal(2, turns.Children.Count);

        // Turn01 has multiple sources → grandchildren
        var turn01 = turns.Children[0];
        Assert.Equal("Turn 1", turn01.Label);
        Assert.Empty(turn01.Fields);
        Assert.Equal(3, turn01.Children.Count);
        Assert.Equal(32, turn01.Children.Sum(c => c.Fields.Count));

        // Turn02 has single source → flat
        var turn02 = turns.Children[1];
        Assert.Equal("Turn 2", turn02.Label);
        Assert.Equal(8, turn02.Fields.Count);
        Assert.Empty(turn02.Children);
    }

    [Fact]
    public void GroupFieldsHierarchical_TurnSourceKeys_NormalizedToParent()
    {
        // BaseGameIsolated → BaseGame, RiziaDLCIsolated/Support → RiziaDLC
        var fields = new List<FieldDefinition>();
        for (var i = 0; i < 10; i++)
            fields.Add(MakeField($"variable:BaseGame.Turn01_Event{i}"));
        for (var i = 0; i < 8; i++)
            fields.Add(MakeField($"variable:BaseGameIsolated.Turn01_Iso{i}"));
        for (var i = 0; i < 6; i++)
            fields.Add(MakeField($"variable:RiziaDLC.Turn01_Rizia{i}"));
        for (var i = 0; i < 5; i++)
            fields.Add(MakeField($"variable:RiziaDLCIsolated.Turn01_RizIso{i}"));
        for (var i = 0; i < 4; i++)
            fields.Add(MakeField($"variable:RiziaDLCSupport.Turn01_RizSup{i}"));

        var result = AdvancedFieldGrouper.GroupFieldsHierarchical(fields);

        var turns = result.FirstOrDefault(c => c.Key == "Turns");
        Assert.NotNull(turns);
        Assert.Single(turns.Children); // Turn01 only
        var turn01 = turns.Children[0];

        // BaseGame + BaseGameIsolated → "Base Game" (18 fields)
        // RiziaDLC + RiziaDLCIsolated + RiziaDLCSupport → "Rizia DLC" (15 fields)
        Assert.Equal(2, turn01.Children.Count);
        var baseGame = turn01.Children.FirstOrDefault(c => c.Label == "Base Game");
        Assert.NotNull(baseGame);
        Assert.Equal(18, baseGame.Fields.Count);

        var rizia = turn01.Children.FirstOrDefault(c => c.Label == "Rizia DLC");
        Assert.NotNull(rizia);
        Assert.Equal(15, rizia.Fields.Count);
    }
}
