using Web.Api.Common;
using Web.Api.Database;
using Web.Api.Features.Leases;
using Web.Api.Features.Properties;
using Web.Api.UnitTests.Abstractions;

namespace Web.Api.UnitTests.Leases;

public class TerminateLeaseHandlerTests : BaseHandlerTest
{
    private static IDateTimeProvider Clock()
    {
        IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTime(2026, 10, 30, 15, 0, 0, DateTimeKind.Utc));
        return clock;
    }

    private static async Task<(Lease Lease, Property Property)> SeedAsync(ApplicationDbContext context)
    {
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Name = "Depa",
            Address = "Av. Lima",
            Type = PropertyType.Apartment,
            Status = PropertyStatus.Leased
        };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            TenantFullName = "María",
            TenantDocumentNumber = "45678912",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 8, 31),
            MonthlyRent = 1000m,
            PaymentDay = 5,
            Status = LeaseStatus.Active
        };
        context.Properties.Add(property);
        context.Leases.Add(lease);
        await context.SaveChangesAsync();
        return (lease, property);
    }

    private static void Pay(ApplicationDbContext context, Guid leaseId, int month) =>
        context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            PeriodYear = 2026,
            PeriodMonth = month,
            Amount = 1000m,
            PaidOn = new DateOnly(2026, month, 5),
            Method = PaymentMethod.Cash
        });

    // RN-11
    [Fact]
    public async Task Handle_Should_ReturnOutstandingBalance_WhenLeaseHasDebt()
    {
        await using ApplicationDbContext context = CreateDbContext();
        (Lease lease, _) = await SeedAsync(context);
        Pay(context, lease.Id, 9);
        await context.SaveChangesAsync();
        var handler = new TerminateLease.Handler(context, Clock());

        Result result = await handler.Handle(new TerminateLease.Command(lease.Id), CancellationToken.None);

        result.Error.ShouldBe(LeaseErrors.HasOutstandingBalance(1000m));
        context.Leases.Single().Status.ShouldBe(LeaseStatus.Active);
    }

    // RN-11
    [Fact]
    public async Task Handle_Should_TerminateAndFreeProperty_WhenNoBalance()
    {
        await using ApplicationDbContext context = CreateDbContext();
        (Lease lease, Property property) = await SeedAsync(context);
        Pay(context, lease.Id, 9);
        Pay(context, lease.Id, 10);
        await context.SaveChangesAsync();
        var handler = new TerminateLease.Handler(context, Clock());

        Result result = await handler.Handle(new TerminateLease.Command(lease.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Lease saved = context.Leases.Single();
        saved.Status.ShouldBe(LeaseStatus.Terminated);
        saved.TerminatedOn.ShouldBe(new DateOnly(2026, 10, 30));
        context.Properties.Single(p => p.Id == property.Id).Status.ShouldBe(PropertyStatus.Available);
    }

    // RN-11
    [Fact]
    public async Task Handle_Should_ReturnNotActive_WhenAlreadyTerminated()
    {
        await using ApplicationDbContext context = CreateDbContext();
        (Lease lease, _) = await SeedAsync(context);
        lease.Status = LeaseStatus.Terminated;
        await context.SaveChangesAsync();
        var handler = new TerminateLease.Handler(context, Clock());

        Result result = await handler.Handle(new TerminateLease.Command(lease.Id), CancellationToken.None);

        result.Error.ShouldBe(LeaseErrors.NotActive(lease.Id));
    }
}
