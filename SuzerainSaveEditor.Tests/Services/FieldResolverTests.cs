using System.Text.Json.Nodes;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class FieldResolverTests
{
    private readonly FieldResolver _resolver = new();

    private static SaveDocument CreateTestDocument() => new()
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
            Notes: "test notes"),
        WarSaveData = new JsonObject(),
        Variables =
        [
            new LuaVariable("BaseGame.GovernmentBudget", new LuaValue.Int(4)),
            new LuaVariable("BaseGame.ConstitutionReform", new LuaValue.Bool(true)),
            new LuaVariable("BaseGameText.AlphonsoToPlayer", new LuaValue.Str("Mr. President")),
            new LuaVariable("RiziaDLC.GovernmentBudget", new LuaValue.Int(7)),
        ],
        EntityUpdates =
        [
            new EntityUpdate("Rizia_Country_Wehlen", "Relations", "2"),
            new EntityUpdate("Rizia_Country_Valgsland", "Relations", "3"),
            new EntityUpdate("Page_HouseOfDelegates", "CurrentComposition", "HODComposition_RNCWins"),
            new EntityUpdate("Progress_Survey_ArgnoMining", "ProgressPercentage", "100"),
        ]
    };

    private static FieldDefinition MakeField(
        string id, string path, FieldSource source,
        FieldType type = FieldType.Int, FieldGroup group = FieldGroup.General) => new()
    {
        Id = id,
        Path = path,
        Label = id,
        Group = group,
        Type = type,
        Source = source
    };

    // --- read variable ---

    [Fact]
    public void ReadValue_Variable_Int_ReturnsStringValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "variable:BaseGame.GovernmentBudget", FieldSource.Variable);

        var result = _resolver.ReadValue(doc, field);

        Assert.Equal("4", result);
    }

    [Fact]
    public void ReadValue_Variable_Bool_ReturnsStringValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "variable:BaseGame.ConstitutionReform", FieldSource.Variable, FieldType.Bool);

        var result = _resolver.ReadValue(doc, field);

        Assert.Equal("True", result);
    }

    [Fact]
    public void ReadValue_Variable_String_ReturnsValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "variable:BaseGameText.AlphonsoToPlayer", FieldSource.Variable, FieldType.String);

        var result = _resolver.ReadValue(doc, field);

        Assert.Equal("Mr. President", result);
    }

    [Fact]
    public void ReadValue_Variable_NotFound_ReturnsNull()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "variable:NonExistent.Variable", FieldSource.Variable);

        var result = _resolver.ReadValue(doc, field);

        Assert.Null(result);
    }

    // --- read entity update ---

    [Fact]
    public void ReadValue_Entity_ReturnsFieldValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "entity:Rizia_Country_Wehlen.Relations", FieldSource.EntityUpdate);

        var result = _resolver.ReadValue(doc, field);

        Assert.Equal("2", result);
    }

    [Fact]
    public void ReadValue_Entity_StringValue_ReturnsFieldValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "entity:Page_HouseOfDelegates.CurrentComposition", FieldSource.EntityUpdate, FieldType.String);

        var result = _resolver.ReadValue(doc, field);

        Assert.Equal("HODComposition_RNCWins", result);
    }

    [Fact]
    public void ReadValue_Entity_NotFound_ReturnsNull()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "entity:NonExistent.Relations", FieldSource.EntityUpdate);

        var result = _resolver.ReadValue(doc, field);

        Assert.Null(result);
    }

    // --- read metadata ---

    [Fact]
    public void ReadValue_Metadata_String_ReturnsValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:campaignName", FieldSource.Metadata, FieldType.String);

        var result = _resolver.ReadValue(doc, field);

        Assert.Equal("KING", result);
    }

    [Fact]
    public void ReadValue_Metadata_Int_ReturnsStringValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:turnNo", FieldSource.Metadata);

        var result = _resolver.ReadValue(doc, field);

        Assert.Equal("11", result);
    }

    [Fact]
    public void ReadValue_Metadata_Bool_ReturnsStringValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:isTorporModeOn", FieldSource.Metadata, FieldType.Bool);

        var result = _resolver.ReadValue(doc, field);

        Assert.Equal("False", result);
    }

    [Fact]
    public void ReadValue_Metadata_Notes_ReturnsValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:notes", FieldSource.Metadata, FieldType.String);

        var result = _resolver.ReadValue(doc, field);

        Assert.Equal("test notes", result);
    }

    [Fact]
    public void ReadValue_Metadata_UnknownProperty_ReturnsNull()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:nonExistentProperty", FieldSource.Metadata);

        var result = _resolver.ReadValue(doc, field);

        Assert.Null(result);
    }

    // --- write variable ---

    [Fact]
    public void WriteValue_Variable_Int_UpdatesValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "variable:BaseGame.GovernmentBudget", FieldSource.Variable);

        var result = _resolver.WriteValue(doc, field, "10");

        var readBack = _resolver.ReadValue(result, field);
        Assert.Equal("10", readBack);
    }

    [Fact]
    public void WriteValue_Variable_Bool_UpdatesValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "variable:BaseGame.ConstitutionReform", FieldSource.Variable, FieldType.Bool);

        var result = _resolver.WriteValue(doc, field, "False");

        var readBack = _resolver.ReadValue(result, field);
        Assert.Equal("False", readBack);
    }

    [Fact]
    public void WriteValue_Variable_String_UpdatesValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "variable:BaseGameText.AlphonsoToPlayer", FieldSource.Variable, FieldType.String);

        var result = _resolver.WriteValue(doc, field, "Your Excellency");

        var readBack = _resolver.ReadValue(result, field);
        Assert.Equal("Your Excellency", readBack);
    }

    [Fact]
    public void WriteValue_Variable_PreservesOtherVariables()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "variable:BaseGame.GovernmentBudget", FieldSource.Variable);

        var result = _resolver.WriteValue(doc, field, "10");

        // other variables should be unchanged
        var otherField = MakeField("test2", "variable:RiziaDLC.GovernmentBudget", FieldSource.Variable);
        Assert.Equal("7", _resolver.ReadValue(result, otherField));
    }

    [Fact]
    public void WriteValue_Variable_DoesNotMutateOriginal()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "variable:BaseGame.GovernmentBudget", FieldSource.Variable);

        _resolver.WriteValue(doc, field, "10");

        // original document should be unchanged
        Assert.Equal("4", _resolver.ReadValue(doc, field));
    }

    // --- write entity update ---

    [Fact]
    public void WriteValue_Entity_UpdatesFieldValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "entity:Rizia_Country_Wehlen.Relations", FieldSource.EntityUpdate);

        var result = _resolver.WriteValue(doc, field, "5");

        Assert.Equal("5", _resolver.ReadValue(result, field));
    }

    [Fact]
    public void WriteValue_Entity_PreservesOtherEntities()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "entity:Rizia_Country_Wehlen.Relations", FieldSource.EntityUpdate);

        var result = _resolver.WriteValue(doc, field, "5");

        var otherField = MakeField("test2", "entity:Rizia_Country_Valgsland.Relations", FieldSource.EntityUpdate);
        Assert.Equal("3", _resolver.ReadValue(result, otherField));
    }

    [Fact]
    public void WriteValue_Entity_DoesNotMutateOriginal()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "entity:Rizia_Country_Wehlen.Relations", FieldSource.EntityUpdate);

        _resolver.WriteValue(doc, field, "5");

        Assert.Equal("2", _resolver.ReadValue(doc, field));
    }

    // --- write metadata ---

    [Fact]
    public void WriteValue_Metadata_String_UpdatesValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:campaignName", FieldSource.Metadata, FieldType.String);

        var result = _resolver.WriteValue(doc, field, "PRESIDENT");

        Assert.Equal("PRESIDENT", _resolver.ReadValue(result, field));
    }

    [Fact]
    public void WriteValue_Metadata_Int_UpdatesValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:turnNo", FieldSource.Metadata);

        var result = _resolver.WriteValue(doc, field, "5");

        Assert.Equal("5", _resolver.ReadValue(result, field));
    }

    [Fact]
    public void WriteValue_Metadata_Bool_UpdatesValue()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:isTorporModeOn", FieldSource.Metadata, FieldType.Bool);

        var result = _resolver.WriteValue(doc, field, "True");

        Assert.Equal("True", _resolver.ReadValue(result, field));
    }

    [Fact]
    public void WriteValue_Metadata_DoesNotMutateOriginal()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:campaignName", FieldSource.Metadata, FieldType.String);

        _resolver.WriteValue(doc, field, "PRESIDENT");

        Assert.Equal("KING", _resolver.ReadValue(doc, field));
    }

    [Fact]
    public void WriteValue_Metadata_PreservesOtherFields()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:campaignName", FieldSource.Metadata, FieldType.String);

        var result = _resolver.WriteValue(doc, field, "PRESIDENT");

        var turnField = MakeField("test2", "meta:turnNo", FieldSource.Metadata);
        Assert.Equal("11", _resolver.ReadValue(result, turnField));
    }

    // --- bool normalization ---

    [Fact]
    public void ReadValue_Entity_BoolType_NormalizesToConsistentCasing()
    {
        var doc = new SaveDocument
        {
            Metadata = CreateTestDocument().Metadata,
            WarSaveData = new JsonObject(),
            Variables = [],
            EntityUpdates = [new EntityUpdate("Test_Entity", "IsActive", "true")]
        };
        var field = MakeField("test", "entity:Test_Entity.IsActive", FieldSource.EntityUpdate, FieldType.Bool);

        var result = _resolver.ReadValue(doc, field);

        // lowercase "true" from entity should be normalized to "True"
        Assert.Equal("True", result);
    }

    [Fact]
    public void ReadValue_Entity_NonBoolType_DoesNotNormalize()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "entity:Page_HouseOfDelegates.CurrentComposition", FieldSource.EntityUpdate, FieldType.String);

        var result = _resolver.ReadValue(doc, field);

        Assert.Equal("HODComposition_RNCWins", result);
    }

    // --- error cases ---

    [Fact]
    public void ReadValue_InvalidVariablePath_ThrowsArgumentException()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "invalid:path", FieldSource.Variable);

        Assert.Throws<ArgumentException>(() => _resolver.ReadValue(doc, field));
    }

    [Fact]
    public void ReadValue_InvalidEntityPath_NoDot_ThrowsArgumentException()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "entity:NoDotSeparator", FieldSource.EntityUpdate);

        Assert.Throws<ArgumentException>(() => _resolver.ReadValue(doc, field));
    }

    [Fact]
    public void WriteValue_Metadata_UnknownProperty_ThrowsArgumentException()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "meta:nonExistentProperty", FieldSource.Metadata);

        Assert.Throws<ArgumentException>(() => _resolver.WriteValue(doc, field, "value"));
    }

    [Fact]
    public void WriteValue_Variable_NotFound_ThrowsKeyNotFoundException()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "variable:NonExistent.Variable", FieldSource.Variable);

        Assert.Throws<KeyNotFoundException>(() => _resolver.WriteValue(doc, field, "42"));
    }

    [Fact]
    public void WriteValue_Entity_NotFound_ThrowsKeyNotFoundException()
    {
        var doc = CreateTestDocument();
        var field = MakeField("test", "entity:NonExistent.FieldName", FieldSource.EntityUpdate);

        Assert.Throws<KeyNotFoundException>(() => _resolver.WriteValue(doc, field, "42"));
    }
}
