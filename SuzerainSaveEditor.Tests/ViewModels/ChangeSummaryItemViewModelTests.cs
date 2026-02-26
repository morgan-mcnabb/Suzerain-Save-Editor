using SuzerainSaveEditor.App.ViewModels;

namespace SuzerainSaveEditor.Tests.ViewModels;

public sealed class ChangeSummaryItemViewModelTests
{
    [Fact]
    public void Properties_SetCorrectly()
    {
        var item = new ChangeSummaryItemViewModel("Budget", "Int", "500", "999");

        Assert.Equal("Budget", item.Label);
        Assert.Equal("Int", item.FieldType);
        Assert.Equal("500", item.OldValue);
        Assert.Equal("999", item.NewValue);
    }

    [Fact]
    public void OldValueDisplay_WhenOldValuePresent_ReturnsOldValue()
    {
        var item = new ChangeSummaryItemViewModel("Budget", "Int", "500", "999");

        Assert.Equal("500", item.OldValueDisplay);
    }

    [Fact]
    public void OldValueDisplay_WhenOldValueNull_ReturnsNone()
    {
        var item = new ChangeSummaryItemViewModel("Budget", "Int", null, "999");

        Assert.Equal("(none)", item.OldValueDisplay);
    }

    [Fact]
    public void HasOldValue_WhenOldValuePresent_ReturnsTrue()
    {
        var item = new ChangeSummaryItemViewModel("Budget", "Int", "500", "999");

        Assert.True(item.HasOldValue);
    }

    [Fact]
    public void HasOldValue_WhenOldValueNull_ReturnsFalse()
    {
        var item = new ChangeSummaryItemViewModel("Budget", "Int", null, "999");

        Assert.False(item.HasOldValue);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new ChangeSummaryItemViewModel("Budget", "Int", "500", "999");
        var b = new ChangeSummaryItemViewModel("Budget", "Int", "500", "999");

        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = new ChangeSummaryItemViewModel("Budget", "Int", "500", "999");
        var b = new ChangeSummaryItemViewModel("Budget", "Int", "500", "1000");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void BoolField_DisplaysCorrectly()
    {
        var item = new ChangeSummaryItemViewModel("Democracy", "Bool", "True", "False");

        Assert.Equal("True", item.OldValueDisplay);
        Assert.Equal("False", item.NewValue);
    }

    [Fact]
    public void EmptyStringOldValue_IsNotNull()
    {
        var item = new ChangeSummaryItemViewModel("Name", "String", "", "Anton");

        Assert.True(item.HasOldValue);
        Assert.Equal("", item.OldValueDisplay);
    }
}
