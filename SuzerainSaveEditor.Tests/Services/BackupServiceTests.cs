using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class BackupServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly BackupService _service = new();

    public BackupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SuzerainTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string CreateTestFile(string fileName = "save.json", string content = "test content")
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    [Fact]
    public async Task CreateBackupAsync_CreatesBackupDirectory()
    {
        var filePath = CreateTestFile();

        await _service.CreateBackupAsync(filePath);

        var backupDir = Path.Combine(_tempDir, "backups");
        Assert.True(Directory.Exists(backupDir));
    }

    [Fact]
    public async Task CreateBackupAsync_CreatesBackupFile()
    {
        var filePath = CreateTestFile();

        var backupPath = await _service.CreateBackupAsync(filePath);

        Assert.True(File.Exists(backupPath));
    }

    [Fact]
    public async Task CreateBackupAsync_BackupContentsMatchOriginal()
    {
        var content = "{ \"key\": \"value\" }";
        var filePath = CreateTestFile(content: content);

        var backupPath = await _service.CreateBackupAsync(filePath);

        Assert.Equal(content, await File.ReadAllTextAsync(backupPath));
    }

    [Fact]
    public async Task CreateBackupAsync_BackupFileName_MatchesExpectedPattern()
    {
        var filePath = CreateTestFile("mysave.json");

        var backupPath = await _service.CreateBackupAsync(filePath);

        var backupFileName = Path.GetFileName(backupPath);
        // pattern: mysave.json.bak.yyyyMMdd-HHmmss
        Assert.Matches(@"^mysave\.json\.bak\.\d{8}-\d{6}$", backupFileName);
    }

    [Fact]
    public async Task CreateBackupAsync_BackupIsInBackupsSubdirectory()
    {
        var filePath = CreateTestFile();

        var backupPath = await _service.CreateBackupAsync(filePath);

        var expectedDir = Path.Combine(_tempDir, "backups");
        Assert.Equal(expectedDir, Path.GetDirectoryName(backupPath));
    }

    [Fact]
    public async Task CreateBackupAsync_ReturnsBackupPath()
    {
        var filePath = CreateTestFile();

        var backupPath = await _service.CreateBackupAsync(filePath);

        Assert.NotNull(backupPath);
        Assert.NotEmpty(backupPath);
    }

    [Fact]
    public async Task CreateBackupAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        var filePath = Path.Combine(_tempDir, "nonexistent.json");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.CreateBackupAsync(filePath));
    }

    [Fact]
    public async Task CreateBackupAsync_NullPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CreateBackupAsync(null!));
    }

    [Fact]
    public async Task CreateBackupAsync_EmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateBackupAsync(""));
    }

    [Fact]
    public async Task CreateBackupAsync_LargeFile_CopiesCorrectly()
    {
        var content = new string('x', 100_000);
        var filePath = CreateTestFile(content: content);

        var backupPath = await _service.CreateBackupAsync(filePath);

        var backupContent = await File.ReadAllTextAsync(backupPath);
        Assert.Equal(content.Length, backupContent.Length);
    }

    [Fact]
    public async Task CreateBackupAsync_OriginalFileUnchanged()
    {
        var content = "original content";
        var filePath = CreateTestFile(content: content);

        await _service.CreateBackupAsync(filePath);

        Assert.Equal(content, await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task CreateBackupAsync_BackupDirectoryAlreadyExists_StillWorks()
    {
        var backupDir = Path.Combine(_tempDir, "backups");
        Directory.CreateDirectory(backupDir);
        var filePath = CreateTestFile();

        var backupPath = await _service.CreateBackupAsync(filePath);

        Assert.True(File.Exists(backupPath));
    }

    [Fact]
    public async Task CreateBackupAsync_SameSecondBackup_OverwritesInsteadOfThrowing()
    {
        var filePath = CreateTestFile(content: "first version");

        var backupPath1 = await _service.CreateBackupAsync(filePath);

        // update the file and backup again within the same second
        await File.WriteAllTextAsync(filePath, "second version");
        var backupPath2 = await _service.CreateBackupAsync(filePath);

        // both calls should succeed and return the same path
        Assert.Equal(backupPath1, backupPath2);
        // the backup should contain the latest content
        Assert.Equal("second version", await File.ReadAllTextAsync(backupPath2));
    }
}
