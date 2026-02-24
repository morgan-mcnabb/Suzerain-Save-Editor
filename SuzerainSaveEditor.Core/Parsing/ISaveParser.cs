using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Core.Parsing;

public interface ISaveParser
{
    SaveDocument Parse(string text);
    string Serialize(SaveDocument document);
}
