namespace SuzerainSaveEditor.Core.Services;

public interface ISavePathProvider
{
    // returns candidate save directories in priority order, most specific first
    IReadOnlyList<string> GetSaveDirectories();
}
