using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Database;
using Web.Api.Features.Leases;
using Web.Api.Features.Properties;
using Web.Api.UnitTests.Abstractions;

namespace Web.Api.UnitTests.Properties;

public sealed class DeactivatePropertyHandlerTests : BaseHandlerTest
{
    private static Property NewProperty() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Casa",
        Address = "Av. Lima 123",
        Type = PropertyType.House,
        Status = PropertyStatus.Leased,
        CreatedAt = DateTime.UtcNow
    };

    private static Lease NewLease(Guid propertyId, LeaseStatus status) => new()
    {
        Id = Guid.NewGuid(),
        PropertyId = propertyId,
        TenantFullName = "Maria Quispe",
        TenantDocumentNumber = "12345678",
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31),
        MonthlyRent = 1000m,
        Status = status,
        CreatedAt = DateTime.UtcNow
    };

    // RN-02
    [Fact]
    public async Task Handle_Should_ReturnHasActiveLease_WhenActiveLeaseExists()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Property property = NewProperty();
        context.Properties.Add(property);
        context.Leases.Add(NewLease(property.Id, LeaseStatus.Active));
        await context.SaveChangesAsync();

        var handler = new DeactivateProperty.Handler(context);

        Result result = await handler.Handle(new DeactivateProperty.Command(property.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PropertyErrors.HasActiveLease);
    }

    // RN-02
    [Fact]
    public async Task Handle_Should_SetInactive_WhenNoActiveLease()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Property property = NewProperty();
        context.Properties.Add(property);
        context.Leases.Add(NewLease(property.Id, LeaseStatus.Terminated));
        await context.SaveChangesAsync();

        var handler = new DeactivateProperty.Handler(context);

        Result result = await handler.Handle(new DeactivateProperty.Command(property.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Property stored = await context.Properties.SingleAsync(p => p.Id == property.Id);
        stored.Status.ShouldBe(PropertyStatus.Inactive);
    }

    // RN-02
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenPropertyDoesNotExist()
    {
        await using ApplicationDbContext context = CreateDbContext();
        var handler = new DeactivateProperty.Handler(context);
        var id = Guid.NewGuid();

        Result result = await handler.Handle(new DeactivateProperty.Command(id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PropertyErrors.NotFound(id));
    }
}
