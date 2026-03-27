namespace SvgEditor.Web.Shared.Validation;

public sealed class ValidationResult(IReadOnlyList<string> errors)
{
    public static readonly ValidationResult Success = new([]);

    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; } = errors;

    public static ValidationResult Failure(params string[] errors) => new(errors);
}
