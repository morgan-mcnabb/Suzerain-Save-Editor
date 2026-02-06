namespace SuzerainSaveEditor.Core.Models;

// maps to the 11 scalar top-level fields in the save file
public sealed record SaveMetadata(
    int SaveFileType,
    string CampaignName,
    string CurrentStoryPack,
    int TurnNo,
    string SaveFileName,
    int SceneBuildIndex,
    string LastModified,
    string Version,
    bool IsVersionMismatched,
    bool IsTorporModeOn,
    string Notes);
