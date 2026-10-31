using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Web.Api.Database;
using Web.Api.Features.Leases;

namespace IntegrationTests.Properties;

public sealed class PropertiesTests : BaseIntegrationTest
{
    private readonly IntegrationTestWebAppFactory _factory;

    public PropertiesTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    private sealed record PropertyDto(Guid Id, string Name, string Address, string Type, string Status);

    private sealed record PagedDto(List<PropertyDto> Items, int TotalCount);

    private async Task<Guid> CreatePropertyAsync(string? name = null)
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "properties",
            new { name = name ?? $"Casa-{Guid.NewGuid():N}", address = "Av. Lima 123", type = "House" });
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task AddLeaseAsync(Guid propertyId, LeaseStatus status)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Leases.Add(new Lease
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            TenantFullName = "Maria Quispe",
            TenantDocumentType = DocumentType.Dni,
            TenantDocumentNumber = "12345678",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            MonthlyRent = 1000m,
            Deposit = 1000m,
            PaymentDay = 5,
            Status = status,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    // RN-01
    [Fact]
    public async Task Create_Should_ReturnId()
    {
        Guid id = await CreatePropertyAsync();

        id.ShouldNotBe(Guid.Empty);
    }

    // RN-01
    [Fact]
    public async Task Create_Should_ReturnBadRequest_When_NameMissing()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "properties",
            new { name = string.Empty, address = "Av. Lima 123", type = "House" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // RNF-04: los errores de validación vienen agrupados por campo (camelCase), como dice el contrato de la API.
        using var problem = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        System.Text.Json.JsonElement errors = problem.RootElement.GetProperty("errors");
        errors.GetProperty("name").GetArrayLength().ShouldBeGreaterThan(0);
    }

    // RN-01
    [Fact]
    public async Task GetById_Should_ReturnProperty()
    {
        Guid id = await CreatePropertyAsync("Depa Miraflores");

        PropertyDto? property = await HttpClient.GetFromJsonAsync<PropertyDto>($"properties/{id}");

        property.ShouldNotBeNull();
        property.Name.ShouldBe("Depa Miraflores");
        property.Status.ShouldBe("Available");
        property.Type.ShouldBe("House");
    }

    // RN-01
    [Fact]
    public async Task GetProperties_Should_FilterByStatus()
    {
        Guid activeId = await CreatePropertyAsync();
        Guid inactiveId = await CreatePropertyAsync();
        (await HttpClient.PostAsync($"properties/{inactiveId}/deactivate", null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        PagedDto? page = await HttpClient.GetFromJsonAsync<PagedDto>("properties?status=Inactive&pageSize=100");

        page.ShouldNotBeNull();
        page.Items.ShouldContain(p => p.Id == inactiveId);
        page.Items.ShouldNotContain(p => p.Id == activeId);
        page.Items.ShouldAllBe(p => p.Status == "Inactive");
    }

    // RN-02
    [Fact]
    public async Task Deactivate_Should_ReturnConflict_When_ActiveLease()
    {
        Guid id = await CreatePropertyAsync();
        await AddLeaseAsync(id, LeaseStatus.Active);

        HttpResponseMessage response = await HttpClient.PostAsync($"properties/{id}/deactivate", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // RN-03
    [Fact]
    public async Task Delete_Should_ReturnConflict_When_LeaseHistory()
    {
        Guid id = await CreatePropertyAsync();
        await AddLeaseAsync(id, LeaseStatus.Terminated);

        HttpResponseMessage response = await HttpClient.DeleteAsync($"properties/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // RN-03
    [Fact]
    public async Task Delete_Should_ReturnNoContent_When_NoHistory()
    {
        Guid id = await CreatePropertyAsync();

        HttpResponseMessage response = await HttpClient.DeleteAsync($"properties/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await HttpClient.GetAsync($"properties/{id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // RN-01
    [Fact]
    public async Task Requests_Should_ReturnUnauthorized_When_NoToken()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("properties");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
