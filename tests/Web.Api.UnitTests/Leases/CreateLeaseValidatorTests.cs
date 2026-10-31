using FluentValidation.TestHelper;
using Web.Api.Features.Leases;

namespace Web.Api.UnitTests.Leases;

public class CreateLeaseValidatorTests
{
    private readonly CreateLease.Validator _validator = new();

    private static CreateLease.Command Valid(
        DocumentType type = DocumentType.Dni,
        string number = "45678912",
        string? phone = "987654321",
        string? email = "maria@example.com",
        DateOnly? end = null,
        decimal rent = 1200m,
        decimal deposit = 2400m,
        int paymentDay = 5) => new(
        Guid.NewGuid(),
        "María Quispe Huamán",
        type,
        number,
        phone,
        email,
        new DateOnly(2026, 8, 1),
        end ?? new DateOnly(2027, 7, 31),
        rent,
        deposit,
        paymentDay);

    // RN-06, RN-07, RN-08, RN-09
    [Fact]
    public void Validate_Should_Pass_WhenCommandIsValid()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    // RN-06
    [Fact]
    public void Validate_Should_Fail_WhenEndDateIsLessThanOneMonthAfterStart()
    {
        TestValidationResult<CreateLease.Command> result =
            _validator.TestValidate(Valid(end: new DateOnly(2026, 8, 31)));

        result.ShouldHaveValidationErrorFor(c => c.EndDate);
    }

    // RN-06
    [Fact]
    public void Validate_Should_Pass_WhenEndDateIsExactlyOneMonthAfterStart()
    {
        _validator.TestValidate(Valid(end: new DateOnly(2026, 9, 1))).ShouldNotHaveValidationErrorFor(c => c.EndDate);
    }

    // RN-07
    [Fact]
    public void Validate_Should_Fail_WhenMonthlyRentIsNotPositive()
    {
        _validator.TestValidate(Valid(rent: 0m, deposit: 0m)).ShouldHaveValidationErrorFor(c => c.MonthlyRent);
    }

    // RN-07
    [Fact]
    public void Validate_Should_Fail_WhenDepositExceedsTwiceRent()
    {
        _validator.TestValidate(Valid(deposit: 2400.01m)).ShouldHaveValidationErrorFor(c => c.Deposit);
    }

    // RN-07
    [Fact]
    public void Validate_Should_Fail_WhenDepositIsNegative()
    {
        _validator.TestValidate(Valid(deposit: -1m)).ShouldHaveValidationErrorFor(c => c.Deposit);
    }

    // RN-08
    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    public void Validate_Should_Fail_WhenPaymentDayIsOutOfRange(int day)
    {
        _validator.TestValidate(Valid(paymentDay: day)).ShouldHaveValidationErrorFor(c => c.PaymentDay);
    }

    // RN-08
    [Theory]
    [InlineData(1)]
    [InlineData(28)]
    public void Validate_Should_Pass_WhenPaymentDayIsAtBounds(int day)
    {
        _validator.TestValidate(Valid(paymentDay: day)).ShouldNotHaveValidationErrorFor(c => c.PaymentDay);
    }

    // RN-09
    [Fact]
    public void Validate_Should_Fail_WhenRucCheckDigitIsWrong()
    {
        _validator.TestValidate(Valid(DocumentType.Ruc, "20601234560"))
            .ShouldHaveValidationErrorFor(c => c.TenantDocumentNumber);
    }

    // RN-09
    [Fact]
    public void Validate_Should_Pass_WhenRucIsValid()
    {
        _validator.TestValidate(Valid(DocumentType.Ruc, "20601234565"))
            .ShouldNotHaveValidationErrorFor(c => c.TenantDocumentNumber);
    }

    // RN-09
    [Fact]
    public void Validate_Should_Fail_WhenDniIsNotEightDigits()
    {
        _validator.TestValidate(Valid(number: "1234567"))
            .ShouldHaveValidationErrorFor(c => c.TenantDocumentNumber);
    }

    // RN-09
    [Fact]
    public void Validate_Should_Fail_WhenPhoneIsNotPeruvianMobile()
    {
        _validator.TestValidate(Valid(phone: "187654321")).ShouldHaveValidationErrorFor(c => c.TenantPhone);
    }

    // RN-09
    [Fact]
    public void Validate_Should_Pass_WhenPhoneAndEmailAreOmitted()
    {
        _validator.TestValidate(Valid(phone: null, email: null)).ShouldNotHaveAnyValidationErrors();
    }

    // RN-09
    [Fact]
    public void Validate_Should_Fail_WhenEmailIsInvalid()
    {
        _validator.TestValidate(Valid(email: "no-es-correo")).ShouldHaveValidationErrorFor(c => c.TenantEmail);
    }
}
