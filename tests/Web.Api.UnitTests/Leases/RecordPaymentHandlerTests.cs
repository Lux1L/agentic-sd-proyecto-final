using Web.Api.Common;
using Web.Api.Database;
using Web.Api.Features.Leases;
using Web.Api.UnitTests.Abstractions;

namespace Web.Api.UnitTests.Leases;

public class RecordPaymentHandlerTests : BaseHandlerTest
{
    private static IDateTimeProvider Clock()
    {
        IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTime(2026, 10, 30, 15, 0, 0, DateTimeKind.Utc));
        return clock;
    }

    private static async Task<Lease> AddLeaseAsync(ApplicationDbContext context, LeaseStatus status = LeaseStatus.Active)
    {
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            PropertyId = Guid.NewGuid(),
            TenantFullName = "María",
            TenantDocumentNumber = "45678912",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2027, 7, 31),
            MonthlyRent = 1200m,
            PaymentDay = 5,
            Status = status
        };
        context.Leases.Add(lease);
        await context.SaveChangesAsync();
        return lease;
    }

    private static RecordPayment.Command Cmd(
        Guid leaseId,
        string period = "2026-10",
        decimal amount = 500m,
        DateOnly? paidOn = null) =>
        new(leaseId, period, amount, paidOn ?? new DateOnly(2026, 10, 5), PaymentMethod.Yape);

    // RN-12
    [Fact]
    public async Task Handle_Should_ReturnFutureDate_WhenPaidOnIsAfterToday()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Lease lease = await AddLeaseAsync(context);
        var handler = new RecordPayment.Handler(context, Clock());

        Result<Guid> result = await handler.Handle(
            Cmd(lease.Id, paidOn: new DateOnly(2026, 10, 31)),
            CancellationToken.None);

        result.Error.ShouldBe(LeaseErrors.PaymentDateInFuture);
    }

    // RN-12
    [Fact]
    public async Task Handle_Should_Succeed_WhenPaidOnIsToday()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Lease lease = await AddLeaseAsync(context);
        var handler = new RecordPayment.Handler(context, Clock());

        Result<Guid> result = await handler.Handle(
            Cmd(lease.Id, paidOn: new DateOnly(2026, 10, 30)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    // RN-13
    [Theory]
    [InlineData("2026-07")]
    [InlineData("2027-08")]
    public async Task Handle_Should_ReturnPeriodOutsideLease_WhenPeriodIsOutOfRange(string period)
    {
        await using ApplicationDbContext context = CreateDbContext();
        Lease lease = await AddLeaseAsync(context);
        var handler = new RecordPayment.Handler(context, Clock());

        Result<Guid> result = await handler.Handle(Cmd(lease.Id, period), CancellationToken.None);

        result.Error.ShouldBe(LeaseErrors.PeriodOutsideLease(period));
    }

    // RN-13
    [Fact]
    public async Task Handle_Should_ReturnNotActive_WhenLeaseIsTerminated()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Lease lease = await AddLeaseAsync(context, LeaseStatus.Terminated);
        var handler = new RecordPayment.Handler(context, Clock());

        Result<Guid> result = await handler.Handle(Cmd(lease.Id), CancellationToken.None);

        result.Error.ShouldBe(LeaseErrors.NotActive(lease.Id));
    }

    // RN-13
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLeaseDoesNotExist()
    {
        await using ApplicationDbContext context = CreateDbContext();
        var handler = new RecordPayment.Handler(context, Clock());
        var id = Guid.NewGuid();

        Result<Guid> result = await handler.Handle(Cmd(id), CancellationToken.None);

        result.Error.ShouldBe(LeaseErrors.NotFound(id));
    }

    // RN-14
    [Fact]
    public async Task Handle_Should_ReturnExceedsRent_WhenSumIsGreaterThanRent()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Lease lease = await AddLeaseAsync(context);
        var handler = new RecordPayment.Handler(context, Clock());
        (await handler.Handle(Cmd(lease.Id, amount: 1000m), CancellationToken.None)).IsSuccess.ShouldBeTrue();

        Result<Guid> result = await handler.Handle(Cmd(lease.Id, amount: 300m), CancellationToken.None);

        result.Error.ShouldBe(LeaseErrors.PaymentExceedsRent(200m));
    }

    // RN-14
    [Fact]
    public async Task Handle_Should_AcceptPartialPaymentsUpToRent()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Lease lease = await AddLeaseAsync(context);
        var handler = new RecordPayment.Handler(context, Clock());

        Result<Guid> first = await handler.Handle(Cmd(lease.Id, amount: 700m), CancellationToken.None);
        Result<Guid> second = await handler.Handle(Cmd(lease.Id, amount: 500m), CancellationToken.None);

        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        context.Payments.Count(p => p.LeaseId == lease.Id).ShouldBe(2);
    }
}
