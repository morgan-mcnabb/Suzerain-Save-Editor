using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.App.Services;

public sealed class FileDialogService : IFileDialogService
{
    private readonly Window _window;
    private readonly ISavePathProvider _savePathProvider;

    public FileDialogService(Window window, ISavePathProvider savePathProvider)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(savePathProvider);
        _window = window;
        _savePathProvider = savePathProvider;
    }

    public async Task<string?> OpenFileAsync()
    {
        IStorageFolder? suggestedFolder = null;

        // try each candidate save directory in priority order
        foreach (var candidatePath in _savePathProvider.GetSaveDirectories())
        {
            if (Directory.Exists(candidatePath))
            {
                suggestedFolder = await _window.StorageProvider
                    .TryGetFolderFromPathAsync(candidatePath);
                if (suggestedFolder is not null)
                    break;
            }
        }

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
