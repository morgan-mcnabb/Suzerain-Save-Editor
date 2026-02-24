namespace SuzerainSaveEditor.Core.Parsing;

public sealed class SaveParseException : Exception
{
    public SaveParseException(string message) : base(message) { }
    public SaveParseException(string message, Exception innerException) : base(message, innerException) { }
}
