using Avalonia.Platform.Storage;

namespace SuzerainSaveEditor.App.Services;

internal static class DragDropHelper
{
    internal static IReadOnlyList<DroppedFile>? ToDroppedFiles(IReadOnlyList<IStorageItem>? items)
    {
        if (items is null) return null;

        var result = new DroppedFile[items.Count];
        for (var i = 0; i < items.Count; i++)
            result[i] = new DroppedFile(items[i].Name, items[i].TryGetLocalPath());

        return result;
    }

    internal static bool HasJsonFile(IReadOnlyList<DroppedFile>? files)
    {
        if (files is null) return false;

        for (var i = 0; i < files.Count; i++)
        {
            if (files[i].Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    internal static string? GetFirstJsonFilePath(IReadOnlyList<DroppedFile>? files)
    {
        if (files is null) return null;

        for (var i = 0; i < files.Count; i++)
        {
            if (files[i].Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return files[i].LocalPath;
        }

        return null;
    }
}
