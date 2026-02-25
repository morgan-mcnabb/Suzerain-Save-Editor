using System.Text;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Parsing;

namespace SuzerainSaveEditor.Core.Services;

// orchestrates open/save with backup and atomic write
public sealed class SaveFileService : ISaveFileService
{
    private readonly ISaveParser _parser;
    private readonly IBackupService _backupService;

    public SaveFileService(ISaveParser parser, IBackupService backupService)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(backupService);

        _parser = parser;
        _backupService = backupService;
    }

    public async Task<SaveDocument> OpenAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Save file not found.", filePath);

        var text = await File.ReadAllTextAsync(filePath);
        return _parser.Parse(text);
    }

    public async Task SaveAsync(string filePath, SaveDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(document);

        // backup first — if this fails, the exception propagates and we abort
        if (File.Exists(filePath))
            await _backupService.CreateBackupAsync(filePath);

        // serialize
        var text = _parser.Serialize(document);

        // write to temp file then atomic replace
        var tempPath = filePath + ".tmp";
        try
        {
            await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var writer = new StreamWriter(fs, new UTF8Encoding(false)))
            {
                await writer.WriteAsync(text);
                await writer.FlushAsync();
                await fs.FlushAsync();
            }

            ReplaceOriginal(tempPath, filePath);
        }
        catch (Exception ex)
        {
            // clean up orphaned temp file on failure, surfacing cleanup errors
            // instead of silently swallowing them
            if (!TryDeleteTempFile(tempPath, out var cleanupEx))
                throw new IOException(
                    $"Save failed and temp-file cleanup also failed. " +
                    $"Orphaned file may remain at: {tempPath}",
                    new AggregateException(ex, cleanupEx!));
            throw;
        }
    }

    // replaces the original file atomically, handling the TOCTOU race where the
    // original may be deleted between the Exists check and the Replace call
    private static void ReplaceOriginal(string tempPath, string filePath)
    {
        if (!File.Exists(filePath))
        {
            File.Move(tempPath, filePath);
            return;
        }

        try
        {
            File.Replace(tempPath, filePath, null);
        }
        catch (FileNotFoundException)
        {
            // original was deleted between Exists check and Replace call
            File.Move(tempPath, filePath);
        }
    }

    private static bool TryDeleteTempFile(string tempPath, out Exception? exception)
    {
        try
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            exception = null;
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }
}
