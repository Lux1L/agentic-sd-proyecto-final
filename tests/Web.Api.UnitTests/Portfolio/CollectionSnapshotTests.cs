using Web.Api.Features.Leases;
using Web.Api.Features.Portfolio;

namespace Web.Api.UnitTests.Portfolio;

public class CollectionSnapshotTests
{
    private static readonly DateOnly Today = new(2026, 10, 31);

    private static Lease CreateLease(
        DateOnly start,
        DateOnly end,
        int paymentDay,
        DateOnly? terminatedOn = null) => new()
    {
        Id = Guid.NewGuid(),
        StartDate = start,
        EndDate = end,
        MonthlyRent = 1000m,
        PaymentDay = paymentDay,
        Status = LeaseStatus.Active,
        TerminatedOn = terminatedOn
    };

    private static Payment Pay(int year, int month) => new()
    {
        Id = Guid.NewGuid(),
        PeriodYear = year,
        PeriodMonth = month,
        Amount = 1000m,
        Method = PaymentMethod.Cash
    };

    private static CollectionSnapshot Snapshot(Lease lease, IEnumerable<Payment> payments, DateOnly today) =>
        CollectionSnapshot.From(lease, LeaseStatement.Calculate(lease, payments, today));

    // RN-16, RN-17, RN-19
    [Fact]
    public void From_Should_ListOverduePeriodsOldestFirst_When_SeveralMonthsUnpaid()
    {
        Lease lease = CreateLease(new DateOnly(2026, 6, 1), new DateOnly(2027, 5, 31), paymentDay: 1);

        CollectionSnapshot snapshot = Snapshot(lease, [Pay(2026, 6), Pay(2026, 7)], Today);

        snapshot.OverduePeriods.ShouldBe(["2026-08", "2026-09", "2026-10"]);
        snapshot.MaxDaysOverdue.ShouldBe(91);
        snapshot.CurrentPeriodDueDate.ShouldBe(new DateOnly(2026, 10, 1));
        snapshot.NextDueDate.ShouldBe(new DateOnly(2026, 11, 1));
    }

    // RN-19
    [Fact]
    public void From_Should_ReportNoOverdue_When_UpToDate()
    {
        Lease lease = CreateLease(new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31), paymentDay: 5);

        CollectionSnapshot snapshot = Snapshot(lease, [Pay(2026, 8), Pay(2026, 9), Pay(2026, 10)], Today);

        snapshot.OverduePeriods.ShouldBeEmpty();
        snapshot.MaxDaysOverdue.ShouldBe(0);
        snapshot.CurrentPeriodDueDate.ShouldBe(new DateOnly(2026, 10, 5));
        snapshot.NextDueDate.ShouldBe(new DateOnly(2026, 11, 5));
    }

    // RN-16, RN-19
    [Fact]
    public void From_Should_NotListCurrentPeriod_When_NotYetDue()
    {
        Lease lease = CreateLease(new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31), paymentDay: 20);

        CollectionSnapshot snapshot = Snapshot(lease, [Pay(2026, 8)], new DateOnly(2026, 10, 10));

        snapshot.OverduePeriods.ShouldBe(["2026-09"]);
        snapshot.MaxDaysOverdue.ShouldBe(20);
        snapshot.CurrentPeriodDueDate.ShouldBe(new DateOnly(2026, 10, 20));
    }

    // RN-19
    [Fact]
    public void From_Should_HaveNoNextDueDate_When_CurrentMonthIsLastOfLease()
    {
        Lease lease = CreateLease(new DateOnly(2026, 8, 1), new DateOnly(2026, 10, 31), paymentDay: 5);

        CollectionSnapshot snapshot = Snapshot(lease, [Pay(2026, 8), Pay(2026, 9), Pay(2026, 10)], Today);

        snapshot.CurrentPeriodDueDate.ShouldBe(new DateOnly(2026, 10, 5));
        snapshot.NextDueDate.ShouldBeNull();
    }

    // RN-19
    [Fact]
    public void From_Should_HaveNextDueDate_When_NextMonthIsLastOfLease()
    {
        Lease lease = CreateLease(new DateOnly(2026, 8, 1), new DateOnly(2026, 11, 30), paymentDay: 5);

        CollectionSnapshot snapshot = Snapshot(lease, [], Today);

        snapshot.NextDueDate.ShouldBe(new DateOnly(2026, 11, 5));
    }

    // RN-19
    [Fact]
    public void From_Should_HaveNoNextDueDate_When_TerminatedThisMonth()
    {
        Lease lease = CreateLease(
            new DateOnly(2026, 8, 1),
            new DateOnly(2027, 7, 31),
            paymentDay: 5,
            terminatedOn: new DateOnly(2026, 10, 15));

        CollectionSnapshot snapshot = Snapshot(lease, [], Today);

        snapshot.NextDueDate.ShouldBeNull();
    }

    // RN-15, RN-19
    [Fact]
    public void From_Should_UseStartDateAsNextDueDate_When_LeaseStartsNextMonthAfterPaymentDay()
    {
        Lease lease = CreateLease(new DateOnly(2026, 11, 20), new DateOnly(2027, 11, 19), paymentDay: 5);

        CollectionSnapshot snapshot = Snapshot(lease, [], Today);

        snapshot.CurrentPeriodDueDate.ShouldBeNull();
        snapshot.OverduePeriods.ShouldBeEmpty();
        snapshot.MaxDaysOverdue.ShouldBe(0);
        snapshot.NextDueDate.ShouldBe(new DateOnly(2026, 11, 20));
    }

    // RN-19
    [Fact]
    public void From_Should_HaveNoNextDueDate_When_LeaseStartsAfterNextMonth()
    {
        Lease lease = CreateLease(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31), paymentDay: 5);

        CollectionSnapshot snapshot = Snapshot(lease, [], Today);

        snapshot.NextDueDate.ShouldBeNull();
    }

    // RN-19
    [Fact]
    public void None_Should_HaveEmptyValues_When_NoActiveLease()
    {
        CollectionSnapshot none = CollectionSnapshot.None;

        none.CurrentPeriodDueDate.ShouldBeNull();
        none.OverduePeriods.ShouldBeEmpty();
        none.MaxDaysOverdue.ShouldBe(0);
        none.NextDueDate.ShouldBeNull();
    }
}
