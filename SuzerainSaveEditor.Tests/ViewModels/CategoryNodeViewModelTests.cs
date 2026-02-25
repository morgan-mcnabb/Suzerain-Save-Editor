using SuzerainSaveEditor.App.ViewModels;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Tests.ViewModels;

public sealed class CategoryNodeViewModelTests
{
    private static FieldViewModel MakeFieldVm(
        string id, string label, string? description = null) =>
        new(id, label, description, FieldType.Bool, "False");

    private static CategoryNodeViewModel CreateLeafNode(
        string key = "BaseGame.Situation",
        string label = "Situation",
        int sortOrder = 0,
        List<FieldViewModel>? fields = null,
        CategoryNodeViewModel? parent = null)
    {
        fields ??=
        [
            MakeFieldVm("field1", "Economy Stimulus", "Turn 01 > General"),
            MakeFieldVm("field2", "Military Border", "Turn 01 > Security"),
            MakeFieldVm("field3", "Budget Overview", "Turn 01 > Finance")
        ];
        return new CategoryNodeViewModel(key, label, sortOrder, fields, [], parent);
    }

    private static CategoryNodeViewModel CreateParentNode()
    {
        var parent = new CategoryNodeViewModel("BaseGame", "Base Game", 10, [],
        [
            CreateLeafNode("BaseGame.Situation", "Situation", 0,
            [
                MakeFieldVm("f1", "Economy Stimulus"),
                MakeFieldVm("f2", "Military Border")
            ]),
            CreateLeafNode("BaseGame.Policy", "Policy", 1,
            [
                MakeFieldVm("f3", "Constitutional Reform"),
                MakeFieldVm("f4", "Tax Policy")
            ])
        ]);
        return parent;
    }

    // constructor tests

    [Fact]
    public void Constructor_SetsProperties()
    {
        var node = CreateLeafNode();

        Assert.Equal("BaseGame.Situation", node.Key);
        Assert.Equal("Situation", node.Label);
        Assert.Equal(0, node.SortOrder);
        Assert.Equal(3, node.TotalCount);
        Assert.Equal(3, node.FilteredCount);
    }

    [Fact]
    public void Constructor_LeafNode_IsLeafTrue()
    {
        var node = CreateLeafNode();
        Assert.True(node.IsLeaf);
    }

    [Fact]
    public void Constructor_DefaultsCollapsed()
    {
        var node = CreateLeafNode();
        Assert.False(node.IsExpanded);
    }

    [Fact]
    public void Constructor_DefaultsVisible()
    {
        var node = CreateLeafNode();
        Assert.True(node.IsVisible);
    }

    [Fact]
    public void Constructor_DefaultsNotSelected()
    {
        var node = CreateLeafNode();
        Assert.False(node.IsSelected);
    }

    // header text

    [Fact]
    public void HeaderText_UnfilteredShowsTotalOnly()
    {
        var node = CreateLeafNode();
        Assert.Equal("Situation (3)", node.HeaderText);
    }

    // parent node tests

    [Fact]
    public void ParentNode_IsLeafFalse()
    {
        var parent = CreateParentNode();
        Assert.False(parent.IsLeaf);
    }

    [Fact]
    public void ParentNode_TotalCount_IncludesChildTotals()
    {
        var parent = CreateParentNode();
        Assert.Equal(4, parent.TotalCount); // 2 + 2
    }

    [Fact]
    public void ParentNode_HasChildren()
    {
        var parent = CreateParentNode();
        Assert.Equal(2, parent.Children.Count);
    }

    // breadcrumb

    [Fact]
    public void BreadcrumbPath_RootNode()
    {
        var node = CreateLeafNode();
        Assert.Equal("Situation", node.BreadcrumbPath);
    }

    [Fact]
    public void BreadcrumbPath_ChildNode()
    {
        var parent = new CategoryNodeViewModel("BaseGame", "Base Game", 10, [], [], null);
        var child = CreateLeafNode(parent: parent);
        Assert.Equal("Base Game > Situation", child.BreadcrumbPath);
    }

