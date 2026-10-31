namespace Web.Api.Features.Leases;

public sealed record PeriodLine(
    string Period,
    DateOnly DueDate,
    decimal Paid,
    decimal Balance,
    PeriodStatus Status,
    int DaysOverdue);
