using System.Text.Json;

namespace SuzerainSaveEditor.Core.Services;

public sealed class RecentFilesService(IAppDataPathProvider appDataPathProvider) : IRecentFilesService
{
    private const int MaxEntries = 10;
    private const string FileName = "recent-files.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IReadOnlyList<RecentFileEntry>> LoadAsync()
    {
        var path = GetFilePath();
        if (!File.Exists(path))
            return [];

        List<RecentFileEntry> entries;
        try
        {
            var json = await File.ReadAllTextAsync(path);
            entries = JsonSerializer.Deserialize<List<RecentFileEntry>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }

        var valid = entries.Where(e => File.Exists(e.FilePath)).ToList();

        if (valid.Count != entries.Count)
            await WriteAsync(valid);

        return valid;
    }

    public async Task AddAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        filePath = Path.GetFullPath(filePath);
        var entries = await LoadRawAsync();

        entries.RemoveAll(e => string.Equals(e.FilePath, filePath, FilePathComparison()));
        entries.Insert(0, new RecentFileEntry(filePath, Path.GetFileName(filePath), DateTime.UtcNow));

        if (entries.Count > MaxEntries)
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

        await WriteAsync(entries);
    }

    public async Task RemoveAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        filePath = Path.GetFullPath(filePath);
        var entries = await LoadRawAsync();
        entries.RemoveAll(e => string.Equals(e.FilePath, filePath, FilePathComparison()));
        await WriteAsync(entries);
    }

    public async Task ClearAsync()
    {
        await WriteAsync([]);
    }

    private string GetFilePath() =>
        Path.Combine(appDataPathProvider.GetAppDataDirectory(), FileName);

    private async Task<List<RecentFileEntry>> LoadRawAsync()
    {
        var path = GetFilePath();
        if (!File.Exists(path))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<RecentFileEntry>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task WriteAsync(List<RecentFileEntry> entries)
    {
        var path = GetFilePath();
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(entries, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private static StringComparison FilePathComparison() =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
