using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace SuzerainSaveEditor.App.Services;

public sealed class FileDialogService : IFileDialogService
{
    private readonly Window _window;

    public FileDialogService(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _window = window;
    }

    public async Task<string?> OpenFileAsync()
    {
        var suzerainSavePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low",
            "Torpor Games",
            "Suzerain");

        IStorageFolder? suggestedFolder = null;
        if (Directory.Exists(suzerainSavePath))
            suggestedFolder = await _window.StorageProvider.TryGetFolderFromPathAsync(suzerainSavePath);

        var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Suzerain Save File",
            SuggestedStartLocation = suggestedFolder,
            FileTypeFilter =
            [
                new FilePickerFileType("JSON Files") { Patterns = ["*.json"] },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] }
            ],
            AllowMultiple = false
        });

        if (files.Count == 0)
            return null;

        return files[0].TryGetLocalPath();
    }
}
