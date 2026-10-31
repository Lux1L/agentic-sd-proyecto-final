using Web.Api.Features.Leases;

namespace Web.Api.UnitTests.Leases;

public class LeaseStatementTests
{
    private static Lease CreateLease(
        DateOnly? start = null,
        DateOnly? end = null,
        DateOnly? terminatedOn = null) => new()
    {
        Id = Guid.NewGuid(),
        StartDate = start ?? new DateOnly(2026, 8, 1),
        EndDate = end ?? new DateOnly(2027, 7, 31),
        MonthlyRent = 1200m,
        PaymentDay = 5,
        Status = terminatedOn is null ? LeaseStatus.Active : LeaseStatus.Terminated,
        TerminatedOn = terminatedOn
    };

    private static Payment Pay(int year, int month, decimal amount) => new()
    {
        Id = Guid.NewGuid(),
        PeriodYear = year,
        PeriodMonth = month,
        Amount = amount,
        Method = PaymentMethod.Cash
    };

    // RN-15, RN-16, RN-17
    [Fact]
    public void Calculate_Should_ReportOverdueDebt_When_OctoberIsPartiallyPaidAndPastDue()
    {
        Lease lease = CreateLease();
        Payment[] payments = [Pay(2026, 8, 1200m), Pay(2026, 9, 1200m), Pay(2026, 10, 500m)];

        var statement = LeaseStatement.Calculate(lease, payments, new DateOnly(2026, 10, 20));

        statement.Periods.Count.ShouldBe(3);
        PeriodLine october = statement.Periods[2];
        october.Status.ShouldBe(PeriodStatus.Overdue);
        october.DaysOverdue.ShouldBe(15);
        october.Balance.ShouldBe(700m);
        statement.OverdueDebt.ShouldBe(700m);
        statement.OutstandingBalance.ShouldBe(700m);
        statement.CurrentPeriod.ShouldBe(october);
    }

    // RN-16
    [Fact]
    public void Calculate_Should_MarkPending_When_CurrentMonthNotDueAndUnpaid()
    {
        var statement = LeaseStatement.Calculate(CreateLease(), [], new DateOnly(2026, 8, 3));

        statement.Periods.Single().Status.ShouldBe(PeriodStatus.Pending);
        statement.OverdueDebt.ShouldBe(0m);
        statement.OutstandingBalance.ShouldBe(1200m);
    }

    // RN-16
    [Fact]
    public void Calculate_Should_MarkPartial_When_PartialPaymentAndNotDue()
    {
        var statement = LeaseStatement.Calculate(
            CreateLease(),
            [Pay(2026, 8, 400m)],
            new DateOnly(2026, 8, 3));

        PeriodLine line = statement.Periods.Single();
        line.Status.ShouldBe(PeriodStatus.Partial);
        line.Balance.ShouldBe(800m);
    }

    // RN-16
    [Fact]
    public void Calculate_Should_NotMarkOverdue_When_TodayIsDueDate()
    {
        var statement = LeaseStatement.Calculate(CreateLease(), [], new DateOnly(2026, 8, 5));

        statement.Periods.Single().Status.ShouldBe(PeriodStatus.Pending);
        statement.Periods.Single().DaysOverdue.ShouldBe(0);
    }

    // RN-16
    [Fact]
    public void Calculate_Should_MarkOverdue_When_TodayIsAfterDueDate()
    {
        var statement = LeaseStatement.Calculate(CreateLease(), [], new DateOnly(2026, 8, 6));

        statement.Periods.Single().Status.ShouldBe(PeriodStatus.Overdue);
        statement.Periods.Single().DaysOverdue.ShouldBe(1);
    }

    // RN-15
    [Fact]
    public void Calculate_Should_ReturnEmpty_When_TodayIsBeforeStart()
    {
        var statement = LeaseStatement.Calculate(CreateLease(), [], new DateOnly(2026, 7, 31));

        statement.Periods.ShouldBeEmpty();
        statement.CurrentPeriod.ShouldBeNull();
        statement.OutstandingBalance.ShouldBe(0m);
    }

    // RN-15
    [Fact]
    public void Calculate_Should_StopAtTerminationMonth_When_LeaseTerminated()
    {
        Lease lease = CreateLease(terminatedOn: new DateOnly(2026, 9, 15));

        var statement = LeaseStatement.Calculate(lease, [], new DateOnly(2026, 12, 1));

        statement.Periods.Select(p => p.Period).ShouldBe(["2026-08", "2026-09"]);
    }

    // RN-15
    [Fact]
    public void Calculate_Should_StopAtEndMonth_When_TodayIsAfterLeaseEnd()
    {
        Lease lease = CreateLease(end: new DateOnly(2026, 10, 31));

        var statement = LeaseStatement.Calculate(lease, [], new DateOnly(2027, 3, 1));

        statement.Periods.Select(p => p.Period).ShouldBe(["2026-08", "2026-09", "2026-10"]);
    }

    // RN-17
    [Fact]
    public void Calculate_Should_MarkPaid_When_PaymentsReachRent()
    {
        var statement = LeaseStatement.Calculate(
            CreateLease(),
            [Pay(2026, 8, 700m), Pay(2026, 8, 500m)],
            new DateOnly(2026, 8, 20));

        PeriodLine line = statement.Periods.Single();
        line.Status.ShouldBe(PeriodStatus.Paid);
        line.Balance.ShouldBe(0m);
    }

    // RN-15
    [Fact]
    public void Calculate_Should_DueOnStartDate_When_LeaseStartsAfterPaymentDay_AndTodayIsStartDate()
    {
        Lease lease = CreateLease(start: new DateOnly(2026, 10, 30));

        var statement = LeaseStatement.Calculate(lease, [], new DateOnly(2026, 10, 30));

        PeriodLine line = statement.Periods.Single();
        line.Period.ShouldBe("2026-10");
        line.DueDate.ShouldBe(new DateOnly(2026, 10, 30));
        line.Status.ShouldBe(PeriodStatus.Pending);
    }

    // RN-15
    [Fact]
    public void Calculate_Should_MarkOverdueOneDay_When_LeaseStartsAfterPaymentDay_AndTodayIsNextDay()
    {
        Lease lease = CreateLease(start: new DateOnly(2026, 10, 30));

        var statement = LeaseStatement.Calculate(lease, [], new DateOnly(2026, 10, 31));

        PeriodLine line = statement.Periods.Single();
        line.DueDate.ShouldBe(new DateOnly(2026, 10, 30));
        line.Status.ShouldBe(PeriodStatus.Overdue);
        line.DaysOverdue.ShouldBe(1);
    }
}
