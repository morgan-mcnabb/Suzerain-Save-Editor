using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Tests.Models;

public sealed class SaveMetadataTests
{
    [Fact]
    public void Construction_SetsAllProperties()
    {
        var metadata = new SaveMetadata(
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
            Notes: "");

        Assert.Equal(3, metadata.SaveFileType);
        Assert.Equal("KING", metadata.CampaignName);
        Assert.Equal("StoryPack_Rizia", metadata.CurrentStoryPack);
        Assert.Equal(11, metadata.TurnNo);
        Assert.Equal("King1", metadata.SaveFileName);
        Assert.Equal(2, metadata.SceneBuildIndex);
        Assert.Equal("20-07-2025_21-10-39", metadata.LastModified);
        Assert.Equal("3.1.0.1.137", metadata.Version);
        Assert.False(metadata.IsVersionMismatched);
        Assert.False(metadata.IsTorporModeOn);
        Assert.Equal("", metadata.Notes);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new SaveMetadata(3, "KING", "StoryPack_Rizia", 11, "King1", 2,
            "20-07-2025_21-10-39", "3.1.0.1.137", false, false, "");
        var b = new SaveMetadata(3, "KING", "StoryPack_Rizia", 11, "King1", 2,
            "20-07-2025_21-10-39", "3.1.0.1.137", false, false, "");
        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentTurnNo_AreNotEqual()
    {
        var a = new SaveMetadata(3, "KING", "StoryPack_Rizia", 11, "King1", 2,
            "20-07-2025_21-10-39", "3.1.0.1.137", false, false, "");
        var b = new SaveMetadata(3, "KING", "StoryPack_Rizia", 5, "King1", 2,
            "20-07-2025_21-10-39", "3.1.0.1.137", false, false, "");
        Assert.NotEqual(a, b);
    }
}
