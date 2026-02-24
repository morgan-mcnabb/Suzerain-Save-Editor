using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Parsing;

namespace SuzerainSaveEditor.Tests.Parsing;

public sealed class LuaTableParserTests
{
    [Fact]
    public void Parse_EmptyTable_ReturnsEmptyList()
    {
        var result = LuaTableParser.Parse("Variable={}; ");

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_SingleBoolTrue_ReturnsSingleVariable()
    {
        var result = LuaTableParser.Parse("Variable={[\"key\"]=true}; ");

        Assert.Single(result);
        Assert.Equal("key", result[0].Key);
        Assert.Equal(new LuaValue.Bool(true), result[0].Value);
    }

    [Fact]
    public void Parse_SingleBoolFalse_ReturnsSingleVariable()
    {
        var result = LuaTableParser.Parse("Variable={[\"key\"]=false}; ");

        Assert.Single(result);
        Assert.Equal(new LuaValue.Bool(false), result[0].Value);
    }

    [Fact]
    public void Parse_PositiveInteger_ReturnsIntValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"budget\"]=38}; ");

        Assert.Single(result);
        Assert.Equal(new LuaValue.Int(38), result[0].Value);
    }

    [Fact]
    public void Parse_NegativeInteger_ReturnsNegativeIntValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"score\"]=-250}; ");

        Assert.Single(result);
        Assert.Equal(new LuaValue.Int(-250), result[0].Value);
    }

    [Fact]
    public void Parse_ZeroInteger_ReturnsZeroIntValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"count\"]=0}; ");

        Assert.Single(result);
        Assert.Equal(new LuaValue.Int(0), result[0].Value);
    }

    [Fact]
    public void Parse_SimpleString_ReturnsStrValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"name\"]=\"hello\"}; ");

        Assert.Single(result);
        Assert.Equal(new LuaValue.Str("hello"), result[0].Value);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmptyStrValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"name\"]=\"\"}; ");

        Assert.Single(result);
        Assert.Equal(new LuaValue.Str(""), result[0].Value);
    }

    [Fact]
    public void Parse_StringWithCommas_PreservesCommas()
    {
        var result = LuaTableParser.Parse(
            "Variable={[\"allies\"]=\"Pales, Derdia, Wehlen and Morella\"}; ");

        Assert.Single(result);
        Assert.Equal(new LuaValue.Str("Pales, Derdia, Wehlen and Morella"), result[0].Value);
    }

    [Fact]
    public void Parse_StringWithPeriods_PreservesPeriods()
    {
        var result = LuaTableParser.Parse(
            "Variable={[\"version\"]=\"3.1.0.1.137\"}; ");

        Assert.Single(result);
        Assert.Equal(new LuaValue.Str("3.1.0.1.137"), result[0].Value);
    }

    [Fact]
    public void Parse_KeyContainingDoubleEquals_ParsesCorrectly()
    {
        var result = LuaTableParser.Parse(
            "Variable={[\"RiziaDLCSupport.News_Floating_V_TopGuardFlightAcademy_==_true\"]=true}; ");

        Assert.Single(result);
        Assert.Equal("RiziaDLCSupport.News_Floating_V_TopGuardFlightAcademy_==_true", result[0].Key);
        Assert.Equal(new LuaValue.Bool(true), result[0].Value);
    }

    [Fact]
    public void Parse_MultipleVariables_ReturnsAllInOrder()
    {
        var input = "Variable={[\"a\"]=true, [\"b\"]=42, [\"c\"]=\"hello\"}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Equal(3, result.Count);
        Assert.Equal("a", result[0].Key);
        Assert.Equal(new LuaValue.Bool(true), result[0].Value);
        Assert.Equal("b", result[1].Key);
        Assert.Equal(new LuaValue.Int(42), result[1].Value);
        Assert.Equal("c", result[2].Key);
        Assert.Equal(new LuaValue.Str("hello"), result[2].Value);
    }

    [Fact]
    public void Parse_PreservesInsertionOrder()
    {
        var input = "Variable={[\"third\"]=3, [\"first\"]=1, [\"second\"]=2}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Equal("third", result[0].Key);
        Assert.Equal("first", result[1].Key);
        Assert.Equal("second", result[2].Key);
    }

    [Fact]
    public void Parse_MultipleKeysWithDoubleEquals_AllParsed()
    {
        var input = "Variable={[\"a_==_true\"]=true, [\"b_==_false\"]=false}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Equal(2, result.Count);
        Assert.Equal("a_==_true", result[0].Key);
        Assert.Equal("b_==_false", result[1].Key);
    }

    [Fact]
    public void Parse_DottedNamespaceKeys_ParsesCorrectly()
    {
        var input = "Variable={[\"BaseGame.GovernmentBudget\"]=38, [\"BaseGame.Economy\"]=24}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Equal(2, result.Count);
        Assert.Equal("BaseGame.GovernmentBudget", result[0].Key);
        Assert.Equal(new LuaValue.Int(38), result[0].Value);
    }

    [Fact]
    public void Parse_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LuaTableParser.Parse(null!));
    }

    [Fact]
    public void Parse_MissingPrefix_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LuaTableParser.Parse("[\"key\"]=true}; "));
    }

    [Fact]
    public void Parse_MissingSuffix_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LuaTableParser.Parse("Variable={[\"key\"]=true}"));
    }

    [Fact]
    public void Parse_TruncatedKey_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LuaTableParser.Parse("Variable={[\"key}; "));
    }

    [Fact]
    public void Parse_TruncatedStringValue_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LuaTableParser.Parse("Variable={[\"key\"]=\"unterminated}; "));
    }

    [Fact]
    public void Parse_InvalidValueChar_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LuaTableParser.Parse("Variable={[\"key\"]=xyz}; "));
    }

    [Fact]
    public void Parse_MixedValueTypes_AllParsedCorrectly()
    {
        var input = "Variable={[\"flag\"]=true, [\"count\"]=7, [\"neg\"]=-1, [\"name\"]=\"test\", [\"empty\"]=\"\", [\"off\"]=false}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Equal(6, result.Count);
        Assert.Equal(new LuaValue.Bool(true), result[0].Value);
        Assert.Equal(new LuaValue.Int(7), result[1].Value);
        Assert.Equal(new LuaValue.Int(-1), result[2].Value);
        Assert.Equal(new LuaValue.Str("test"), result[3].Value);
        Assert.Equal(new LuaValue.Str(""), result[4].Value);
        Assert.Equal(new LuaValue.Bool(false), result[5].Value);
    }

    [Fact]
    public void Parse_ScientificNotation_Positive_ReturnsNumValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"big\"]=1E+09}; ");

        Assert.Single(result);
        var num = Assert.IsType<LuaValue.Num>(result[0].Value);
        Assert.Equal("1E+09", num.Raw);
        Assert.Equal(1_000_000_000, num.NumericValue);
    }

    [Fact]
    public void Parse_ScientificNotation_Negative_ReturnsNumValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"sentinel\"]=-1E+09}; ");

        Assert.Single(result);
        var num = Assert.IsType<LuaValue.Num>(result[0].Value);
        Assert.Equal("-1E+09", num.Raw);
        Assert.Equal(-1_000_000_000, num.NumericValue);
    }

    [Fact]
    public void Parse_ScientificNotation_LowercaseE_ReturnsNumValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"val\"]=5e+03}; ");

        Assert.Single(result);
        var num = Assert.IsType<LuaValue.Num>(result[0].Value);
        Assert.Equal("5e+03", num.Raw);
        Assert.Equal(5000, num.NumericValue);
    }

    [Fact]
    public void Parse_ScientificNotation_NegativeExponent_ReturnsNumValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"tiny\"]=1E-03}; ");

        Assert.Single(result);
        var num = Assert.IsType<LuaValue.Num>(result[0].Value);
        Assert.Equal("1E-03", num.Raw);
        Assert.Equal(0.001, num.NumericValue, 6);
    }

    [Fact]
    public void Parse_ScientificNotation_PreservesRawFormat()
    {
        var result = LuaTableParser.Parse("Variable={[\"x\"]=-1E+09}; ");

        var num = Assert.IsType<LuaValue.Num>(result[0].Value);
        Assert.Equal("-1E+09", num.ToLuaString());
    }

    [Fact]
    public void Parse_ScientificNotation_MixedWithOtherTypes()
    {
        var input = "Variable={[\"flag\"]=true, [\"big\"]=-1E+09, [\"count\"]=5}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Equal(3, result.Count);
        Assert.IsType<LuaValue.Bool>(result[0].Value);
        Assert.IsType<LuaValue.Num>(result[1].Value);
        Assert.IsType<LuaValue.Int>(result[2].Value);
    }


    [Fact]
    public void Parse_StringWithEscapedQuote_ParsesCorrectly()
    {
        var input = "Variable={[\"msg\"]=\"say \\\"hello\\\"\"}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Single(result);
        Assert.Equal(new LuaValue.Str("say \"hello\""), result[0].Value);
    }

    [Fact]
    public void Parse_StringWithEscapedBackslash_ParsesCorrectly()
    {
        var input = "Variable={[\"path\"]=\"a\\\\b\"}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Single(result);
        Assert.Equal(new LuaValue.Str("a\\b"), result[0].Value);
    }

    [Fact]
    public void Parse_StringWithEscapedCarriageReturn_ParsesCorrectly()
    {
        var input = "Variable={[\"msg\"]=\"line1\\rline2\"}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Single(result);
        Assert.Equal(new LuaValue.Str("line1\rline2"), result[0].Value);
    }

    [Fact]
    public void Parse_StringWithEscapedTab_ParsesCorrectly()
    {
        var input = "Variable={[\"msg\"]=\"col1\\tcol2\"}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Single(result);
        Assert.Equal(new LuaValue.Str("col1\tcol2"), result[0].Value);
    }

    [Fact]
    public void Parse_StringWithEscapedNull_ParsesCorrectly()
    {
        var input = "Variable={[\"msg\"]=\"before\\0after\"}; ";
        var result = LuaTableParser.Parse(input);

        Assert.Single(result);
        Assert.Equal(new LuaValue.Str("before\0after"), result[0].Value);
    }

    [Fact]
    public void RoundTrip_StringWithCarriageReturn_PreservesValue()
    {
        var original = new List<LuaVariable>
        {
            new("msg", new LuaValue.Str("line1\rline2"))
        };
        var serialized = LuaTableSerializer.Serialize(original);
        var parsed = LuaTableParser.Parse(serialized);

        Assert.Single(parsed);
        Assert.Equal("line1\rline2", ((LuaValue.Str)parsed[0].Value).Value);
    }

    [Fact]
    public void RoundTrip_StringWithQuotes_PreservesValue()
    {
        var original = new List<LuaVariable>
        {
            new("msg", new LuaValue.Str("say \"hello\""))
        };
        var serialized = LuaTableSerializer.Serialize(original);
        var parsed = LuaTableParser.Parse(serialized);

        Assert.Single(parsed);
        Assert.Equal("say \"hello\"", ((LuaValue.Str)parsed[0].Value).Value);
    }

    // --- decimal numbers (bug 6) ---

    [Fact]
    public void Parse_DecimalNumber_ReturnsNumValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"pi\"]=3.14}; ");

        Assert.Single(result);
        var num = Assert.IsType<LuaValue.Num>(result[0].Value);
        Assert.Equal("3.14", num.Raw);
        Assert.Equal(3.14, num.NumericValue, 6);
    }

    [Fact]
    public void Parse_NegativeDecimal_ReturnsNumValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"neg\"]=-0.5}; ");

        Assert.Single(result);
        var num = Assert.IsType<LuaValue.Num>(result[0].Value);
        Assert.Equal("-0.5", num.Raw);
    }

    [Fact]
    public void Parse_DecimalWithScientificNotation_ReturnsNumValue()
    {
        var result = LuaTableParser.Parse("Variable={[\"x\"]=1.5e2}; ");

        Assert.Single(result);
        var num = Assert.IsType<LuaValue.Num>(result[0].Value);
        Assert.Equal("1.5e2", num.Raw);
        Assert.Equal(150.0, num.NumericValue, 6);
    }


    [Fact]
    public void Parse_KeyEndAtVeryEndOfBody_ParsesCorrectly()
    {
        // the "]=true sequence ends at the very last positions of the body
        var result = LuaTableParser.Parse("Variable={[\"k\"]=true}; ");

        Assert.Single(result);
        Assert.Equal("k", result[0].Key);
    }


    [Fact]
    public void Parse_LoneMinus_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LuaTableParser.Parse("Variable={[\"x\"]=-}; "));
    }

    [Fact]
    public void Parse_ScientificNotation_NoExponentDigits_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LuaTableParser.Parse("Variable={[\"x\"]=123E}; "));
    }

    [Fact]
    public void Parse_ScientificNotation_SignButNoExponentDigits_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LuaTableParser.Parse("Variable={[\"x\"]=123E+}; "));
    }

    [Fact]
    public void Parse_ScientificNotation_LowercaseNoDigits_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LuaTableParser.Parse("Variable={[\"x\"]=5e}; "));
    }
}
