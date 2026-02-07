namespace SuzerainSaveEditor.Core.Services;

public interface IBackupService
{
    Task<string> CreateBackupAsync(string filePath);
}
