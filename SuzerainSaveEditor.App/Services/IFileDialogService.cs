namespace SuzerainSaveEditor.App.Services;

public interface IFileDialogService
{
    Task<string?> OpenFileAsync();
}
