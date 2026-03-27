namespace SvgEditor.Web.Shared.Validation;

public interface IValidator<T>
{
    ValidationResult Validate(T instance);
}
