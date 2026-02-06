using System.Text.Json.Nodes;
using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Tests.Models;

public sealed class SaveDocumentTests
{
    [Fact]
    public void Construction_WithRequiredProperties_Succeeds()
    {
        var doc = new SaveDocument
        {
            Metadata = new SaveMetadata(3, "KING", "StoryPack_Rizia", 11, "King1", 2,
                "20-07-2025_21-10-39", "3.1.0.1.137", false, false, ""),
            WarSaveData = new JsonObject(),
            Variables =
            [
                new LuaVariable("BaseGame.GovernmentBudget", new LuaValue.Int(38)),
                new LuaVariable("BaseGame.Economy", new LuaValue.Int(24))
            ],
            EntityUpdates =
            [
                new EntityUpdate("Economy_Budget", "ProgressPercentage", "75")
            ]
        };

        Assert.Equal("KING", doc.Metadata.CampaignName);
        Assert.Equal(2, doc.Variables.Count);
        Assert.Single(doc.EntityUpdates);
    }

    [Fact]
    public void Variables_PreservesInsertionOrder()
    {
        var doc = new SaveDocument
        {
            Metadata = new SaveMetadata(3, "KING", "StoryPack_Rizia", 11, "King1", 2,
                "20-07-2025_21-10-39", "3.1.0.1.137", false, false, ""),
            WarSaveData = new JsonObject(),
            Variables =
            [
                new LuaVariable("first", new LuaValue.Bool(true)),
                new LuaVariable("second", new LuaValue.Int(42)),
                new LuaVariable("third", new LuaValue.Str("hello"))
            ],
            EntityUpdates = []
        };

        Assert.Equal("first", doc.Variables[0].Key);
        Assert.Equal("second", doc.Variables[1].Key);
        Assert.Equal("third", doc.Variables[2].Key);
    }

    [Fact]
    public void WarSaveData_PreservesJsonStructure()
    {
        var warData = new JsonObject
        {
            ["warFragmentName"] = "",
            ["warTokenSaveData"] = new JsonArray()
        };

        var doc = new SaveDocument
        {
            Metadata = new SaveMetadata(3, "KING", "StoryPack_Rizia", 11, "King1", 2,
                "20-07-2025_21-10-39", "3.1.0.1.137", false, false, ""),
            WarSaveData = warData,
            Variables = [],
            EntityUpdates = []
        };

        Assert.Equal("", doc.WarSaveData["warFragmentName"]?.GetValue<string>());
        Assert.IsType<JsonArray>(doc.WarSaveData["warTokenSaveData"]);
    }
}
