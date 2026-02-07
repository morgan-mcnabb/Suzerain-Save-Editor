namespace SuzerainSaveEditor.Core.Parsing;

/// <summary>
/// thrown when a save file cannot be parsed
/// </summary>
public sealed class SaveParseException : Exception
{
    public SaveParseException(string message) : base(message) { }
    public SaveParseException(string message, Exception innerException) : base(message, innerException) { }
}
