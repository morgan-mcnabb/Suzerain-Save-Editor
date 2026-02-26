namespace SuzerainSaveEditor.Core.Services;

public interface IRecentFilesService
{
    Task<IReadOnlyList<RecentFileEntry>> LoadAsync();

    Task AddAsync(string filePath);

    Task RemoveAsync(string filePath);

    Task ClearAsync();
}
