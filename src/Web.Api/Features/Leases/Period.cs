using System.Globalization;

namespace Web.Api.Features.Leases;

/// <summary>Período de cobro mensual en formato YYYY-MM.</summary>
public readonly record struct Period(int Year, int Month) : IComparable<Period>
{
    public static bool TryParse(string? value, out Period period)
    {
        period = default;

        if (value is not { Length: 7 } || value[4] != '-')
        {
            return false;
        }

        if (!AllDigits(value.AsSpan(0, 4)) || !AllDigits(value.AsSpan(5, 2)))
        {
            return false;
        }

        int year = int.Parse(value.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture);
        int month = int.Parse(value.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture);

        if (year < 1 || month is < 1 or > 12)
        {
            return false;
        }

        period = new Period(year, month);
        return true;
    }

    public static Period FromDate(DateOnly date) => new(date.Year, date.Month);

    public Period Next() => Month == 12 ? new Period(Year + 1, 1) : new Period(Year, Month + 1);

    /// <summary>Fecha de vencimiento; si el mes tiene menos días, se usa el último día del mes.</summary>
    public DateOnly DueDate(int paymentDay) =>
        new(Year, Month, Math.Min(paymentDay, DateTime.DaysInMonth(Year, Month)));

    public int CompareTo(Period other) =>
        Year != other.Year ? Year.CompareTo(other.Year) : Month.CompareTo(other.Month);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Year:D4}-{Month:D2}");

    public static bool operator <(Period left, Period right) => left.CompareTo(right) < 0;

    public static bool operator >(Period left, Period right) => left.CompareTo(right) > 0;

    public static bool operator <=(Period left, Period right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Period left, Period right) => left.CompareTo(right) >= 0;

    private static bool AllDigits(ReadOnlySpan<char> span)
    {
        foreach (char c in span)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }
}
