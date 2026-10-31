using Web.Api.Common;
using Web.Api.Database;
using Web.Api.Features.Leases;
using Web.Api.Features.Properties;
using Web.Api.UnitTests.Abstractions;

namespace Web.Api.UnitTests.Leases;

public class CreateLeaseHandlerTests : BaseHandlerTest
{
    private static IDateTimeProvider Clock()
    {
        IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTime(2026, 10, 30, 15, 0, 0, DateTimeKind.Utc));
        return clock;
    }

    private static async Task<Property> AddPropertyAsync(
        ApplicationDbContext context,
        PropertyStatus status = PropertyStatus.Available)
    {
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Name = "Depa 301",
            Address = "Av. Lima 123",
            Type = PropertyType.Apartment,
            Status = status
        };
        context.Properties.Add(property);
        await context.SaveChangesAsync();
        return property;
    }

    private static async Task AddLeaseAsync(
        ApplicationDbContext context,
        Guid propertyId,
        DateOnly start,
        DateOnly end,
        LeaseStatus status)
    {
        context.Leases.Add(new Lease
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            TenantFullName = "Otro",
            TenantDocumentNumber = "45678912",
            StartDate = start,
            EndDate = end,
            MonthlyRent = 1000m,
            PaymentDay = 5,
            Status = status
        });
        await context.SaveChangesAsync();
    }

    private static CreateLease.Command Cmd(Guid propertyId, DateOnly start, DateOnly end) => new(
        propertyId,
        "María Quispe",
        DocumentType.Dni,
        "45678912",
        null,
        null,
        start,
        end,
        1200m,
        1200m,
        5);

    // RN-04
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenPropertyDoesNotExist()
    {
        await using ApplicationDbContext context = CreateDbContext();
        var handler = new CreateLease.Handler(context, Clock());
        var id = Guid.NewGuid();

        Result<Guid> result = await handler.Handle(
            Cmd(id, new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31)),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PropertyErrors.NotFound(id));
    }

    // RN-04
    [Fact]
    public async Task Handle_Should_ReturnInactive_WhenPropertyIsInactive()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Property property = await AddPropertyAsync(context, PropertyStatus.Inactive);
        var handler = new CreateLease.Handler(context, Clock());

        Result<Guid> result = await handler.Handle(
            Cmd(property.Id, new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31)),
            CancellationToken.None);

        result.Error.ShouldBe(PropertyErrors.Inactive(property.Id));
    }

    // RN-05
    [Fact]
    public async Task Handle_Should_ReturnOverlap_WhenActiveLeaseOverlaps()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Property property = await AddPropertyAsync(context, PropertyStatus.Leased);
        await AddLeaseAsync(context, property.Id, new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31), LeaseStatus.Active);
        var handler = new CreateLease.Handler(context, Clock());

        Result<Guid> result = await handler.Handle(
            Cmd(property.Id, new DateOnly(2027, 7, 31), new DateOnly(2028, 7, 30)),
            CancellationToken.None);

        result.Error.ShouldBe(LeaseErrors.Overlap);
    }

    // RN-05
    [Fact]
    public async Task Handle_Should_Succeed_WhenOverlappingLeaseIsTerminated()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Property property = await AddPropertyAsync(context);
        await AddLeaseAsync(context, property.Id, new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31), LeaseStatus.Terminated);
        var handler = new CreateLease.Handler(context, Clock());

        Result<Guid> result = await handler.Handle(
            Cmd(property.Id, new DateOnly(2026, 9, 1), new DateOnly(2027, 8, 31)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    // RN-05
    [Fact]
    public async Task Handle_Should_Succeed_WhenActiveLeaseDoesNotOverlap()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Property property = await AddPropertyAsync(context, PropertyStatus.Leased);
        await AddLeaseAsync(context, property.Id, new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31), LeaseStatus.Active);
        var handler = new CreateLease.Handler(context, Clock());

        Result<Guid> result = await handler.Handle(
            Cmd(property.Id, new DateOnly(2027, 8, 1), new DateOnly(2028, 7, 31)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    // RN-10
    [Fact]
    public async Task Handle_Should_CreateActiveLeaseAndMarkPropertyLeased_WhenValid()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Property property = await AddPropertyAsync(context);
        var handler = new CreateLease.Handler(context, Clock());

        Result<Guid> result = await handler.Handle(
            Cmd(property.Id, new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Lease lease = context.Leases.Single(l => l.Id == result.Value);
        lease.Status.ShouldBe(LeaseStatus.Active);
        context.Properties.Single(p => p.Id == property.Id).Status.ShouldBe(PropertyStatus.Leased);
    }
}