    // ApplyFilter

    [Fact]
    public void ApplyFilter_EmptyQuery_ShowsAllFields()
    {
        var node = CreateLeafNode();
        var result = node.ApplyFilter("");

        Assert.True(result);
        Assert.Equal(3, node.FilteredCount);
        Assert.True(node.IsVisible);
    }

    [Fact]
    public void ApplyFilter_MatchingLabel_Visible()
    {
        var node = CreateLeafNode();
        var result = node.ApplyFilter("Economy");

        Assert.True(result);
        Assert.Equal(1, node.FilteredCount);
    }

    [Fact]
    public void ApplyFilter_NoMatches_NotVisible()
    {
        var node = CreateLeafNode();
        var result = node.ApplyFilter("NonExistent");

        Assert.False(result);
        Assert.Equal(0, node.FilteredCount);
        Assert.False(node.IsVisible);
    }

    [Fact]
    public void ApplyFilter_CaseInsensitive()
    {
        var node = CreateLeafNode();
        var result = node.ApplyFilter("ECONOMY");

        Assert.True(result);
    }

    [Fact]
    public void ApplyFilter_MatchingDescription()
    {
        var node = CreateLeafNode();
        var result = node.ApplyFilter("Security");

        Assert.True(result);
        Assert.Equal(1, node.FilteredCount);
    }

    [Fact]
    public void ApplyFilter_HeaderTextUpdates()
    {
        var node = CreateLeafNode();
        node.ApplyFilter("Economy");

        Assert.Equal("Situation (1/3)", node.HeaderText);
    }

    [Fact]
    public void ApplyFilter_ClearFilter_RestoresCounts()
    {
        var node = CreateLeafNode();
        node.ApplyFilter("Economy");
        Assert.Equal("Situation (1/3)", node.HeaderText);

        node.ApplyFilter("");
        Assert.Equal("Situation (3)", node.HeaderText);
    }

    // ApplyFilter on parent nodes

    [Fact]
    public void ApplyFilter_ParentNode_FiltersChildren()
    {
        var parent = CreateParentNode();
        parent.ApplyFilter("Constitutional");

        // only Policy child should remain visible (has "Constitutional Reform")
        Assert.Single(parent.Children);
        Assert.Equal("Policy", parent.Children[0].Label);
    }

    [Fact]
    public void ApplyFilter_ParentNode_NoMatchesHidesAll()
    {
        var parent = CreateParentNode();
        var result = parent.ApplyFilter("NonExistent");

        Assert.False(result);
        Assert.Empty(parent.Children);
        Assert.False(parent.IsVisible);
    }

    [Fact]
    public void ApplyFilter_ParentNode_ClearRestoresChildren()
    {
        var parent = CreateParentNode();
        parent.ApplyFilter("Constitutional");
        Assert.Single(parent.Children);

        parent.ApplyFilter("");
        Assert.Equal(2, parent.Children.Count);
    }

    // GetFilteredFields

    [Fact]
    public void GetFilteredFields_EmptyQuery_ReturnsAll()
    {
        var node = CreateLeafNode();
        var fields = node.GetFilteredFields("");

        Assert.Equal(3, fields.Count);
    }

    [Fact]
    public void GetFilteredFields_WithQuery_ReturnsMatches()
    {
        var node = CreateLeafNode();
        var fields = node.GetFilteredFields("Economy");

        Assert.Single(fields);
        Assert.Equal("field1", fields[0].FieldId);
    }

    [Fact]
    public void GetFilteredFields_NoMatches_ReturnsEmpty()
    {
        var node = CreateLeafNode();
        var fields = node.GetFilteredFields("NonExistent");

        Assert.Empty(fields);
    }

    // AllFields

    [Fact]
    public void AllFields_ExposesUnfilteredList()
    {
        var node = CreateLeafNode();
        node.ApplyFilter("Economy"); // filters to 1

        Assert.Equal(3, node.AllFields.Count); // still 3 unfiltered
    }

