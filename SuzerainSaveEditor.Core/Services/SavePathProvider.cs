namespace SuzerainSaveEditor.Core.Services;

// resolves suzerain save file directories for the current OS
public sealed class SavePathProvider : ISavePathProvider
{
    private const string Publisher = "Torpor Games";
    private const string GameName = "Suzerain";
    private const int SteamAppId = 1207650;

    public IReadOnlyList<string> GetSaveDirectories()
    {
        if (OperatingSystem.IsWindows())
            return GetWindowsPaths();

        if (OperatingSystem.IsMacOS())
            return GetMacPaths();

        if (OperatingSystem.IsLinux())
            return GetLinuxPaths();

        return [];
    }

    private static List<string> GetWindowsPaths()
    {
        // unity persistentDataPath on windows: %AppData%\..\LocalLow\Torpor Games\Suzerain
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var appData = Directory.GetParent(localAppData)!.FullName;
        return [Path.Combine(appData, "LocalLow", Publisher, GameName)];
    }

    private static List<string> GetMacPaths()
    {
        // unity persistentDataPath on macos: ~/Library/Application Support/Torpor Games/Suzerain
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return [Path.Combine(home, "Library", "Application Support", Publisher, GameName)];
    }

    private static List<string> GetLinuxPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // unity respects XDG_CONFIG_HOME, defaults to ~/.config
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(configHome))
            configHome = Path.Combine(home, ".config");

        // native linux: $XDG_CONFIG_HOME/unity3d/Torpor Games/Suzerain
        var nativePath = Path.Combine(configHome, "unity3d", Publisher, GameName);

        // proton/steam: ~/.steam/steam/steamapps/compatdata/<appid>/pfx/drive_c/users/steamuser/AppData/LocalLow/Torpor Games/Suzerain
        var protonPath = Path.Combine(
            home, ".steam", "steam", "steamapps", "compatdata",
            SteamAppId.ToString(), "pfx", "drive_c", "users", "steamuser",
            "AppData", "LocalLow", Publisher, GameName);

        // native first (more likely if running natively), proton second
        return [nativePath, protonPath];
    }
}
