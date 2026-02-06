using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Tests.Models;

public sealed class LuaVariableTests
{
    [Fact]
    public void Construction_SetsKeyAndValue()
    {
        var variable = new LuaVariable("BaseGame.GovernmentBudget", new LuaValue.Int(38));

        Assert.Equal("BaseGame.GovernmentBudget", variable.Key);
        Assert.Equal(new LuaValue.Int(38), variable.Value);
    }

    [Fact]
    public void RecordEquality_SameKeyAndValue_AreEqual()
    {
        var a = new LuaVariable("key", new LuaValue.Bool(true));
        var b = new LuaVariable("key", new LuaValue.Bool(true));
        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentKey_AreNotEqual()
    {
        var a = new LuaVariable("key1", new LuaValue.Bool(true));
        var b = new LuaVariable("key2", new LuaValue.Bool(true));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentValue_AreNotEqual()
    {
        var a = new LuaVariable("key", new LuaValue.Bool(true));
        var b = new LuaVariable("key", new LuaValue.Bool(false));
        Assert.NotEqual(a, b);
    }
}