    // multiple matches

    [Fact]
    public void ApplyFilter_MultipleMatches()
    {
        var fields = new List<FieldViewModel>
        {
            MakeFieldVm("f1", "Budget Overview", "desc1"),
            MakeFieldVm("f2", "Budget Reform", "desc2"),
            MakeFieldVm("f3", "Military Briefing", "desc3")
        };
        var node = new CategoryNodeViewModel("test", "Test", 0, fields, []);

        node.ApplyFilter("Budget");

        Assert.Equal(2, node.FilteredCount);
    }

    // IsParent

    [Fact]
    public void IsParent_LeafNode_False()
    {
        var node = CreateLeafNode();
        Assert.False(node.IsParent);
    }

    [Fact]
    public void IsParent_ParentNode_True()
    {
        var parent = CreateParentNode();
        Assert.True(parent.IsParent);
    }

    // GetAllDescendantFields

    [Fact]
    public void GetAllDescendantFields_LeafNode_ReturnsOwnFields()
    {
        var node = CreateLeafNode();
        var fields = node.GetAllDescendantFields();

        Assert.Equal(3, fields.Count);
    }

    [Fact]
    public void GetAllDescendantFields_ParentNode_ReturnsChildFields()
    {
        var parent = CreateParentNode();
        var fields = parent.GetAllDescendantFields();

        Assert.Equal(4, fields.Count); // 2 + 2 from children
    }

    [Fact]
    public void GetAllDescendantFields_ThreeLevelTree_ReturnsAllDescendants()
    {
        var grandchild1 = CreateLeafNode("gc1", "GC1", 0,
        [
            MakeFieldVm("f1", "Field One"),
            MakeFieldVm("f2", "Field Two")
        ]);
        var grandchild2 = CreateLeafNode("gc2", "GC2", 1,
        [
            MakeFieldVm("f3", "Field Three")
        ]);
        var child = new CategoryNodeViewModel("child", "Child", 0, [], [grandchild1, grandchild2]);
        var root = new CategoryNodeViewModel("root", "Root", 0, [], [child]);

        var fields = root.GetAllDescendantFields();

        Assert.Equal(3, fields.Count);
    }

    [Fact]
    public void GetAllDescendantFields_WithQuery_FiltersDescendants()
    {
        var grandchild1 = CreateLeafNode("gc1", "GC1", 0,
        [
            MakeFieldVm("f1", "Economy Stimulus"),
            MakeFieldVm("f2", "Military Border")
        ]);
        var grandchild2 = CreateLeafNode("gc2", "GC2", 1,
        [
            MakeFieldVm("f3", "Economy Reform")
        ]);
        var child = new CategoryNodeViewModel("child", "Child", 0, [], [grandchild1, grandchild2]);
        var root = new CategoryNodeViewModel("root", "Root", 0, [], [child]);

        var fields = root.GetAllDescendantFields("Economy");

        Assert.Equal(2, fields.Count);
    }

    // GetSubCategorySummaries

    [Fact]
    public void ApplyFilter_ClearWhenAlreadyUnfiltered_DoesNotRebuildChildren()
    {
        var parent = CreateParentNode();
        var childrenBefore = parent.Children.ToList();

        parent.ApplyFilter("");

        // children should be the exact same object references in the same order
        Assert.Equal(childrenBefore.Count, parent.Children.Count);
        for (var i = 0; i < childrenBefore.Count; i++)
            Assert.Same(childrenBefore[i], parent.Children[i]);
    }

    [Fact]
    public void ApplyFilter_SameQueryTwice_DoesNotRebuildChildren()
    {
        var parent = CreateParentNode();

        parent.ApplyFilter("Constitutional");
        Assert.Single(parent.Children);
        var childrenAfterFirst = parent.Children.ToList();

        // track whether the collection was replaced
        var collectionChanged = false;
        parent.Children.CollectionChanged += (_, _) => collectionChanged = true;

        parent.ApplyFilter("Constitutional");

        // same visible set — collection should not have been rebuilt
        Assert.False(collectionChanged);
        Assert.Single(parent.Children);
        Assert.Same(childrenAfterFirst[0], parent.Children[0]);
    }

