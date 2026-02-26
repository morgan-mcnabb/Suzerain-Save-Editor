namespace SuzerainSaveEditor.Core.Services;

public sealed record RecentFileEntry(string FilePath, string DisplayName, DateTime LastOpenedUtc);
