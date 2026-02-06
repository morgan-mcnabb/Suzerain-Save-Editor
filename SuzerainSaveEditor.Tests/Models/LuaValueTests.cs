using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Tests.Models;

public sealed class LuaValueTests
{
    [Fact]
    public void Bool_True_ToLuaString_ReturnsTrue()
    {
        var value = new LuaValue.Bool(true);
        Assert.Equal("true", value.ToLuaString());
    }

    [Fact]
    public void Bool_False_ToLuaString_ReturnsFalse()
    {
        var value = new LuaValue.Bool(false);
        Assert.Equal("false", value.ToLuaString());
    }

    [Fact]
    public void Int_Positive_ToLuaString_ReturnsNumber()
    {
        var value = new LuaValue.Int(38);
        Assert.Equal("38", value.ToLuaString());
    }

    [Fact]
    public void Int_Negative_ToLuaString_ReturnsNegativeNumber()
    {
        var value = new LuaValue.Int(-250);
        Assert.Equal("-250", value.ToLuaString());
    }

    [Fact]
    public void Int_Zero_ToLuaString_ReturnsZero()
    {
        var value = new LuaValue.Int(0);
        Assert.Equal("0", value.ToLuaString());
    }

    [Fact]
    public void Str_Simple_ToLuaString_ReturnsQuotedString()
    {
        var value = new LuaValue.Str("hello");
        Assert.Equal("\"hello\"", value.ToLuaString());
    }

    [Fact]
    public void Str_WithCommas_ToLuaString_PreservesCommas()
    {
        var value = new LuaValue.Str("Pales, Derdia, Wehlen and Morella");
        Assert.Equal("\"Pales, Derdia, Wehlen and Morella\"", value.ToLuaString());
    }

    [Fact]
    public void Str_Empty_ToLuaString_ReturnsEmptyQuotedString()
    {
        var value = new LuaValue.Str("");
        Assert.Equal("\"\"", value.ToLuaString());
    }

    [Fact]
    public void Bool_RecordEquality_Works()
    {
        var a = new LuaValue.Bool(true);
        var b = new LuaValue.Bool(true);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Bool_RecordInequality_Works()
    {
        var a = new LuaValue.Bool(true);
        var b = new LuaValue.Bool(false);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Int_RecordEquality_Works()
    {
        var a = new LuaValue.Int(42);
        var b = new LuaValue.Int(42);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Str_RecordEquality_Works()
    {
        var a = new LuaValue.Str("test");
        var b = new LuaValue.Str("test");
        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentSubtypes_AreNotEqual()
    {
        LuaValue a = new LuaValue.Bool(true);
        LuaValue b = new LuaValue.Int(1);
        Assert.NotEqual(a, b);
    }
}
