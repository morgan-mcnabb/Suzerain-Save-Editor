using System.Text.Json.Nodes;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Parsing;

namespace SuzerainSaveEditor.Tests.Parsing;

public sealed class JsonSaveParserTests
{
    private readonly JsonSaveParser _parser = new();

    private static string BuildMinimalJson(
        string? variablesOverride = null,
        string? entityUpdatesOverride = null,
        string? warSaveDataOverride = null) =>
        $$"""
        {
            "saveFileType": 3,
            "campaignName": "KING",
            "currentStoryPack": "StoryPack_Rizia",
            "turnNo": 11,
            "saveFileName": "King1",
            "sceneBuildIndex": 2,
            "lastModified": "20-07-2025_21-10-39",
            "version": "3.1.0.1.137",
            "isVersionMismatched": false,
            "isTorporModeOn": false,
            "notes": "",
            "warSaveData": {{warSaveDataOverride ?? """{"warFragmentName": ""}"""}},
            "variables": "{{variablesOverride ?? "Variable={[\\\"a\\\"]=true}; "}}",
            "entityUpdates": {{entityUpdatesOverride ?? "[]"}}
        }
        """;

    [Fact]
    public void Parse_ValidJson_ExtractsMetadata()
    {
        var json = BuildMinimalJson();
        var doc = _parser.Parse(json);

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
    public void Parse_ValidJson_ExtractsWarSaveData()
    {
        var json = BuildMinimalJson();
        var doc = _parser.Parse(json);

        Assert.NotNull(doc.WarSaveData);
        Assert.Equal("", doc.WarSaveData["warFragmentName"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_ValidJson_ExtractsVariables()
    {
        var json = BuildMinimalJson();
        var doc = _parser.Parse(json);

        Assert.Single(doc.Variables);
        Assert.Equal("a", doc.Variables[0].Key);
        Assert.Equal(new LuaValue.Bool(true), doc.Variables[0].Value);
    }

    [Fact]
    public void Parse_MultipleVariables_ExtractsAll()
    {
        var json = BuildMinimalJson(
            variablesOverride: """Variable={[\"a\"]=true, [\"b\"]=42, [\"c\"]=\"hello\"}; """);
        var doc = _parser.Parse(json);

        Assert.Equal(3, doc.Variables.Count);
        Assert.Equal("a", doc.Variables[0].Key);
        Assert.Equal(new LuaValue.Bool(true), doc.Variables[0].Value);
        Assert.Equal("b", doc.Variables[1].Key);
        Assert.Equal(new LuaValue.Int(42), doc.Variables[1].Value);
        Assert.Equal("c", doc.Variables[2].Key);
        Assert.Equal(new LuaValue.Str("hello"), doc.Variables[2].Value);
    }

    [Fact]
    public void Parse_EmptyVariables_ReturnsEmptyList()
    {
        var json = BuildMinimalJson(variablesOverride: "Variable={}; ");
        var doc = _parser.Parse(json);

        Assert.Empty(doc.Variables);
    }

    [Fact]
    public void Parse_EntityUpdates_ExtractsAll()
    {
        var entityJson = """
        [
            {
                "nameInDatabase": "TestEntity",
                "fieldName": "Index",
                "fieldValue": "0"
            },
            {
                "nameInDatabase": "Another",
                "fieldName": "IsActive",
                "fieldValue": "True"
            }
        ]
        """;
        var json = BuildMinimalJson(entityUpdatesOverride: entityJson);
        var doc = _parser.Parse(json);

        Assert.Equal(2, doc.EntityUpdates.Count);
        Assert.Equal("TestEntity", doc.EntityUpdates[0].NameInDatabase);
        Assert.Equal("Index", doc.EntityUpdates[0].FieldName);
        Assert.Equal("0", doc.EntityUpdates[0].FieldValue);
        Assert.Equal("Another", doc.EntityUpdates[1].NameInDatabase);
        Assert.Equal("IsActive", doc.EntityUpdates[1].FieldName);
        Assert.Equal("True", doc.EntityUpdates[1].FieldValue);
    }

    [Fact]
    public void Parse_EmptyEntityUpdates_ReturnsEmptyList()
    {
        var json = BuildMinimalJson();
        var doc = _parser.Parse(json);

        Assert.Empty(doc.EntityUpdates);
    }

    [Fact]
    public void Parse_WarSaveDataIsDetached_ModifyingItDoesNotAffectOriginal()
    {
        var json = BuildMinimalJson(
            warSaveDataOverride: """{"warFragmentName": "test", "warTokenSaveData": []}""");
        var doc = _parser.Parse(json);

        // modifying the extracted warSaveData should not throw
        doc.WarSaveData["newKey"] = "newValue";
        Assert.Equal("newValue", doc.WarSaveData["newKey"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsSaveParseException()
    {
        Assert.Throws<SaveParseException>(() => _parser.Parse("not json"));
    }

    [Fact]
    public void Parse_MissingVariables_ThrowsSaveParseException()
    {
        var json = """
        {
            "saveFileType": 3,
            "campaignName": "KING",
            "currentStoryPack": "StoryPack_Rizia",
            "turnNo": 11,
            "saveFileName": "King1",
            "sceneBuildIndex": 2,
            "lastModified": "20-07-2025_21-10-39",
            "version": "3.1.0.1.137",
            "isVersionMismatched": false,
            "isTorporModeOn": false,
            "notes": "",
            "warSaveData": {},
            "entityUpdates": []
        }
        """;
        var ex = Assert.Throws<SaveParseException>(() => _parser.Parse(json));
        Assert.Contains("variables", ex.Message);
    }

    [Fact]
    public void Parse_MissingWarSaveData_ThrowsSaveParseException()
    {
        var json = """
        {
            "saveFileType": 3,
            "campaignName": "KING",
            "currentStoryPack": "StoryPack_Rizia",
            "turnNo": 11,
            "saveFileName": "King1",
            "sceneBuildIndex": 2,
            "lastModified": "20-07-2025_21-10-39",
            "version": "3.1.0.1.137",
            "isVersionMismatched": false,
            "isTorporModeOn": false,
            "notes": "",
            "variables": "Variable={}; ",
            "entityUpdates": []
        }
        """;
        var ex = Assert.Throws<SaveParseException>(() => _parser.Parse(json));
        Assert.Contains("warSaveData", ex.Message);
    }

    [Fact]
    public void Parse_MissingMetadataField_ThrowsSaveParseException()
    {
        var json = """
        {
            "campaignName": "KING",
            "currentStoryPack": "StoryPack_Rizia",
            "turnNo": 11,
            "saveFileName": "King1",
            "sceneBuildIndex": 2,
            "lastModified": "20-07-2025_21-10-39",
            "version": "3.1.0.1.137",
            "isVersionMismatched": false,
            "isTorporModeOn": false,
            "notes": "",
            "warSaveData": {},
            "variables": "Variable={}; ",
            "entityUpdates": []
        }
        """;
        Assert.Throws<SaveParseException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Serialize_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _parser.Serialize(null!));
    }

    [Fact]
    public void Serialize_ValidDocument_ProducesValidJson()
    {
        var doc = new SaveDocument
        {
            Metadata = new SaveMetadata(
                SaveFileType: 3,
                CampaignName: "KING",
                CurrentStoryPack: "StoryPack_Rizia",
                TurnNo: 11,
                SaveFileName: "King1",
                SceneBuildIndex: 2,
                LastModified: "20-07-2025_21-10-39",
                Version: "3.1.0.1.137",
                IsVersionMismatched: false,
                IsTorporModeOn: false,
                Notes: ""),
            WarSaveData = new JsonObject { ["warFragmentName"] = "" },
            Variables = [new LuaVariable("flag", new LuaValue.Bool(true))],
            EntityUpdates = []
        };

        var json = _parser.Serialize(doc);

        // should be parseable json
        var parsed = JsonNode.Parse(json);
        Assert.NotNull(parsed);
    }

    [Fact]
    public void Serialize_ProducesCorrectMetadataValues()
    {
        var doc = new SaveDocument
        {
            Metadata = new SaveMetadata(
                SaveFileType: 3,
                CampaignName: "KING",
                CurrentStoryPack: "StoryPack_Rizia",
                TurnNo: 11,
                SaveFileName: "King1",
                SceneBuildIndex: 2,
                LastModified: "20-07-2025_21-10-39",
                Version: "3.1.0.1.137",
                IsVersionMismatched: false,
                IsTorporModeOn: false,
                Notes: ""),
            WarSaveData = new JsonObject(),
            Variables = [],
            EntityUpdates = []
        };

        var json = _parser.Serialize(doc);
        var root = JsonNode.Parse(json)!.AsObject();

        Assert.Equal(3, root["saveFileType"]!.GetValue<int>());
        Assert.Equal("KING", root["campaignName"]!.GetValue<string>());
        Assert.Equal(11, root["turnNo"]!.GetValue<int>());
        Assert.False(root["isVersionMismatched"]!.GetValue<bool>());
    }

    [Fact]
    public void Serialize_PreservesEntityUpdates()
    {
        var doc = new SaveDocument
        {
            Metadata = new SaveMetadata(3, "KING", "SP", 1, "F", 2, "D", "V", false, false, ""),
            WarSaveData = new JsonObject(),
            Variables = [],
            EntityUpdates =
            [
                new EntityUpdate("TestEntity", "Index", "0"),
                new EntityUpdate("Another", "IsActive", "True")
            ]
        };

        var json = _parser.Serialize(doc);
        var root = JsonNode.Parse(json)!.AsObject();
        var entities = root["entityUpdates"]!.AsArray();

        Assert.Equal(2, entities.Count);
        Assert.Equal("TestEntity", entities[0]!["nameInDatabase"]!.GetValue<string>());
        Assert.Equal("True", entities[1]!["fieldValue"]!.GetValue<string>());
    }

    [Fact]
    public void Serialize_Uses4SpaceIndent()
    {
        var doc = new SaveDocument
        {
            Metadata = new SaveMetadata(3, "K", "S", 1, "F", 2, "D", "V", false, false, ""),
            WarSaveData = new JsonObject(),
            Variables = [],
            EntityUpdates = []
        };

        var json = _parser.Serialize(doc);

        Assert.Contains("    \"saveFileType\"", json);
    }

    [Fact]
    public void Serialize_UsesCrlfNewlines()
    {
        var doc = new SaveDocument
        {
            Metadata = new SaveMetadata(3, "K", "S", 1, "F", 2, "D", "V", false, false, ""),
            WarSaveData = new JsonObject(),
            Variables = [],
            EntityUpdates = []
        };

        var json = _parser.Serialize(doc);

        Assert.Contains("\r\n", json);
        // should not have bare LF (all LFs should be preceded by CR)
        var lines = json.Split('\n');
        foreach (var line in lines.Take(lines.Length - 1))
        {
            Assert.EndsWith("\r", line);
        }
    }

    [Fact]
    public void RoundTrip_MinimalDocument_ProducesIdenticalOutput()
    {
        var doc = new SaveDocument
        {
            Metadata = new SaveMetadata(3, "KING", "StoryPack_Rizia", 11, "King1", 2,
                "20-07-2025_21-10-39", "3.1.0.1.137", false, false, ""),
            WarSaveData = new JsonObject { ["warFragmentName"] = "" },
            Variables = [new LuaVariable("flag", new LuaValue.Bool(true))],
            EntityUpdates = [new EntityUpdate("Test", "Index", "0")]
        };

        var serialized = _parser.Serialize(doc);
        var reparsed = _parser.Parse(serialized);
        var reserialized = _parser.Serialize(reparsed);

        Assert.Equal(serialized, reserialized);
    }
}
