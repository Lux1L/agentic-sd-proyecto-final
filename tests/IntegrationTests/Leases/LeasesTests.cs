using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Web.Api.Database;
using Web.Api.Features.Properties;

namespace IntegrationTests.Leases;

public sealed class LeasesTests : BaseIntegrationTest
{
    private readonly IntegrationTestWebAppFactory _factory;

    public LeasesTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    private sealed record PeriodDto(string Period, decimal Paid, decimal Balance, string Status);

    private sealed record StatementDto(
        Guid LeaseId,
        decimal OverdueDebt,
        decimal OutstandingBalance,
        List<PeriodDto> Periods);

    private sealed record LeaseDto(Guid Id, string Status, string? TerminatedOn);

    private static DateOnly LimaToday() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

    private static string Fmt(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string PeriodOf(DateOnly d) => d.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    private async Task<Guid> CreatePropertyAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Name = $"Depa {Guid.NewGuid():N}",
            Address = "Av. Lima 123",
            Type = PropertyType.Apartment,
            Status = PropertyStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
        db.Properties.Add(property);
        await db.SaveChangesAsync();
        return property.Id;
    }

    private async Task AuthenticateAsync()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);
    }

    private static object LeaseBody(Guid propertyId, DateOnly start, DateOnly end) => new
    {
        propertyId,
        tenantFullName = "María Quispe Huamán",
        tenantDocumentType = "Dni",
        tenantDocumentNumber = "45678912",
        tenantPhone = "987654321",
        tenantEmail = "maria@example.com",
        startDate = Fmt(start),
        endDate = Fmt(end),
        monthlyRent = 1000m,
        deposit = 1000m,
        paymentDay = 1
    };

    private async Task<Guid> CreateLeaseAsync(Guid propertyId, DateOnly start, DateOnly end)
    {
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("leases", LeaseBody(propertyId, start, end));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private Task<HttpResponseMessage> PayAsync(Guid leaseId, string period, decimal amount) =>
        HttpClient.PostAsJsonAsync(
            $"leases/{leaseId}/payments",
            new { period, amount, paidOn = Fmt(LimaToday()), method = "Yape" });

    // RN-05
    [Fact]
    public async Task Create_Should_ReturnId()
    {
        await AuthenticateAsync();
        Guid propertyId = await CreatePropertyAsync();

        Guid id = await CreateLeaseAsync(propertyId, new DateOnly(2030, 1, 1), new DateOnly(2030, 12, 31));

        id.ShouldNotBe(Guid.Empty);
    }

    // RN-05
    [Fact]
    public async Task Create_Should_ReturnConflict_When_DatesOverlap()
    {
        await AuthenticateAsync();
        Guid propertyId = await CreatePropertyAsync();
        await CreateLeaseAsync(propertyId, new DateOnly(2030, 1, 1), new DateOnly(2030, 12, 31));

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "leases",
            LeaseBody(propertyId, new DateOnly(2030, 6, 1), new DateOnly(2031, 5, 31)));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // RN-14
    [Fact]
    public async Task RecordPayment_Should_ReturnBadRequest_When_ExceedsRent()
    {
        await AuthenticateAsync();
        Guid propertyId = await CreatePropertyAsync();
        DateOnly start = LimaToday().AddMonths(-1);
        Guid leaseId = await CreateLeaseAsync(propertyId, start, start.AddMonths(12));

        HttpResponseMessage response = await PayAsync(leaseId, PeriodOf(start), 1000.01m);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // RN-11, RN-14, RN-15, RN-16
    [Fact]
    public async Task FullFlow_Should_TrackDebt_And_Terminate_When_Paid()
    {
        await AuthenticateAsync();
        Guid propertyId = await CreatePropertyAsync();
        DateOnly today = LimaToday();
        var start = new DateOnly(today.AddMonths(-3).Year, today.AddMonths(-3).Month, 1);
        Guid leaseId = await CreateLeaseAsync(propertyId, start, start.AddMonths(12).AddDays(-1));

        // Pago parcial del primer período
        (await PayAsync(leaseId, PeriodOf(start), 400m)).EnsureSuccessStatusCode();

        StatementDto? statement = await HttpClient.GetFromJsonAsync<StatementDto>($"leases/{leaseId}/statement");
        statement.ShouldNotBeNull();
        statement.Periods.Count.ShouldBe(4);
        statement.OverdueDebt.ShouldBeGreaterThan(0m);
        statement.OutstandingBalance.ShouldBe(4000m - 400m);

        // Con saldo no se puede terminar
        HttpResponseMessage blocked = await HttpClient.PostAsync($"leases/{leaseId}/terminate", null);
        blocked.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // Pagar todos los saldos
        foreach (PeriodDto period in statement.Periods.Where(p => p.Balance > 0))
        {
            (await PayAsync(leaseId, period.Period, period.Balance)).EnsureSuccessStatusCode();
        }

        StatementDto? settled = await HttpClient.GetFromJsonAsync<StatementDto>($"leases/{leaseId}/statement");
        settled!.OutstandingBalance.ShouldBe(0m);

        HttpResponseMessage terminated = await HttpClient.PostAsync($"leases/{leaseId}/terminate", null);
        terminated.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        LeaseDto? lease = await HttpClient.GetFromJsonAsync<LeaseDto>($"leases/{leaseId}");
        lease!.Status.ShouldBe("Terminated");
        lease.TerminatedOn.ShouldBe(Fmt(today));
    }
}
