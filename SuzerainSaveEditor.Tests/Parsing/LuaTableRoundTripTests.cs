using System.Text.Json.Nodes;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Parsing;

namespace SuzerainSaveEditor.Tests.Parsing;

public sealed class LuaTableRoundTripTests
{
    private static string GetExampleSaveFilePath()
    {
        // walk up from bin/Debug/net10.0 to find the repo root
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "example_save-file.json")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.NotNull(dir);
        return Path.Combine(dir!, "example_save-file.json");
    }

    private static string ExtractVariablesString(string saveFilePath)
    {
        var json = File.ReadAllText(saveFilePath);
        var root = JsonNode.Parse(json)!.AsObject();
        return root["variables"]!.GetValue<string>();
    }

    [Fact]
    public void Parse_ExampleSaveFile_Returns12793Variables()
    {
        var variablesString = ExtractVariablesString(GetExampleSaveFilePath());
        var result = LuaTableParser.Parse(variablesString);

        Assert.Equal(12793, result.Count);
    }

    [Fact]
    public void Parse_ExampleSaveFile_ContainsBoolValues()
    {
        var variablesString = ExtractVariablesString(GetExampleSaveFilePath());
        var result = LuaTableParser.Parse(variablesString);

        var boolCount = result.Count(v => v.Value is LuaValue.Bool);
        Assert.True(boolCount > 12000, $"Expected >12000 bool values, got {boolCount}");
    }

    [Fact]
    public void Parse_ExampleSaveFile_ContainsIntValues()
    {
        var variablesString = ExtractVariablesString(GetExampleSaveFilePath());
        var result = LuaTableParser.Parse(variablesString);

        var intCount = result.Count(v => v.Value is LuaValue.Int);
        Assert.True(intCount > 400, $"Expected >400 int values, got {intCount}");
    }

    [Fact]
    public void Parse_ExampleSaveFile_ContainsStringValues()
    {
        var variablesString = ExtractVariablesString(GetExampleSaveFilePath());
        var result = LuaTableParser.Parse(variablesString);

        var strCount = result.Count(v => v.Value is LuaValue.Str);
        Assert.True(strCount > 50, $"Expected >50 string values, got {strCount}");
    }

    [Fact]
    public void Parse_ExampleSaveFile_ContainsKnownBoolVariable()
    {
        var variablesString = ExtractVariablesString(GetExampleSaveFilePath());
        var result = LuaTableParser.Parse(variablesString);

        var variable = result.FirstOrDefault(v => v.Key == "GameCondition.Turn01_A_PoliticalOverview");
        Assert.NotNull(variable);
        Assert.Equal(new LuaValue.Bool(false), variable.Value);
    }

    [Fact]
    public void Parse_ExampleSaveFile_ContainsKnownIntVariable()
    {
        var variablesString = ExtractVariablesString(GetExampleSaveFilePath());
        var result = LuaTableParser.Parse(variablesString);

        var variable = result.FirstOrDefault(v => v.Key == "BaseGameSetup.CurrentTurn");
        Assert.NotNull(variable);
        Assert.Equal(new LuaValue.Int(11), variable.Value);
    }

    [Fact]
    public void Parse_ExampleSaveFile_ContainsKeysWithDoubleEquals()
    {
        var variablesString = ExtractVariablesString(GetExampleSaveFilePath());
        var result = LuaTableParser.Parse(variablesString);

        var keysWithDoubleEquals = result.Where(v => v.Key.Contains("==")).ToList();
        Assert.True(keysWithDoubleEquals.Count > 0, "Expected at least one key containing '=='");
    }

    [Fact]
    public void RoundTrip_ExampleSaveFile_ProducesByteIdenticalOutput()
    {
        var variablesString = ExtractVariablesString(GetExampleSaveFilePath());
        var parsed = LuaTableParser.Parse(variablesString);
        var serialized = LuaTableSerializer.Serialize(parsed);

        Assert.Equal(variablesString, serialized);
    }

    [Fact]
    public void Parse_ExampleSaveFile_FirstVariableIsGameCondition()
    {
        var variablesString = ExtractVariablesString(GetExampleSaveFilePath());
        var result = LuaTableParser.Parse(variablesString);

        Assert.StartsWith("GameCondition.", result[0].Key);
    }
}
