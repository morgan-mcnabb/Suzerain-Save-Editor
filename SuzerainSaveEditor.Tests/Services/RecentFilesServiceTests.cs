using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class RecentFilesServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly RecentFilesService _service;

    public RecentFilesServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SuzerainTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _service = new RecentFilesService(new FakeAppDataPathProvider(_tempDir));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string CreateTestFile(string name = "save.json")
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, "test");
        return path;
    }

    [Fact]
    public async Task LoadAsync_NoFile_ReturnsEmptyList()
    {
        var result = await _service.LoadAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_SingleEntry_LoadReturnsIt()
    {
        var file = CreateTestFile();

        await _service.AddAsync(file);
        var result = await _service.LoadAsync();

        Assert.Single(result);
        Assert.Equal(file, result[0].FilePath);
    }

    [Fact]
    public async Task AddAsync_SetsDisplayNameFromFileName()
    {
        var file = CreateTestFile("my-save.json");

        await _service.AddAsync(file);
        var result = await _service.LoadAsync();

        Assert.Equal("my-save.json", result[0].DisplayName);
    }

    [Fact]
    public async Task AddAsync_SetsLastOpenedUtc()
    {
        var before = DateTime.UtcNow;
        var file = CreateTestFile();

        await _service.AddAsync(file);
        var result = await _service.LoadAsync();

        var after = DateTime.UtcNow;
        Assert.InRange(result[0].LastOpenedUtc, before, after);
    }

    [Fact]
    public async Task AddAsync_MostRecentFirst()
    {
        var file1 = CreateTestFile("a.json");
        var file2 = CreateTestFile("b.json");

        await _service.AddAsync(file1);
        await _service.AddAsync(file2);
        var result = await _service.LoadAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(file2, result[0].FilePath);
        Assert.Equal(file1, result[1].FilePath);
    }

    [Fact]
    public async Task AddAsync_DuplicatePath_MovesToTop()
    {
        var file1 = CreateTestFile("a.json");
        var file2 = CreateTestFile("b.json");

        await _service.AddAsync(file1);
        await _service.AddAsync(file2);
        await _service.AddAsync(file1);
        var result = await _service.LoadAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(file1, result[0].FilePath);
        Assert.Equal(file2, result[1].FilePath);
    }

    [Fact]
    public async Task AddAsync_ExceedingMax_TrimsOldest()
    {
        var files = new List<string>();
        for (var i = 0; i < 12; i++)
        {
            var file = CreateTestFile($"save{i}.json");
            files.Add(file);
            await _service.AddAsync(file);
        }

        var result = await _service.LoadAsync();

        Assert.Equal(10, result.Count);
        Assert.Equal(files[11], result[0].FilePath);
        Assert.Equal(files[2], result[9].FilePath);
    }

    [Fact]
    public async Task RemoveAsync_RemovesEntry()
    {
        var file1 = CreateTestFile("a.json");
        var file2 = CreateTestFile("b.json");
        await _service.AddAsync(file1);
        await _service.AddAsync(file2);

        await _service.RemoveAsync(file1);
        var result = await _service.LoadAsync();

        Assert.Single(result);
        Assert.Equal(file2, result[0].FilePath);
    }

    [Fact]
    public async Task RemoveAsync_NonExistentPath_NoError()
    {
        var file = CreateTestFile();
        await _service.AddAsync(file);

        await _service.RemoveAsync(Path.Combine(_tempDir, "nope.json"));
        var result = await _service.LoadAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        var file1 = CreateTestFile("a.json");
        var file2 = CreateTestFile("b.json");
        await _service.AddAsync(file1);
        await _service.AddAsync(file2);

        await _service.ClearAsync();
        var result = await _service.LoadAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadAsync_StaleEntry_FilteredOut()
    {
        var file = CreateTestFile();
        await _service.AddAsync(file);
        File.Delete(file);

        var result = await _service.LoadAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadAsync_MixOfStaleAndValid_KeepsValid()
    {
        var valid = CreateTestFile("valid.json");
        var stale = CreateTestFile("stale.json");
        await _service.AddAsync(valid);
        await _service.AddAsync(stale);
        File.Delete(stale);

        var result = await _service.LoadAsync();

        Assert.Single(result);
        Assert.Equal(valid, result[0].FilePath);
    }

    [Fact]
    public async Task LoadAsync_CorruptedJson_ReturnsEmptyList()
    {
        var jsonPath = Path.Combine(_tempDir, "recent-files.json");
        await File.WriteAllTextAsync(jsonPath, "not valid json {{{");

        var result = await _service.LoadAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_NullPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_EmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddAsync(""));
    }

    [Fact]
    public async Task RemoveAsync_NullPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.RemoveAsync(null!));
    }

    [Fact]
    public async Task AddAsync_CreatesAppDataDirectory()
    {
        var subDir = Path.Combine(_tempDir, "nested");
        var service = new RecentFilesService(new FakeAppDataPathProvider(subDir));
        var file = CreateTestFile();

        await service.AddAsync(file);

        Assert.True(Directory.Exists(subDir));
    }

    [Fact]
    public async Task LoadAsync_StaleEntries_PersistsCleanedList()
    {
        var valid = CreateTestFile("valid.json");
        var stale = CreateTestFile("stale.json");
        await _service.AddAsync(valid);
        await _service.AddAsync(stale);
        File.Delete(stale);

        await _service.LoadAsync();

        var secondLoad = await _service.LoadAsync();
        Assert.Single(secondLoad);
        Assert.Equal(valid, secondLoad[0].FilePath);
    }

    private sealed class FakeAppDataPathProvider(string directory) : IAppDataPathProvider
    {
        public string GetAppDataDirectory() => directory;
    }
}
