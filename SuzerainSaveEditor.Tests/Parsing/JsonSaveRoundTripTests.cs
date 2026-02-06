using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Parsing;

namespace SuzerainSaveEditor.Tests.Parsing;

public sealed class JsonSaveRoundTripTests
{
    private readonly JsonSaveParser _parser = new();

    private static string GetExampleSaveFilePath()
    {
        // walk up from bin/Debug/net10.0 to find the repo root
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "example_save-file.json")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.NotNull(dir);
        return Path.Combine(dir!, "example_save-file.json");
    }

    private static string ReadSaveFile()
    {
        return File.ReadAllText(GetExampleSaveFilePath());
    }

    [Fact]
    public void Parse_ExampleSaveFile_ExtractsCorrectMetadata()
    {
        var doc = _parser.Parse(ReadSaveFile());

        Assert.Equal(3, doc.Metadata.SaveFileType);
        Assert.Equal("KING", doc.Metadata.CampaignName);
        Assert.Equal("StoryPack_Rizia", doc.Metadata.CurrentStoryPack);
        Assert.Equal(11, doc.Metadata.TurnNo);
        Assert.Equal("King1", doc.Metadata.SaveFileName);
        Assert.Equal(2, doc.Metadata.SceneBuildIndex);
        Assert.Equal("20-07-2025_21-10-39", doc.Metadata.LastModified);
        Assert.Equal("3.1.0.1.137", doc.Metadata.Version);
        Assert.False(doc.Metadata.IsVersionMismatched);
        Assert.False(doc.Metadata.IsTorporModeOn);
        Assert.Equal("", doc.Metadata.Notes);
    }

    [Fact]
    public void Parse_ExampleSaveFile_Extracts12793Variables()
    {
        var doc = _parser.Parse(ReadSaveFile());

        Assert.Equal(12793, doc.Variables.Count);
    }

    [Fact]
    public void Parse_ExampleSaveFile_Extracts2636EntityUpdates()
    {
        var doc = _parser.Parse(ReadSaveFile());

        Assert.Equal(2636, doc.EntityUpdates.Count);
    }

    [Fact]
    public void Parse_ExampleSaveFile_ExtractsWarSaveData()
    {
        var doc = _parser.Parse(ReadSaveFile());

        Assert.NotNull(doc.WarSaveData);
        Assert.Equal("", doc.WarSaveData["warFragmentName"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_ExampleSaveFile_ContainsKnownVariable()
    {
        var doc = _parser.Parse(ReadSaveFile());

        var variable = doc.Variables.FirstOrDefault(v => v.Key == "BaseGameSetup.CurrentTurn");
        Assert.NotNull(variable);
        Assert.Equal(new LuaValue.Int(11), variable.Value);
    }

    [Fact]
    public void Parse_ExampleSaveFile_ContainsKnownEntityUpdate()
    {
        var doc = _parser.Parse(ReadSaveFile());

        var entity = doc.EntityUpdates.FirstOrDefault(e =>
            e.NameInDatabase == "Prologue_Summer_Court" && e.FieldName == "Index");
        Assert.NotNull(entity);
        Assert.Equal("0", entity.FieldValue);
    }

    [Fact]
    public void Parse_ExampleSaveFile_FirstEntityIsCorrect()
    {
        var doc = _parser.Parse(ReadSaveFile());

        Assert.Equal("Prologue_Summer_Court", doc.EntityUpdates[0].NameInDatabase);
        Assert.Equal("Index", doc.EntityUpdates[0].FieldName);
        Assert.Equal("0", doc.EntityUpdates[0].FieldValue);
    }

    [Fact]
    public void RoundTrip_ExampleSaveFile_ProducesByteIdenticalOutput()
    {
        var original = ReadSaveFile();
        var doc = _parser.Parse(original);
        var serialized = _parser.Serialize(doc);

        Assert.Equal(original, serialized);
    }

    [Fact]
    public void RoundTrip_ExampleSaveFile_DoubleRoundTrip()
    {
        var original = ReadSaveFile();
        var doc1 = _parser.Parse(original);
        var serialized1 = _parser.Serialize(doc1);
        var doc2 = _parser.Parse(serialized1);
        var serialized2 = _parser.Serialize(doc2);

        Assert.Equal(original, serialized2);
    }
}
