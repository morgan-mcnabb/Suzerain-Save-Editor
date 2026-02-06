using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Tests.Models;

public sealed class EntityUpdateTests
{
    [Fact]
    public void Construction_SetsAllProperties()
    {
        var update = new EntityUpdate("Economy_Budget", "ProgressPercentage", "75");

        Assert.Equal("Economy_Budget", update.NameInDatabase);
        Assert.Equal("ProgressPercentage", update.FieldName);
        Assert.Equal("75", update.FieldValue);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new EntityUpdate("Entity", "Field", "Value");
        var b = new EntityUpdate("Entity", "Field", "Value");
        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = new EntityUpdate("Entity", "Field", "Value1");
        var b = new EntityUpdate("Entity", "Field", "Value2");
        Assert.NotEqual(a, b);
    }
}
