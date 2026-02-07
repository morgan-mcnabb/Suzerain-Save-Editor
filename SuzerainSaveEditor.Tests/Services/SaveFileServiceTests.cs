using System.Text.Json.Nodes;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Parsing;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class SaveFileServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ISaveParser _parser = new JsonSaveParser();
    private readonly BackupService _backupService = new();

    public SaveFileServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SuzerainTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SaveFileService CreateService(IBackupService? backupService = null)
    {
        return new SaveFileService(_parser, backupService ?? _backupService);
    }

    private static SaveDocument CreateMinimalDocument() => new()
    {
        Metadata = new SaveMetadata(
            SaveFileType: 3,
            CampaignName: "KING",
            CurrentStoryPack: "StoryPack_Sordland",
            TurnNo: 5,
            SaveFileName: "King1",
            SceneBuildIndex: 2,
            LastModified: "01-01-2025_12-00-00",
            Version: "3.1.0.1.137",
            IsVersionMismatched: false,
            IsTorporModeOn: false,
            Notes: ""),
        WarSaveData = new JsonObject(),
        Variables =
        [
            new LuaVariable("BaseGame.TestVar", new LuaValue.Bool(true)),
        ],
        EntityUpdates =
        [
            new EntityUpdate("Test_Entity", "TestField", "42"),
        ]
    };

    private string WriteSaveFile(string fileName = "save.json")
    {
        var doc = CreateMinimalDocument();
        var text = _parser.Serialize(doc);
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, text);
        return filePath;
    }

    // --- open ---

    [Fact]
    public async Task OpenAsync_ReturnsParsesSaveDocument()
    {
        var filePath = WriteSaveFile();
        var service = CreateService();

        var doc = await service.OpenAsync(filePath);

        Assert.Equal("KING", doc.Metadata.CampaignName);
        Assert.Equal(5, doc.Metadata.TurnNo);
        Assert.Single(doc.Variables);
        Assert.Single(doc.EntityUpdates);
    }

    [Fact]
    public async Task OpenAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        var service = CreateService();
        var filePath = Path.Combine(_tempDir, "nonexistent.json");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => service.OpenAsync(filePath));
    }

    [Fact]
    public async Task OpenAsync_NullPath_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.OpenAsync(null!));
    }

    [Fact]
    public async Task OpenAsync_EmptyPath_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.OpenAsync(""));
    }

    // --- save ---

    [Fact]
    public async Task SaveAsync_WritesFileToPath()
    {
        var filePath = WriteSaveFile();
        var service = CreateService();
        var doc = await service.OpenAsync(filePath);

        await service.SaveAsync(filePath, doc);

        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task SaveAsync_CreatesBackup()
    {
        var filePath = WriteSaveFile();
        var service = CreateService();
        var doc = await service.OpenAsync(filePath);

        await service.SaveAsync(filePath, doc);

        var backupDir = Path.Combine(_tempDir, "backups");
        Assert.True(Directory.Exists(backupDir));
        Assert.Single(Directory.GetFiles(backupDir));
    }

    [Fact]
    public async Task SaveAsync_BackupMatchesOriginalContent()
    {
        var filePath = WriteSaveFile();
        var originalContent = await File.ReadAllTextAsync(filePath);
        var service = CreateService();
        var doc = await service.OpenAsync(filePath);

        await service.SaveAsync(filePath, doc);

        var backupDir = Path.Combine(_tempDir, "backups");
        var backupFiles = Directory.GetFiles(backupDir);
        var backupContent = await File.ReadAllTextAsync(backupFiles[0]);
        Assert.Equal(originalContent, backupContent);
    }

    [Fact]
    public async Task SaveAsync_SavedFileCanBeReopened()
    {
        var filePath = WriteSaveFile();
        var service = CreateService();
        var doc = await service.OpenAsync(filePath);

        await service.SaveAsync(filePath, doc);

        var reopened = await service.OpenAsync(filePath);
        Assert.Equal(doc.Metadata.CampaignName, reopened.Metadata.CampaignName);
        Assert.Equal(doc.Variables.Count, reopened.Variables.Count);
    }

    [Fact]
    public async Task SaveAsync_NoTempFileLeftBehind()
    {
        var filePath = WriteSaveFile();
        var service = CreateService();
        var doc = await service.OpenAsync(filePath);

        await service.SaveAsync(filePath, doc);

        var tempPath = filePath + ".tmp";
        Assert.False(File.Exists(tempPath));
    }

    [Fact]
    public async Task SaveAsync_NewFile_CreatesWithoutBackup()
    {
        var filePath = Path.Combine(_tempDir, "new_save.json");
        var service = CreateService();
        var doc = CreateMinimalDocument();

        await service.SaveAsync(filePath, doc);

        Assert.True(File.Exists(filePath));
        var backupDir = Path.Combine(_tempDir, "backups");
        Assert.False(Directory.Exists(backupDir));
    }

    [Fact]
    public async Task SaveAsync_BackupFailure_AbortsWithoutWriting()
    {
        var filePath = WriteSaveFile();
        var originalContent = await File.ReadAllTextAsync(filePath);
        var failingBackup = new FailingBackupService();
        var service = CreateService(failingBackup);
        var doc = CreateMinimalDocument();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveAsync(filePath, doc));

        // original file should be untouched
        var currentContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(originalContent, currentContent);
    }

    [Fact]
    public async Task SaveAsync_NullPath_ThrowsArgumentException()
    {
        var service = CreateService();
        var doc = CreateMinimalDocument();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SaveAsync(null!, doc));
    }

    [Fact]
    public async Task SaveAsync_NullDocument_ThrowsArgumentNullException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SaveAsync("test.json", null!));
    }

    // --- round-trip with real save file ---

    [Fact]
    public async Task OpenAndSave_RoundTrip_ProducesIdenticalFile()
    {
        var filePath = WriteSaveFile();
        var service = CreateService();
        var originalContent = await File.ReadAllTextAsync(filePath);

        var doc = await service.OpenAsync(filePath);
        await service.SaveAsync(filePath, doc);

        var savedContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(originalContent, savedContent);
    }

    // test double that always throws
    private sealed class FailingBackupService : IBackupService
    {
        public Task<string> CreateBackupAsync(string filePath)
        {
            throw new InvalidOperationException("Backup failed intentionally");
        }
    }
}
