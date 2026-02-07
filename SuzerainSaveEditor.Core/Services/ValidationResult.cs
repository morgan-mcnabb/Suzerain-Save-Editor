namespace SuzerainSaveEditor.Core.Services;

// result of validating a field value
public sealed record ValidationResult(bool IsValid, string? Error = null)
{
    public static readonly ValidationResult Success = new(true);

    public static ValidationResult Failure(string error) => new(false, error);
}
