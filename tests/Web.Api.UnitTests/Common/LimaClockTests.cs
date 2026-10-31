using Web.Api.Common;

namespace Web.Api.UnitTests.Common;

public class LimaClockTests
{
    // RN-18
    [Theory]
    [InlineData("2026-10-06T03:00:00Z", "2026-10-05")]
    [InlineData("2026-10-06T04:59:59Z", "2026-10-05")]
    [InlineData("2026-10-06T05:00:00Z", "2026-10-06")]
    public void Today_Should_ConvertToLimaDate_When_UtcCrossesMidnight(string utc, string expected)
    {
        IDateTimeProvider provider = Substitute.For<IDateTimeProvider>();
        provider.UtcNow.Returns(DateTime.Parse(
            utc,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal));

        provider.Today().ShouldBe(DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }
}