    [Fact]
    public void ApplyFilter_DifferentQuery_RebuildChildren()
    {
        var parent = CreateParentNode();

        parent.ApplyFilter("Constitutional");
        Assert.Single(parent.Children);

        parent.ApplyFilter("Economy");

        // different visible set — should have rebuilt
        Assert.Single(parent.Children);
        Assert.Equal("Situation", parent.Children[0].Label);
    }

    [Fact]
    public void ApplyFilter_ClearAfterFilter_RebuildsChildren()
    {
        var parent = CreateParentNode();
        parent.ApplyFilter("Constitutional");
        Assert.Single(parent.Children);

        parent.ApplyFilter("");

        Assert.Equal(2, parent.Children.Count);
        Assert.Equal("Situation", parent.Children[0].Label);
        Assert.Equal("Policy", parent.Children[1].Label);
    }

    [Fact]
    public void ApplyFilter_ClearWhenUnfiltered_StillRecursesIntoChildren()
    {
        var parent = CreateParentNode();

        // filter first so children have reduced FilteredCount
        parent.ApplyFilter("Economy");
        Assert.Single(parent.Children);
        Assert.Equal(1, parent.Children[0].FilteredCount);

        // now clear — even though parent rebuilds Children, the children
        // themselves must have their counts restored
        parent.ApplyFilter("");
        Assert.Equal(2, parent.Children.Count);
        Assert.Equal(2, parent.Children[0].FilteredCount); // Situation has 2 fields
        Assert.Equal(2, parent.Children[1].FilteredCount); // Policy has 2 fields
    }

    [Fact]
    public void GetSubCategorySummaries_LeafNode_ReturnsEmpty()
    {
        var node = CreateLeafNode();
        var summaries = node.GetSubCategorySummaries();

        Assert.Empty(summaries);
    }

    [Fact]
    public void GetSubCategorySummaries_ParentNode_ReturnsOnePerChild()
    {
        var parent = CreateParentNode();
        var summaries = parent.GetSubCategorySummaries();

        Assert.Equal(2, summaries.Count);
        Assert.Equal("Situation", summaries[0].Label);
        Assert.Equal("Policy", summaries[1].Label);
    }

    [Fact]
    public void GetSubCategorySummaries_ReturnsCorrectFieldCounts()
    {
        var parent = CreateParentNode();
        var summaries = parent.GetSubCategorySummaries();

        Assert.Equal(2, summaries[0].FieldCount);
        Assert.Equal(2, summaries[1].FieldCount);
    }

    [Fact]
    public void GetSubCategorySummaries_TargetNodePointsToChild()
    {
        var parent = CreateParentNode();
        var summaries = parent.GetSubCategorySummaries();

        Assert.Same(parent.Children[0], summaries[0].TargetNode);
        Assert.Same(parent.Children[1], summaries[1].TargetNode);
    }

    [Fact]
    public void GetSubCategorySummaries_WithSearch_FiltersChildren()
    {
        var parent = CreateParentNode();
        // apply filter first so Children collection is filtered
        parent.ApplyFilter("Constitutional");
        var summaries = parent.GetSubCategorySummaries("Constitutional");

        // only Policy child has "Constitutional Reform"
        Assert.Single(summaries);
        Assert.Equal("Policy", summaries[0].Label);
    }

    [Fact]
    public void GetSubCategorySummaries_WithSearch_ExcludesEmptyChildren()
    {
        var parent = CreateParentNode();
        parent.ApplyFilter("NonExistent");
        var summaries = parent.GetSubCategorySummaries("NonExistent");

        Assert.Empty(summaries);
    }

    [Fact]
    public void GetSubCategorySummaries_EmptySearch_ReturnsAll()
    {
        var parent = CreateParentNode();
        var summaries = parent.GetSubCategorySummaries("");

        Assert.Equal(2, summaries.Count);
    }
}
