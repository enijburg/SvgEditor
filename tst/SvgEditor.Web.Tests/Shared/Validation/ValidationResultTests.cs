using SvgEditor.Web.Shared.Validation;

namespace SvgEditor.Web.Tests.Shared.Validation;

[TestClass]
public sealed class ValidationResultTests
{
    [TestMethod]
    public void Success_IsValidTrue_NoErrors()
    {
        var result = ValidationResult.Success;

        Assert.IsTrue(result.IsValid);
        Assert.IsEmpty(result.Errors);
    }

    [TestMethod]
    public void Failure_IsValidFalse_HasErrors()
    {
        var result = ValidationResult.Failure("Error 1", "Error 2");

        Assert.IsFalse(result.IsValid);
        Assert.HasCount(2, result.Errors);
        Assert.AreEqual("Error 1", result.Errors[0]);
        Assert.AreEqual("Error 2", result.Errors[1]);
    }

    [TestMethod]
    public void Constructor_WithEmptyErrors_IsValid()
    {
        var result = new ValidationResult([]);

        Assert.IsTrue(result.IsValid);
    }
}
