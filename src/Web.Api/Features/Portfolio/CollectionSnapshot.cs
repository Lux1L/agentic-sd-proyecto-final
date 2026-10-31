using Web.Api.Features.Leases;

namespace Web.Api.Features.Portfolio;

/// <summary>
/// Datos de cobranza de un contrato para la bandeja del resumen de cartera (RN-15..RN-17, RN-19). Función pura.
/// </summary>
public sealed record CollectionSnapshot(
    DateOnly? CurrentPeriodDueDate,
    IReadOnlyList<string> OverduePeriods,
    int MaxDaysOverdue,
    DateOnly? NextDueDate)
{
    /// <summary>Propiedad sin contrato vigente.</summary>
    public static CollectionSnapshot None { get; } = new(null, [], 0, null);

    public static CollectionSnapshot From(Lease lease, LeaseStatement statement)
    {
        // Las líneas del estado de cuenta van del período más antiguo al más reciente.
        List<PeriodLine> overdue = [.. statement.Periods.Where(p => p.Status == PeriodStatus.Overdue)];

        return new CollectionSnapshot(
            statement.CurrentPeriod?.DueDate,
            [.. overdue.Select(p => p.Period)],
            overdue.Count == 0 ? 0 : overdue.Max(p => p.DaysOverdue),
            NextPeriodDueDate(lease, statement.Today));
    }

    // RN-19: vencimiento del período siguiente al mes actual, solo si está dentro del contrato.
    private static DateOnly? NextPeriodDueDate(Lease lease, DateOnly today)
    {
        Period next = Period.FromDate(today).Next();
        var first = Period.FromDate(lease.StartDate);
        var last = Period.FromDate(lease.EndDate);
        if (lease.TerminatedOn is { } terminatedOn && Period.FromDate(terminatedOn) < last)
        {
            last = Period.FromDate(terminatedOn);
        }

        if (next < first || next > last)
        {
            return null;
        }

        // RN-15: el primer período vence como mínimo en la fecha de inicio del contrato.
        DateOnly dueDate = next.DueDate(lease.PaymentDay);
        return dueDate >= lease.StartDate ? dueDate : lease.StartDate;
    }
}
