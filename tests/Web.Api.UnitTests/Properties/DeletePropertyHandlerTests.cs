using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Database;
using Web.Api.Features.Leases;
using Web.Api.Features.Properties;
using Web.Api.UnitTests.Abstractions;

namespace Web.Api.UnitTests.Properties;

public sealed class DeletePropertyHandlerTests : BaseHandlerTest
{
    private static Property NewProperty() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Casa",
        Address = "Av. Lima 123",
        Type = PropertyType.House,
        Status = PropertyStatus.Available,
        CreatedAt = DateTime.UtcNow
    };

    // RN-03
    [Fact]
    public async Task Handle_Should_ReturnHasLeaseHistory_WhenAnyLeaseExists()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Property property = NewProperty();
        context.Properties.Add(property);
        context.Leases.Add(new Lease
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            TenantFullName = "Maria Quispe",
            TenantDocumentNumber = "12345678",
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31),
            MonthlyRent = 1000m,
            Status = LeaseStatus.Terminated,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new DeleteProperty.Handler(context);

        Result result = await handler.Handle(new DeleteProperty.Command(property.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PropertyErrors.HasLeaseHistory);
        (await context.Properties.AnyAsync(p => p.Id == property.Id)).ShouldBeTrue();
    }

    // RN-03
    [Fact]
    public async Task Handle_Should_DeleteProperty_WhenNoLeaseHistory()
    {
        await using ApplicationDbContext context = CreateDbContext();
        Property property = NewProperty();
        context.Properties.Add(property);
        await context.SaveChangesAsync();

        var handler = new DeleteProperty.Handler(context);

        Result result = await handler.Handle(new DeleteProperty.Command(property.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await context.Properties.AnyAsync(p => p.Id == property.Id)).ShouldBeFalse();
    }

    // RN-03
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenPropertyDoesNotExist()
    {
        await using ApplicationDbContext context = CreateDbContext();
        var handler = new DeleteProperty.Handler(context);
        var id = Guid.NewGuid();

        Result result = await handler.Handle(new DeleteProperty.Command(id), CancellationToken.None);

        result.Error.ShouldBe(PropertyErrors.NotFound(id));
    }
}
