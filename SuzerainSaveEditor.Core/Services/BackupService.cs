namespace SuzerainSaveEditor.Core.Services;

// creates timestamped backups in a backups/ subdirectory next to the save file
public sealed class BackupService : IBackupService
{
    public async Task<string> CreateBackupAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Cannot backup: file not found.", filePath);

        var directory = Path.GetDirectoryName(filePath)
            ?? throw new ArgumentException("Cannot determine directory for file.", nameof(filePath));

        var backupDir = Path.Combine(directory, "backups");
        Directory.CreateDirectory(backupDir);

        var fileName = Path.GetFileName(filePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var backupFileName = $"{fileName}.bak.{timestamp}";
        var backupPath = Path.Combine(backupDir, backupFileName);

        await using var source = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var destination = new FileStream(backupPath, FileMode.Create, FileAccess.Write);
        await source.CopyToAsync(destination);
        await destination.FlushAsync();

        return backupPath;
    }
}
