using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Core.Parsing;

/// <summary>
/// format-agnostic save file parser abstraction
/// </summary>
public interface ISaveParser
{
    SaveDocument Parse(string text);
    string Serialize(SaveDocument document);
}
