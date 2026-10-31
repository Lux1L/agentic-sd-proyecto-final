using Web.Api.Features.Leases;

namespace Web.Api.UnitTests.Leases;

public class PeriodTests
{
    // RN-15
    [Fact]
    public void TryParse_Should_ReturnPeriod_When_FormatIsValid()
    {
        bool ok = Period.TryParse("2026-10", out Period period);

        ok.ShouldBeTrue();
        period.ShouldBe(new Period(2026, 10));
    }

    // RN-15
    [Theory]
    [InlineData("2026-13")]
    [InlineData("2026-00")]
    [InlineData("26-10")]
    [InlineData("2026-1")]
    [InlineData("2026/10")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_Should_ReturnFalse_When_FormatIsInvalid(string? value)
    {
        Period.TryParse(value, out _).ShouldBeFalse();
    }

    // RN-15
    [Fact]
    public void Next_Should_RollOverYear_When_MonthIsDecember()
    {
        new Period(2026, 12).Next().ShouldBe(new Period(2027, 1));
    }

    // RN-16
    [Fact]
    public void DueDate_Should_UsePaymentDay_When_MonthHasEnoughDays()
    {
        new Period(2026, 10).DueDate(5).ShouldBe(new DateOnly(2026, 10, 5));
    }

    // RN-16
    [Fact]
    public void DueDate_Should_ClampToLastDay_When_MonthIsShorter()
    {
        new Period(2026, 2).DueDate(31).ShouldBe(new DateOnly(2026, 2, 28));
    }

    // RN-15
    [Fact]
    public void ToString_Should_FormatAsYearMonth_When_Called()
    {
        new Period(2026, 3).ToString().ShouldBe("2026-03");
    }

    // RN-15
    [Fact]
    public void Operators_Should_CompareChronologically_When_PeriodsDiffer()
    {
        var a = new Period(2026, 12);
        var b = new Period(2027, 1);

        (a < b).ShouldBeTrue();
        (b > a).ShouldBeTrue();
        (a <= new Period(2026, 12)).ShouldBeTrue();
        (a >= b).ShouldBeFalse();
    }
}
