using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Web.Api.Common;
using Web.Api.Database;
using Web.Api.Features.Leases;
using Web.Api.Features.Properties;

namespace IntegrationTests.Portfolio;

public sealed class PortfolioTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private readonly IntegrationTestWebAppFactory _factory = factory;

    // RN-16, RN-17
    [Fact]
    public async Task GetSummary_Should_ReportCurrentPeriodStatusAndOverdueDebt()
    {
        // Arrange
        DateOnly today = _factory.Services.GetRequiredService<IDateTimeProvider>().Today();
        var m0 = new DateOnly(today.Year, today.Month, 1);
        DateOnly start = m0.AddMonths(-2);

        var upToDateProperty = Guid.NewGuid();
        var overdueProperty = Guid.NewGuid();
        var upToDateLease = Guid.NewGuid();
        var overdueLease = Guid.NewGuid();

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            DateTime now = DateTime.UtcNow;

            context.Properties.AddRange(
                NewProperty(upToDateProperty, "Al día", now),
                NewProperty(overdueProperty, "Atrasada", now));

            // Día de pago 1 y contrato iniciado hace 2 meses: los períodos previos vencieron.
            context.Leases.AddRange(
                NewLease(upToDateLease, upToDateProperty, "Inquilino Al Día", start, now),
                NewLease(overdueLease, overdueProperty, "Inquilino Moroso", start, now));

            for (int i = 0; i <= 2; i++)
            {
                DateOnly period = start.AddMonths(i);
                context.Payments.Add(NewPayment(upToDateLease, period, 1000m, today, now));
            }

            // Solo el primer período se pagó; el segundo venció sin pago.
            context.Payments.Add(NewPayment(overdueLease, start, 1000m, today, now));

            await context.SaveChangesAsync();
        }

        (Guid _, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("portfolio/summary");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement[] items = [.. body.GetProperty("properties").EnumerateArray()];

        JsonElement upToDate = items.Single(p => p.GetProperty("propertyId").GetGuid() == upToDateProperty);
        upToDate.GetProperty("currentPeriodStatus").GetString().ShouldBe("Paid");
        upToDate.GetProperty("overdueDebt").GetDecimal().ShouldBe(0m);
        upToDate.GetProperty("tenantFullName").GetString().ShouldBe("Inquilino Al Día");

        // El período actual vence el día 1 (hoy >= día 1): si ya pasó el día 1, está atrasado.
        // Períodos vencidos sin pago: el mes -1 siempre, y el actual si hoy > día 1.
        decimal expectedDebt = today.Day > 1 ? 2000m : 1000m;
        JsonElement overdue = items.Single(p => p.GetProperty("propertyId").GetGuid() == overdueProperty);
        overdue.GetProperty("overdueDebt").GetDecimal().ShouldBe(expectedDebt);
        overdue.GetProperty("currentPeriodStatus").GetString().ShouldBe(today.Day > 1 ? "Overdue" : "Pending");

        body.GetProperty("totalOverdueDebt").GetDecimal().ShouldBeGreaterThanOrEqualTo(expectedDebt);
        body.GetProperty("totalProperties").GetInt32().ShouldBeGreaterThanOrEqualTo(2);
        body.GetProperty("leasedProperties").GetInt32().ShouldBeGreaterThanOrEqualTo(2);
    }

    // RNF-03
    [Fact]
    public async Task GetSummary_Should_ReturnUnauthorized_WhenNoToken()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("portfolio/summary");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static Property NewProperty(Guid id, string name, DateTime now) => new()
    {
        Id = id,
        Name = name,
        Address = "Dirección de prueba 123",
        Type = PropertyType.Apartment,
        Status = PropertyStatus.Leased,
        CreatedAt = now
    };

    private static Lease NewLease(Guid id, Guid propertyId, string tenant, DateOnly start, DateTime now) => new()
    {
        Id = id,
        PropertyId = propertyId,
        TenantFullName = tenant,
        TenantDocumentType = DocumentType.Dni,
        TenantDocumentNumber = "12345678",
        StartDate = start,
        EndDate = start.AddMonths(12).AddDays(-1),
        MonthlyRent = 1000m,
        Deposit = 1000m,
        PaymentDay = 1,
        Status = LeaseStatus.Active,
        CreatedAt = now
    };

    private static Payment NewPayment(Guid leaseId, DateOnly period, decimal amount, DateOnly today, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        LeaseId = leaseId,
        PeriodYear = period.Year,
        PeriodMonth = period.Month,
        Amount = amount,
        PaidOn = today,
        Method = PaymentMethod.Cash,
        CreatedAt = now
    };
}
