using FluentValidation.TestHelper;
using Web.Api.Features.Leases;

namespace Web.Api.UnitTests.Leases;

public class RecordPaymentValidatorTests
{
    private readonly RecordPayment.Validator _validator = new();

    private static RecordPayment.Command Valid(
        string period = "2026-10",
        decimal amount = 500m,
        DateOnly? paidOn = null,
        PaymentMethod method = PaymentMethod.Yape) => new(
        Guid.NewGuid(),
        period,
        amount,
        paidOn ?? new DateOnly(2026, 10, 5),
        method);

    // RN-12
    [Fact]
    public void Validate_Should_Pass_WhenCommandIsValid()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    // RN-12
    [Theory]
    [InlineData("2026-13")]
    [InlineData("26-10")]
    [InlineData("")]
    public void Validate_Should_Fail_WhenPeriodFormatIsInvalid(string period)
    {
        _validator.TestValidate(Valid(period: period)).ShouldHaveValidationErrorFor(c => c.Period);
    }

    // RN-12
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_Should_Fail_WhenAmountIsNotPositive(int amount)
    {
        _validator.TestValidate(Valid(amount: amount)).ShouldHaveValidationErrorFor(c => c.Amount);
    }

    // RN-12
    [Fact]
    public void Validate_Should_Fail_WhenMethodIsOutOfEnum()
    {
        _validator.TestValidate(Valid(method: (PaymentMethod)999)).ShouldHaveValidationErrorFor(c => c.Method);
    }

    // RN-12
    [Fact]
    public void Validate_Should_Fail_WhenPaidOnIsDefault()
    {
        _validator.TestValidate(Valid(paidOn: default(DateOnly))).ShouldHaveValidationErrorFor(c => c.PaidOn);
    }
}
