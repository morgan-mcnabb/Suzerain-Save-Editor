using SuzerainSaveEditor.App.ViewModels;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Tests.ViewModels;

public sealed class SubCategorySummaryViewModelTests
{
    private static FieldViewModel MakeFieldVm(
        string id, string label, string? description = null, bool isDirty = false)
    {
        var vm = new FieldViewModel(id, label, description, FieldType.Bool, "False");
        if (isDirty) vm.IsDirty = true;
        return vm;
    }

    private static CategoryNodeViewModel MakeLeafNode(
        string key = "BaseGame.Situation",
        string label = "Situation",
        List<FieldViewModel>? fields = null)
    {
        fields ??=
        [
            MakeFieldVm("f1", "Economy Stimulus"),
            MakeFieldVm("f2", "Military Border"),
            MakeFieldVm("f3", "Budget Overview"),
            MakeFieldVm("f4", "Tax Policy"),
            MakeFieldVm("f5", "Education Reform")
        ];
        return new CategoryNodeViewModel(key, label, 0, fields, []);
    }

    // constructor

    [Fact]
    public void Constructor_SetsLabel()
    {
        var node = MakeLeafNode();
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal("Situation", summary.Label);
    }

    [Fact]
    public void Constructor_SetsFieldCount()
    {
        var node = MakeLeafNode();
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal(5, summary.FieldCount);
    }

    [Fact]
    public void Constructor_SetsTargetNode()
    {
        var node = MakeLeafNode();
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Same(node, summary.TargetNode);
    }

    // preview labels

    [Fact]
    public void PreviewLabels_ShowsFirstThree()
    {
        var node = MakeLeafNode();
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal(3, summary.PreviewLabels.Count);
        Assert.Equal("Economy Stimulus", summary.PreviewLabels[0]);
        Assert.Equal("Military Border", summary.PreviewLabels[1]);
        Assert.Equal("Budget Overview", summary.PreviewLabels[2]);
    }

    [Fact]
    public void PreviewLabels_FewerThanThree_ShowsAll()
    {
        var fields = new List<FieldViewModel>
        {
            MakeFieldVm("f1", "Economy"),
            MakeFieldVm("f2", "Military")
        };
        var node = MakeLeafNode(fields: fields);
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal(2, summary.PreviewLabels.Count);
    }

    // remaining count

    [Fact]
    public void RemainingCount_FiveFields_ReturnsTwo()
    {
        var node = MakeLeafNode();
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal(2, summary.RemainingCount);
        Assert.True(summary.HasRemaining);
        Assert.Equal("+2 more", summary.RemainingText);
    }

    [Fact]
    public void RemainingCount_ThreeFields_ReturnsZero()
    {
        var fields = new List<FieldViewModel>
        {
            MakeFieldVm("f1", "A"),
            MakeFieldVm("f2", "B"),
            MakeFieldVm("f3", "C")
        };
        var node = MakeLeafNode(fields: fields);
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal(0, summary.RemainingCount);
        Assert.False(summary.HasRemaining);
    }

    // dirty fields

    [Fact]
    public void DirtyCount_NoDirtyFields_ReturnsZero()
    {
        var node = MakeLeafNode();
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal(0, summary.DirtyCount);
        Assert.False(summary.HasDirtyFields);
    }

    [Fact]
    public void DirtyCount_WithDirtyFields_ReturnsCount()
    {
        var fields = new List<FieldViewModel>
        {
            MakeFieldVm("f1", "Economy", isDirty: true),
            MakeFieldVm("f2", "Military"),
            MakeFieldVm("f3", "Budget", isDirty: true)
        };
        var node = MakeLeafNode(fields: fields);
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal(2, summary.DirtyCount);
        Assert.True(summary.HasDirtyFields);
        Assert.Equal("2 modified", summary.DirtyText);
    }

    [Fact]
    public void DirtyCount_SingleDirty_SingularText()
    {
        var fields = new List<FieldViewModel>
        {
            MakeFieldVm("f1", "Economy", isDirty: true),
            MakeFieldVm("f2", "Military")
        };
        var node = MakeLeafNode(fields: fields);
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal("1 modified", summary.DirtyText);
    }

    // refresh dirty count

    [Fact]
    public void RefreshDirtyCount_UpdatesFromFieldState()
    {
        var fields = new List<FieldViewModel>
        {
            MakeFieldVm("f1", "Economy"),
            MakeFieldVm("f2", "Military")
        };
        var node = MakeLeafNode(fields: fields);
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal(0, summary.DirtyCount);
        Assert.False(summary.HasDirtyFields);

        fields[0].IsDirty = true;
        summary.RefreshDirtyCount();

        Assert.Equal(1, summary.DirtyCount);
        Assert.True(summary.HasDirtyFields);
        Assert.Equal("1 modified", summary.DirtyText);
    }

    [Fact]
    public void RefreshDirtyCount_ClearedAfterRevert()
    {
        var fields = new List<FieldViewModel>
        {
            MakeFieldVm("f1", "Economy", isDirty: true),
            MakeFieldVm("f2", "Military", isDirty: true)
        };
        var node = MakeLeafNode(fields: fields);
        var summary = new SubCategorySummaryViewModel(node);

        Assert.Equal(2, summary.DirtyCount);

        fields[0].IsDirty = false;
        fields[1].IsDirty = false;
        summary.RefreshDirtyCount();

        Assert.Equal(0, summary.DirtyCount);
        Assert.False(summary.HasDirtyFields);
    }

    // search query filtering

    [Fact]
    public void SearchQuery_FiltersFields()
    {
        var fields = new List<FieldViewModel>
        {
            MakeFieldVm("f1", "Economy Stimulus"),
            MakeFieldVm("f2", "Military Border"),
            MakeFieldVm("f3", "Economy Reform"),
            MakeFieldVm("f4", "Tax Policy")
        };
        var node = MakeLeafNode(fields: fields);
        var summary = new SubCategorySummaryViewModel(node, "Economy");

        Assert.Equal(2, summary.FieldCount);
        Assert.Equal(2, summary.PreviewLabels.Count);
    }

    [Fact]
    public void SearchQuery_NoMatches_ZeroCount()
    {
        var node = MakeLeafNode();
        var summary = new SubCategorySummaryViewModel(node, "NonExistent");

        Assert.Equal(0, summary.FieldCount);
        Assert.Empty(summary.PreviewLabels);
    }

    [Fact]
    public void NullSearchQuery_ShowsAll()
    {
        var node = MakeLeafNode();
        var summary = new SubCategorySummaryViewModel(node, null);

        Assert.Equal(5, summary.FieldCount);
    }
}
