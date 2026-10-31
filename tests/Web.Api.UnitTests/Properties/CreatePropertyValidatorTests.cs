using FluentValidation.TestHelper;
using Web.Api.Features.Properties;

namespace Web.Api.UnitTests.Properties;

public sealed class CreatePropertyValidatorTests
{
    private readonly CreateProperty.Validator _validator = new();

    // RN-01
    [Fact]
    public void Validator_Should_HaveError_WhenNameIsEmpty()
    {
        var command = new CreateProperty.Command(string.Empty, "Av. Lima 123", PropertyType.House);

        TestValidationResult<CreateProperty.Command> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    // RN-01
    [Fact]
    public void Validator_Should_HaveError_WhenNameExceeds100Characters()
    {
        var command = new CreateProperty.Command(new string('a', 101), "Av. Lima 123", PropertyType.House);

        TestValidationResult<CreateProperty.Command> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    // RN-01
    [Fact]
    public void Validator_Should_HaveError_WhenAddressIsEmpty()
    {
        var command = new CreateProperty.Command("Casa", string.Empty, PropertyType.House);

        TestValidationResult<CreateProperty.Command> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Address);
    }

    // RN-01
    [Fact]
    public void Validator_Should_HaveError_WhenAddressExceeds200Characters()
    {
        var command = new CreateProperty.Command("Casa", new string('a', 201), PropertyType.House);

        TestValidationResult<CreateProperty.Command> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Address);
    }

    // RN-01
    [Fact]
    public void Validator_Should_HaveError_WhenTypeIsNotInEnum()
    {
        var command = new CreateProperty.Command("Casa", "Av. Lima 123", (PropertyType)99);

        TestValidationResult<CreateProperty.Command> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Type);
    }

    // RN-01
    [Fact]
    public void Validator_Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new CreateProperty.Command("Casa", "Av. Lima 123", PropertyType.House);

        TestValidationResult<CreateProperty.Command> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
