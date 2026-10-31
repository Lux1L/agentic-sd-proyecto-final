using Web.Api.Features.Leases;

namespace Web.Api.UnitTests.Leases;

public class TenantDocumentTests
{
    // RN-09
    [Theory]
    [InlineData("12345678", true)]
    [InlineData("1234567", false)]
    [InlineData("123456789", false)]
    [InlineData("1234567a", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_Should_ValidateDni_When_NumberGiven(string? number, bool expected)
    {
        TenantDocument.IsValid(DocumentType.Dni, number).ShouldBe(expected);
    }

    // RN-09
    [Theory]
    [InlineData("ABC123456", true)]
    [InlineData("ABC123456789", true)]
    [InlineData("ABC12345", false)]
    [InlineData("ABC1234567890", false)]
    [InlineData("ABC-12345", false)]
    public void IsValid_Should_ValidateCe_When_NumberGiven(string number, bool expected)
    {
        TenantDocument.IsValid(DocumentType.Ce, number).ShouldBe(expected);
    }

    // RN-09
    [Theory]
    [InlineData("20601234565")]
    [InlineData("10123456781")]
    public void IsValid_Should_ReturnTrue_When_RucIsValid(string number)
    {
        TenantDocument.IsValid(DocumentType.Ruc, number).ShouldBeTrue();
    }

    // RN-09
    [Fact]
    public void IsValid_Should_ReturnFalse_When_RucCheckDigitIsWrong()
    {
        TenantDocument.IsValid(DocumentType.Ruc, "20601234560").ShouldBeFalse();
    }

    // RN-09
    [Theory]
    [InlineData("30601234565")]
    [InlineData("2060123456")]
    [InlineData("2060123456A")]
    public void IsValid_Should_ReturnFalse_When_RucPrefixOrFormatIsInvalid(string number)
    {
        TenantDocument.IsValid(DocumentType.Ruc, number).ShouldBeFalse();
    }

    // RN-09
    [Theory]
    [InlineData("912345678", true)]
    [InlineData("812345678", false)]
    [InlineData("91234567", false)]
    [InlineData("9123456789", false)]
    [InlineData(null, false)]
    public void IsValidPeruvianMobile_Should_ValidateFormat_When_PhoneGiven(string? phone, bool expected)
    {
        TenantDocument.IsValidPeruvianMobile(phone).ShouldBe(expected);
    }
}
