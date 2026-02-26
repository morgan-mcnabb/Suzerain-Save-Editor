using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class AppDataPathProviderTests
{
    private readonly AppDataPathProvider _provider = new();

    [Fact]
    public void GetAppDataDirectory_ReturnsNonEmptyString()
    {
        var path = _provider.GetAppDataDirectory();

        Assert.False(string.IsNullOrWhiteSpace(path));
    }

    [Fact]
    public void GetAppDataDirectory_ReturnsAbsolutePath()
    {
        var path = _provider.GetAppDataDirectory();

        Assert.True(Path.IsPathRooted(path));
    }

    [Fact]
    public void GetAppDataDirectory_EndsWithAppName()
    {
        var path = _provider.GetAppDataDirectory();

        Assert.EndsWith("SuzerainSaveEditor", path);
    }

    [SkippableFact]
    public void GetAppDataDirectory_Windows_UsesAppData()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var path = _provider.GetAppDataDirectory();

        Assert.Contains("AppData", path);
    }

    [SkippableFact]
    public void GetAppDataDirectory_MacOS_UsesApplicationSupport()
    {
        Skip.IfNot(OperatingSystem.IsMacOS());

        var path = _provider.GetAppDataDirectory();

        Assert.Contains("Application Support", path);
    }
}
