using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class SavePathProviderTests
{
    private readonly SavePathProvider _provider = new();

    [Fact]
    public void GetSaveDirectories_ReturnsNonEmptyList()
    {
        var paths = _provider.GetSaveDirectories();

        Assert.NotEmpty(paths);
    }

    [Fact]
    public void GetSaveDirectories_AllPathsAreNonEmpty()
    {
        var paths = _provider.GetSaveDirectories();

        Assert.All(paths, p => Assert.False(string.IsNullOrWhiteSpace(p)));
    }

    [Fact]
    public void GetSaveDirectories_AllPathsAreAbsolute()
    {
        var paths = _provider.GetSaveDirectories();

        Assert.All(paths, p => Assert.True(Path.IsPathRooted(p)));
    }

    [Fact]
    public void GetSaveDirectories_AllPathsContainPublisher()
    {
        var paths = _provider.GetSaveDirectories();

        Assert.All(paths, p => Assert.Contains("Torpor Games", p));
    }

    [Fact]
    public void GetSaveDirectories_AllPathsContainGameName()
    {
        var paths = _provider.GetSaveDirectories();

        Assert.All(paths, p => Assert.Contains("Suzerain", p));
    }

    // windows-specific tests

    [SkippableFact]
    public void GetSaveDirectories_Windows_ReturnsSinglePath()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var paths = _provider.GetSaveDirectories();

        Assert.Single(paths);
    }

    [SkippableFact]
    public void GetSaveDirectories_Windows_PathContainsLocalAppData()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var paths = _provider.GetSaveDirectories();

        Assert.Contains("AppData", paths[0]);
    }

    // linux-specific tests

    [SkippableFact]
    public void GetSaveDirectories_Linux_ReturnsTwoPaths()
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        var paths = _provider.GetSaveDirectories();

        Assert.Equal(2, paths.Count);
    }

    [SkippableFact]
    public void GetSaveDirectories_Linux_FirstPathIsNative()
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        var paths = _provider.GetSaveDirectories();

        Assert.Contains("unity3d", paths[0]);
    }

    [SkippableFact]
    public void GetSaveDirectories_Linux_SecondPathIsProton()
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        var paths = _provider.GetSaveDirectories();

        Assert.Contains("compatdata", paths[1]);
        Assert.Contains("1207650", paths[1]);
    }

    // macos-specific tests

    [SkippableFact]
    public void GetSaveDirectories_MacOS_ReturnsSinglePath()
    {
        Skip.IfNot(OperatingSystem.IsMacOS());

        var paths = _provider.GetSaveDirectories();

        Assert.Single(paths);
    }

    [SkippableFact]
    public void GetSaveDirectories_MacOS_PathContainsApplicationSupport()
    {
        Skip.IfNot(OperatingSystem.IsMacOS());

        var paths = _provider.GetSaveDirectories();

        Assert.Contains("Application Support", paths[0]);
    }
}
