using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Core.Services;

public interface ISaveFileService
{
    Task<SaveDocument> OpenAsync(string filePath);
    Task SaveAsync(string filePath, SaveDocument document);
}
