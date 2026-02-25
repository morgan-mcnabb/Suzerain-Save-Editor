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

    [Fact]
    public void Num_ToLuaString_ReturnsRawValue()
    {
        var value = new LuaValue.Num("-1E+09");
        Assert.Equal("-1E+09", value.ToLuaString());
    }

    [Fact]
    public void Num_NumericValue_ReturnsCorrectDouble()
    {
        var value = new LuaValue.Num("-1E+09");
        Assert.Equal(-1_000_000_000, value.NumericValue);
    }

    [Fact]
    public void Num_RecordEquality_Works()
    {
        var a = new LuaValue.Num("-1E+09");
        var b = new LuaValue.Num("-1E+09");
        Assert.Equal(a, b);
    }


    [Fact]
    public void Str_WithQuotes_ToLuaString_EscapesQuotes()
    {
        var value = new LuaValue.Str("say \"hello\"");
        Assert.Equal("\"say \\\"hello\\\"\"", value.ToLuaString());
    }

    [Fact]
    public void Str_WithBackslash_ToLuaString_EscapesBackslash()
    {
        var value = new LuaValue.Str("path\\to\\file");
        Assert.Equal("\"path\\\\to\\\\file\"", value.ToLuaString());
    }

    [Fact]
    public void Str_WithBackslashAndQuote_ToLuaString_EscapesBoth()
    {
        var value = new LuaValue.Str("a\\\"b");
        Assert.Equal("\"a\\\\\\\"b\"", value.ToLuaString());
    }

    [Fact]
    public void Str_WithNewline_ToLuaString_EscapesNewline()
    {
        var value = new LuaValue.Str("line1\nline2");
        Assert.Equal("\"line1\\nline2\"", value.ToLuaString());
    }

    [Fact]
    public void Str_WithCarriageReturn_ToLuaString_EscapesCarriageReturn()
    {
        var value = new LuaValue.Str("line1\rline2");
        Assert.Equal("\"line1\\rline2\"", value.ToLuaString());
    }

    [Fact]
    public void Str_WithTab_ToLuaString_EscapesTab()
    {
        var value = new LuaValue.Str("col1\tcol2");
        Assert.Equal("\"col1\\tcol2\"", value.ToLuaString());
    }

    [Fact]
    public void Str_WithNull_ToLuaString_EscapesNull()
    {
        var value = new LuaValue.Str("before\0after");
        Assert.Equal("\"before\\0after\"", value.ToLuaString());
    }

    [Fact]
    public void Str_WithAllEscapes_ToLuaString_EscapesAll()
    {
        var value = new LuaValue.Str("a\\b\"c\nd\re\tf\0g");
        Assert.Equal("\"a\\\\b\\\"c\\nd\\re\\tf\\0g\"", value.ToLuaString());
    }
}
