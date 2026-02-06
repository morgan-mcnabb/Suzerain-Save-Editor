using System.Text.Json.Nodes;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class EditSessionTests
{
    private readonly ISchemaService _schema = new SchemaService();
    private readonly IFieldResolver _resolver = new FieldResolver();

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
            new LuaVariable("BaseGameSetup.CurrentTurn", new LuaValue.Int(5)),
            new LuaVariable("BaseGame.GovernmentBudget", new LuaValue.Int(4)),
            new LuaVariable("BaseGame.Economy_Taxation", new LuaValue.Int(2)),
            new LuaVariable("BaseGame.Economy_EconomicState", new LuaValue.Int(3)),
            new LuaVariable("BaseGame.ConstitutionReform", new LuaValue.Bool(true)),
            new LuaVariable("BaseGame.Impeached", new LuaValue.Bool(false)),
            new LuaVariable("BaseGameText.AlphonsoToPlayer", new LuaValue.Str("Mr. President")),
            new LuaVariable("BaseGame.Opinion_OldGuard", new LuaValue.Int(5)),
            new LuaVariable("BaseGame.Opinion_Reformist", new LuaValue.Int(3)),
            new LuaVariable("BaseGame.Opinion_NFP", new LuaValue.Int(2)),
            new LuaVariable("BaseGame.Opinion_Bluds", new LuaValue.Int(1)),
            new LuaVariable("BaseGame.Opinion_Military", new LuaValue.Int(4)),
            new LuaVariable("BaseGame.Opinion_Oligarchs", new LuaValue.Int(3)),
            new LuaVariable("BaseGame.Relations_Rumburg", new LuaValue.Int(-2)),
            new LuaVariable("BaseGame.Relations_Lespia", new LuaValue.Int(3)),
            new LuaVariable("BaseGame.Relations_Agnolia", new LuaValue.Int(1)),
            new LuaVariable("BaseGame.Relations_Wehlen", new LuaValue.Int(2)),
            new LuaVariable("BaseGame.Relations_Contana", new LuaValue.Int(0)),
            new LuaVariable("BaseGame.Relations_Valgsland", new LuaValue.Int(1)),
            new LuaVariable("BaseGame.Economy_OilPrice", new LuaValue.Int(50)),
            new LuaVariable("BaseGame.Economy_Healthcare_Spending", new LuaValue.Int(3)),
            new LuaVariable("BaseGame.Economy_Education_Spending", new LuaValue.Int(2)),
            new LuaVariable("BaseGame.Economy_LawEnforcement_Spending", new LuaValue.Int(1)),
            new LuaVariable("BaseGame.Economy_Social_Spending", new LuaValue.Int(2)),
            new LuaVariable("BaseGame.Economy_Infrastructure_Spending", new LuaValue.Int(1)),
            new LuaVariable("RiziaDLCSetup.CurrentTurn", new LuaValue.Int(3)),
            new LuaVariable("RiziaDLC.GovernmentBudget", new LuaValue.Int(7)),
            new LuaVariable("RiziaDLC.RoyalTreasury", new LuaValue.Int(5)),
            new LuaVariable("RiziaDLC.Opinion_Nobles", new LuaValue.Int(4)),
            new LuaVariable("RiziaDLC.Opinion_Merchants", new LuaValue.Int(3)),
            new LuaVariable("RiziaDLC.Opinion_Peasants", new LuaValue.Int(2)),
            new LuaVariable("RiziaDLC.Opinion_Clergy", new LuaValue.Int(3)),
            new LuaVariable("RiziaDLC.Opinion_Military", new LuaValue.Int(4)),
            new LuaVariable("RiziaDLC.Relations_Wehlen", new LuaValue.Int(2)),
            new LuaVariable("RiziaDLC.Relations_Lespia", new LuaValue.Int(1)),
            new LuaVariable("RiziaDLC.Relations_Rumburg", new LuaValue.Int(-1)),
            new LuaVariable("RiziaDLCText.KingsSon", new LuaValue.Str("Aldric")),
            new LuaVariable("RiziaDLCText.FinalChapterTitle", new LuaValue.Str("Dawn")),
            new LuaVariable("RiziaDLCText.LucitaTitle", new LuaValue.Str("Advisor")),
            new LuaVariable("RiziaDLCText.TheocracyName", new LuaValue.Str("Holy Order")),
        ],
        EntityUpdates =
        [
            new EntityUpdate("Rizia_Country_Wehlen", "Relations", "2"),
            new EntityUpdate("Rizia_Country_Valgsland", "Relations", "3"),
            new EntityUpdate("Page_HouseOfDelegates", "CurrentComposition", "HODComposition_RNCWins"),
        ]
    };

    private EditSession CreateSession(SaveDocument? doc = null, string? filePath = null)
    {
        return new EditSession(doc ?? CreateTestDocument(), filePath, _schema, _resolver);
    }

    // --- initial state ---

    [Fact]
    public void NewSession_IsDirty_IsFalse()
    {
        var session = CreateSession();
        Assert.False(session.IsDirty);
    }

    [Fact]
    public void NewSession_GetDirtyFields_IsEmpty()
    {
        var session = CreateSession();
        Assert.Empty(session.GetDirtyFields());
    }

    [Fact]
    public void NewSession_CurrentDocument_IsSameAsOriginal()
    {
        var session = CreateSession();
        Assert.Same(session.OriginalDocument, session.CurrentDocument);
    }

    [Fact]
    public void NewSession_FilePath_IsStored()
    {
        var session = CreateSession(filePath: @"C:\saves\test.json");
        Assert.Equal(@"C:\saves\test.json", session.FilePath);
    }

    [Fact]
    public void NewSession_FilePath_CanBeNull()
    {
        var session = CreateSession(filePath: null);
        Assert.Null(session.FilePath);
    }

    // --- get value ---

    [Fact]
    public void GetValue_Variable_ReturnsCurrentValue()
    {
        var session = CreateSession();
        Assert.Equal("4", session.GetValue("sordland.governmentBudget"));
    }

    [Fact]
    public void GetValue_Metadata_ReturnsCurrentValue()
    {
        var session = CreateSession();
        Assert.Equal("KING", session.GetValue("meta.campaignName"));
    }

    [Fact]
    public void GetValue_Entity_ReturnsCurrentValue()
    {
        var session = CreateSession();
        Assert.Equal("2", session.GetValue("rizia.entityWehlenRelations"));
    }

    [Fact]
    public void GetValue_UnknownField_ThrowsKeyNotFoundException()
    {
        var session = CreateSession();
        Assert.Throws<KeyNotFoundException>(() => session.GetValue("nonexistent.field"));
    }

    // --- set value ---

    [Fact]
    public void SetValue_Int_UpdatesCurrentDocument()
    {
        var session = CreateSession();
        var result = session.SetValue("sordland.governmentBudget", "8");

        Assert.True(result.IsValid);
        Assert.Equal("8", session.GetValue("sordland.governmentBudget"));
    }

    [Fact]
    public void SetValue_Bool_UpdatesCurrentDocument()
    {
        var session = CreateSession();
        var result = session.SetValue("sordland.constitutionReform", "False");

        Assert.True(result.IsValid);
        Assert.Equal("False", session.GetValue("sordland.constitutionReform"));
    }

    [Fact]
    public void SetValue_String_UpdatesCurrentDocument()
    {
        var session = CreateSession();
        var result = session.SetValue("sordland.alphonsoTitle", "Your Excellency");

        Assert.True(result.IsValid);
        Assert.Equal("Your Excellency", session.GetValue("sordland.alphonsoTitle"));
    }

    [Fact]
    public void SetValue_Metadata_UpdatesCurrentDocument()
    {
        var session = CreateSession();
        var result = session.SetValue("meta.campaignName", "PRESIDENT");

        Assert.True(result.IsValid);
        Assert.Equal("PRESIDENT", session.GetValue("meta.campaignName"));
    }

    [Fact]
    public void SetValue_Entity_UpdatesCurrentDocument()
    {
        var session = CreateSession();
        var result = session.SetValue("rizia.entityWehlenRelations", "5");

        Assert.True(result.IsValid);
        Assert.Equal("5", session.GetValue("rizia.entityWehlenRelations"));
    }

    [Fact]
    public void SetValue_MakesSessionDirty()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");

        Assert.True(session.IsDirty);
    }

    [Fact]
    public void SetValue_TracksDirtyField()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");

        var dirty = session.GetDirtyFields();
        Assert.Single(dirty);
        Assert.Equal("sordland.governmentBudget", dirty[0].FieldId);
        Assert.Equal("4", dirty[0].OldValue);
        Assert.Equal("8", dirty[0].NewValue);
    }

    [Fact]
    public void SetValue_DoesNotMutateOriginalDocument()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");

        var field = _schema.GetById("sordland.governmentBudget")!;
        Assert.Equal("4", _resolver.ReadValue(session.OriginalDocument, field));
    }

    [Fact]
    public void SetValue_PreservesOtherFields()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");

        Assert.Equal("2", session.GetValue("sordland.economyTaxation"));
        Assert.Equal("KING", session.GetValue("meta.campaignName"));
    }

    [Fact]
    public void SetValue_BackToOriginal_RemovesDirtyState()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");
        Assert.True(session.IsDirty);

        session.SetValue("sordland.governmentBudget", "4");
        Assert.False(session.IsDirty);
        Assert.Empty(session.GetDirtyFields());
    }

    [Fact]
    public void SetValue_MultipleFields_TracksAll()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");
        session.SetValue("sordland.economyTaxation", "5");

        var dirty = session.GetDirtyFields();
        Assert.Equal(2, dirty.Count);
    }

    [Fact]
    public void SetValue_SameFieldTwice_KeepsLatestValue()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");
        session.SetValue("sordland.governmentBudget", "6");

        Assert.Equal("6", session.GetValue("sordland.governmentBudget"));
        var dirty = session.GetDirtyFields();
        Assert.Single(dirty);
        Assert.Equal("6", dirty[0].NewValue);
    }

    [Fact]
    public void SetValue_UnknownField_ThrowsKeyNotFoundException()
    {
        var session = CreateSession();
        Assert.Throws<KeyNotFoundException>(() => session.SetValue("nonexistent.field", "42"));
    }

    // --- validation ---

    [Fact]
    public void SetValue_InvalidInt_ReturnsError()
    {
        var session = CreateSession();
        var result = session.SetValue("sordland.governmentBudget", "abc");

        Assert.False(result.IsValid);
        Assert.Contains("not a valid integer", result.Error);
    }

    [Fact]
    public void SetValue_InvalidBool_ReturnsError()
    {
        var session = CreateSession();
        var result = session.SetValue("sordland.constitutionReform", "maybe");

        Assert.False(result.IsValid);
        Assert.Contains("not a valid boolean", result.Error);
    }

    [Fact]
    public void SetValue_BelowMin_ReturnsError()
    {
        var session = CreateSession();
        var result = session.SetValue("sordland.governmentBudget", "-11");

        Assert.False(result.IsValid);
        Assert.Contains("below minimum", result.Error);
    }

    [Fact]
    public void SetValue_AboveMax_ReturnsError()
    {
        var session = CreateSession();
        var result = session.SetValue("sordland.governmentBudget", "11");

        Assert.False(result.IsValid);
        Assert.Contains("exceeds maximum", result.Error);
    }

    [Fact]
    public void SetValue_AtMin_Succeeds()
    {
        var session = CreateSession();
        var result = session.SetValue("sordland.governmentBudget", "-10");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SetValue_AtMax_Succeeds()
    {
        var session = CreateSession();
        var result = session.SetValue("sordland.governmentBudget", "10");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SetValue_InvalidValue_DoesNotApplyEdit()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "abc");

        Assert.False(session.IsDirty);
        Assert.Equal("4", session.GetValue("sordland.governmentBudget"));
    }

    [Fact]
    public void ValidateField_ValidInt_ReturnsSuccess()
    {
        var session = CreateSession();
        var result = session.ValidateField("sordland.governmentBudget", "5");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateField_InvalidInt_ReturnsError()
    {
        var session = CreateSession();
        var result = session.ValidateField("sordland.governmentBudget", "abc");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateField_UnknownField_ThrowsKeyNotFoundException()
    {
        var session = CreateSession();
        Assert.Throws<KeyNotFoundException>(() => session.ValidateField("nonexistent", "5"));
    }

    [Fact]
    public void ValidateAll_NoEdits_ReturnsSuccess()
    {
        var session = CreateSession();
        Assert.True(session.ValidateAll().IsValid);
    }

    [Fact]
    public void ValidateAll_ValidEdits_ReturnsSuccess()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");
        session.SetValue("meta.campaignName", "PRESIDENT");

        Assert.True(session.ValidateAll().IsValid);
    }

    // --- revert ---

    [Fact]
    public void RevertField_RestoresOriginalValue()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");

        session.RevertField("sordland.governmentBudget");

        Assert.Equal("4", session.GetValue("sordland.governmentBudget"));
        Assert.False(session.IsDirty);
    }

    [Fact]
    public void RevertField_PreservesOtherEdits()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");
        session.SetValue("sordland.economyTaxation", "5");

        session.RevertField("sordland.governmentBudget");

        Assert.True(session.IsDirty);
        Assert.Equal("4", session.GetValue("sordland.governmentBudget"));
        Assert.Equal("5", session.GetValue("sordland.economyTaxation"));
    }

    [Fact]
    public void RevertField_UneditedField_DoesNothing()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");

        session.RevertField("sordland.economyTaxation");

        Assert.True(session.IsDirty);
        Assert.Equal("8", session.GetValue("sordland.governmentBudget"));
    }

    [Fact]
    public void RevertField_UnknownField_ThrowsKeyNotFoundException()
    {
        var session = CreateSession();
        Assert.Throws<KeyNotFoundException>(() => session.RevertField("nonexistent.field"));
    }

    [Fact]
    public void RevertAll_RestoresAllOriginalValues()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");
        session.SetValue("sordland.economyTaxation", "5");
        session.SetValue("meta.campaignName", "PRESIDENT");

        session.RevertAll();

        Assert.False(session.IsDirty);
        Assert.Empty(session.GetDirtyFields());
        Assert.Equal("4", session.GetValue("sordland.governmentBudget"));
        Assert.Equal("2", session.GetValue("sordland.economyTaxation"));
        Assert.Equal("KING", session.GetValue("meta.campaignName"));
    }

    [Fact]
    public void RevertAll_RestoresCurrentDocumentToOriginal()
    {
        var session = CreateSession();
        session.SetValue("sordland.governmentBudget", "8");

        session.RevertAll();

        Assert.Same(session.OriginalDocument, session.CurrentDocument);
    }

    // --- int with no min/max ---

    [Fact]
    public void SetValue_IntWithNoMinMax_AcceptsAnyValidInt()
    {
        var session = CreateSession();
        // oilPrice has no min/max in schema
        var result = session.SetValue("sordland.oilPrice", "9999");

        Assert.True(result.IsValid);
        Assert.Equal("9999", session.GetValue("sordland.oilPrice"));
    }

    // --- bool casing normalization ---

    [Fact]
    public void SetValue_BoolLowercaseSameAsOriginal_NotDirty()
    {
        var session = CreateSession();
        // constitutionReform is true in the test doc, ReadValue returns "True"
        var result = session.SetValue("sordland.constitutionReform", "true");

        Assert.True(result.IsValid);
        Assert.False(session.IsDirty);
    }

    [Fact]
    public void SetValue_BoolLowercaseDifferentFromOriginal_IsDirty()
    {
        var session = CreateSession();
        // constitutionReform is true, so "false" is a real change
        var result = session.SetValue("sordland.constitutionReform", "false");

        Assert.True(result.IsValid);
        Assert.True(session.IsDirty);
    }

    [Fact]
    public void SetValue_MetadataBoolLowercaseSameAsOriginal_NotDirty()
    {
        var session = CreateSession();
        // isTorporModeOn is false in the test doc, ReadValue returns "False"
        var result = session.SetValue("meta.isTorporModeOn", "false");

        Assert.True(result.IsValid);
        Assert.False(session.IsDirty);
    }

    // --- string accepts anything ---

    [Fact]
    public void SetValue_String_AcceptsAnyValue()
    {
        var session = CreateSession();
        var result = session.SetValue("meta.notes", "");

        Assert.True(result.IsValid);
    }
}
