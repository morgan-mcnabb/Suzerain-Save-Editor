using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Parsing;

namespace SuzerainSaveEditor.Tests.Parsing;

public sealed class LuaTableSerializerTests
{
    [Fact]
    public void Serialize_EmptyList_ReturnsEmptyTable()
    {
        var result = LuaTableSerializer.Serialize([]);

        Assert.Equal("Variable={}; ", result);
    }

    [Fact]
    public void Serialize_SingleBool_ProducesCorrectFormat()
    {
        var variables = new List<LuaVariable>
        {
            new("key", new LuaValue.Bool(true))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"key\"]=true}; ", result);
    }

    [Fact]
    public void Serialize_SingleInt_ProducesCorrectFormat()
    {
        var variables = new List<LuaVariable>
        {
            new("budget", new LuaValue.Int(38))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"budget\"]=38}; ", result);
    }

    [Fact]
    public void Serialize_NegativeInt_ProducesCorrectFormat()
    {
        var variables = new List<LuaVariable>
        {
            new("score", new LuaValue.Int(-250))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"score\"]=-250}; ", result);
    }

    [Fact]
    public void Serialize_SingleString_ProducesCorrectFormat()
    {
        var variables = new List<LuaVariable>
        {
            new("name", new LuaValue.Str("hello"))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"name\"]=\"hello\"}; ", result);
    }

    [Fact]
    public void Serialize_EmptyString_ProducesCorrectFormat()
    {
        var variables = new List<LuaVariable>
        {
            new("name", new LuaValue.Str(""))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"name\"]=\"\"}; ", result);
    }

    [Fact]
    public void Serialize_MultipleVariables_SeparatedByCommaSpace()
    {
        var variables = new List<LuaVariable>
        {
            new("a", new LuaValue.Bool(true)),
            new("b", new LuaValue.Int(42)),
            new("c", new LuaValue.Str("hello"))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"a\"]=true, [\"b\"]=42, [\"c\"]=\"hello\"}; ", result);
    }

    [Fact]
    public void Serialize_PreservesKeyOrder()
    {
        var variables = new List<LuaVariable>
        {
            new("third", new LuaValue.Int(3)),
            new("first", new LuaValue.Int(1)),
            new("second", new LuaValue.Int(2))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"third\"]=3, [\"first\"]=1, [\"second\"]=2}; ", result);
    }

    [Fact]
    public void Serialize_KeyWithDoubleEquals_PreservesKey()
    {
        var variables = new List<LuaVariable>
        {
            new("Support.News_==_true", new LuaValue.Bool(true))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"Support.News_==_true\"]=true}; ", result);
    }

    [Fact]
    public void Serialize_StringWithCommas_PreservesCommas()
    {
        var variables = new List<LuaVariable>
        {
            new("allies", new LuaValue.Str("Pales, Derdia, Wehlen"))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"allies\"]=\"Pales, Derdia, Wehlen\"}; ", result);
    }

    [Fact]
    public void Serialize_KeyWithBackslash_EscapesBackslash()
    {
        var variables = new List<LuaVariable>
        {
            new("path\\to\\key", new LuaValue.Bool(true))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"path\\\\to\\\\key\"]=true}; ", result);
    }

    [Fact]
    public void Serialize_KeyWithQuote_EscapesQuote()
    {
        var variables = new List<LuaVariable>
        {
            new("say\"hello", new LuaValue.Bool(false))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"say\\\"hello\"]=false}; ", result);
    }

    [Fact]
    public void Serialize_KeyWithCloseSequence_EscapesCorrectly()
    {
        var variables = new List<LuaVariable>
        {
            new("bad\"]=inject", new LuaValue.Int(1))
        };

        var result = LuaTableSerializer.Serialize(variables);

        Assert.Equal("Variable={[\"bad\\\"]=inject\"]=1}; ", result);
    }

    [Fact]
    public void RoundTrip_KeyWithBackslash_PreservesKey()
    {
        var variables = new List<LuaVariable>
        {
            new("path\\to\\key", new LuaValue.Bool(true))
        };

        var serialized = LuaTableSerializer.Serialize(variables);
        var parsed = LuaTableParser.Parse(serialized);

        Assert.Single(parsed);
        Assert.Equal("path\\to\\key", parsed[0].Key);
    }

    [Fact]
    public void RoundTrip_KeyWithQuote_PreservesKey()
    {
        var variables = new List<LuaVariable>
        {
            new("say\"hello", new LuaValue.Bool(false))
        };

        var serialized = LuaTableSerializer.Serialize(variables);
        var parsed = LuaTableParser.Parse(serialized);

        Assert.Single(parsed);
        Assert.Equal("say\"hello", parsed[0].Key);
    }

    [Fact]
    public void RoundTrip_KeyWithCloseSequence_PreservesKey()
    {
        var variables = new List<LuaVariable>
        {
            new("bad\"]=inject", new LuaValue.Int(1))
        };

        var serialized = LuaTableSerializer.Serialize(variables);
        var parsed = LuaTableParser.Parse(serialized);

        Assert.Single(parsed);
        Assert.Equal("bad\"]=inject", parsed[0].Key);
    }

    [Fact]
    public void Serialize_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LuaTableSerializer.Serialize(null!));
    }

    [Fact]
    public void RoundTrip_SimpleTable_ProducesIdenticalOutput()
    {
        var input = "Variable={[\"a\"]=true, [\"b\"]=42, [\"c\"]=\"hello\"}; ";
        var parsed = LuaTableParser.Parse(input);
        var serialized = LuaTableSerializer.Serialize(parsed);

        Assert.Equal(input, serialized);
    }

    [Fact]
    public void RoundTrip_KeysWithDoubleEquals_ProducesIdenticalOutput()
    {
        var input = "Variable={[\"a_==_true\"]=true, [\"b_==_false\"]=false}; ";
        var parsed = LuaTableParser.Parse(input);
        var serialized = LuaTableSerializer.Serialize(parsed);

        Assert.Equal(input, serialized);
    }

    [Fact]
    public void RoundTrip_StringWithCommas_ProducesIdenticalOutput()
    {
        var input = "Variable={[\"allies\"]=\"Pales, Derdia, Wehlen and Morella\"}; ";
        var parsed = LuaTableParser.Parse(input);
        var serialized = LuaTableSerializer.Serialize(parsed);

        Assert.Equal(input, serialized);
    }

    [Fact]
    public void RoundTrip_MixedTypes_ProducesIdenticalOutput()
    {
        var input = "Variable={[\"flag\"]=true, [\"count\"]=7, [\"neg\"]=-1, [\"name\"]=\"test\", [\"empty\"]=\"\", [\"off\"]=false}; ";
        var parsed = LuaTableParser.Parse(input);
        var serialized = LuaTableSerializer.Serialize(parsed);

        Assert.Equal(input, serialized);
    }
}
