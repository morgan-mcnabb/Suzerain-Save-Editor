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
            await using (var writer = new StreamWriter(fs))
            {
                await writer.WriteAsync(text);
                await writer.FlushAsync();
                await fs.FlushAsync();
            }

            if (File.Exists(filePath))
                File.Replace(tempPath, filePath, null);
            else
                File.Move(tempPath, filePath);
        }
        catch
        {
            // clean up orphaned temp file on failure
            try { File.Delete(tempPath); } catch { /* best effort */ }
            throw;
        }
    }
}
