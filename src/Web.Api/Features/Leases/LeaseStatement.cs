namespace Web.Api.Features.Leases;

/// <summary>
/// Estado de cuenta calculado, sin persistir (RN-15..RN-17, ADR-004). Función pura.
/// </summary>
public sealed record LeaseStatement(
    DateOnly Today,
    decimal MonthlyRent,
    IReadOnlyList<PeriodLine> Periods,
    decimal OverdueDebt,
    decimal OutstandingBalance)
{
    public PeriodLine? CurrentPeriod
    {
        get
        {
            string current = Period.FromDate(Today).ToString();
            return Periods.FirstOrDefault(p => p.Period == current);
        }
    }

    public static LeaseStatement Calculate(Lease lease, IEnumerable<Payment> payments, DateOnly today)
    {
        var first = Period.FromDate(lease.StartDate);
        Period last = Min(Period.FromDate(lease.EndDate), Period.FromDate(today));
        if (lease.TerminatedOn is { } terminatedOn)
        {
            last = Min(last, Period.FromDate(terminatedOn));
        }

        var paidByPeriod = payments
            .GroupBy(p => new Period(p.PeriodYear, p.PeriodMonth))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var lines = new List<PeriodLine>();
        for (Period period = first; period <= last; period = period.Next())
        {
            decimal paid = paidByPeriod.GetValueOrDefault(period);
            decimal balance = Math.Max(0m, lease.MonthlyRent - paid);
            DateOnly dueDate = period.DueDate(lease.PaymentDay);

            PeriodStatus status;
            int daysOverdue = 0;
            if (paid >= lease.MonthlyRent)
            {
                status = PeriodStatus.Paid;
            }
            else if (today > dueDate)
            {
                status = PeriodStatus.Overdue;
                daysOverdue = today.DayNumber - dueDate.DayNumber;
            }
            else
            {
                status = paid > 0 ? PeriodStatus.Partial : PeriodStatus.Pending;
            }

            lines.Add(new PeriodLine(period.ToString(), dueDate, paid, balance, status, daysOverdue));
        }

        decimal overdueDebt = lines.Where(l => l.Status == PeriodStatus.Overdue).Sum(l => l.Balance);
        decimal outstanding = lines.Sum(l => l.Balance);

        return new LeaseStatement(today, lease.MonthlyRent, lines, overdueDebt, outstanding);
    }

    private static Period Min(Period a, Period b) => a <= b ? a : b;
}
