using System.Globalization;
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
        // Lo abonado en el período actual (alimenta el Meter del resumen).
        upToDate.GetProperty("currentPeriodPaid").GetDecimal().ShouldBe(1000m);

        // El período actual vence el día 1 (hoy >= día 1): si ya pasó el día 1, está atrasado.
        // Períodos vencidos sin pago: el mes -1 siempre, y el actual si hoy > día 1.
        decimal expectedDebt = today.Day > 1 ? 2000m : 1000m;
        JsonElement overdue = items.Single(p => p.GetProperty("propertyId").GetGuid() == overdueProperty);
        overdue.GetProperty("overdueDebt").GetDecimal().ShouldBe(expectedDebt);
        overdue.GetProperty("currentPeriodStatus").GetString().ShouldBe(today.Day > 1 ? "Overdue" : "Pending");
        overdue.GetProperty("currentPeriodPaid").GetDecimal().ShouldBe(0m);

        body.GetProperty("totalOverdueDebt").GetDecimal().ShouldBeGreaterThanOrEqualTo(expectedDebt);
        body.GetProperty("totalProperties").GetInt32().ShouldBeGreaterThanOrEqualTo(2);
        body.GetProperty("leasedProperties").GetInt32().ShouldBeGreaterThanOrEqualTo(2);
    }

    // RN-16, RN-17, RN-19
    [Fact]
    public async Task GetSummary_Should_ListOverduePeriods_When_SeveralMonthsUnpaid()
    {
        // Arrange
        DateOnly today = Today();
        var m0 = new DateOnly(today.Year, today.Month, 1);
        DateOnly start = m0.AddMonths(-3);
        var propertyId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();

        // Día de pago 1 y solo el primer período pagado: m-2 y m-1 siempre están atrasados; m0 si hoy > día 1.
        await SeedAsync(
            NewProperty(propertyId, "Varios meses atrasada", DateTime.UtcNow),
            NewLease(leaseId, propertyId, "Inquilino Moroso", start, DateTime.UtcNow),
            NewPayment(leaseId, start, 1000m, today, DateTime.UtcNow));

        // Act
        JsonElement item = await GetSummaryItemAsync(propertyId);

        // Assert
        List<string> expectedPeriods = [PeriodOf(m0.AddMonths(-2)), PeriodOf(m0.AddMonths(-1))];
        if (today.Day > 1)
        {
            expectedPeriods.Add(PeriodOf(m0));
        }

        item.GetProperty("overduePeriods").EnumerateArray().Select(p => p.GetString()).ShouldBe(expectedPeriods);
        item.GetProperty("maxDaysOverdue").GetInt32().ShouldBe(today.DayNumber - m0.AddMonths(-2).DayNumber);
        DateOf(item, "currentPeriodDueDate").ShouldBe(m0);
        DateOf(item, "nextDueDate").ShouldBe(m0.AddMonths(1));
    }

    // RN-15, RN-19
    [Fact]
    public async Task GetSummary_Should_ReportNextDueDate_When_UpToDate()
    {
        // Arrange
        DateOnly today = Today();
        var m0 = new DateOnly(today.Year, today.Month, 1);
        DateOnly start = m0.AddMonths(-2);
        var propertyId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();

        await SeedAsync(
            NewProperty(propertyId, "Al día con próximo vencimiento", DateTime.UtcNow),
            NewLease(leaseId, propertyId, "Inquilino Puntual", start, DateTime.UtcNow, paymentDay: 28),
            NewPayment(leaseId, start, 1000m, today, DateTime.UtcNow),
            NewPayment(leaseId, m0.AddMonths(-1), 1000m, today, DateTime.UtcNow),
            NewPayment(leaseId, m0, 1000m, today, DateTime.UtcNow));

        // Act
        JsonElement item = await GetSummaryItemAsync(propertyId);

        // Assert
        item.GetProperty("overduePeriods").GetArrayLength().ShouldBe(0);
        item.GetProperty("maxDaysOverdue").GetInt32().ShouldBe(0);
        DateOf(item, "currentPeriodDueDate").ShouldBe(new DateOnly(m0.Year, m0.Month, 28));
        DateOnly next = m0.AddMonths(1);
        DateOf(item, "nextDueDate").ShouldBe(new DateOnly(next.Year, next.Month, 28));
    }

    // RN-19
    [Fact]
    public async Task GetSummary_Should_ReturnEmptyCollectionFields_When_NoActiveLease()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        await SeedAsync(NewProperty(propertyId, "Disponible sin contrato", DateTime.UtcNow, PropertyStatus.Available));

        // Act
        JsonElement item = await GetSummaryItemAsync(propertyId);

        // Assert
        item.GetProperty("leaseId").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("currentPeriodDueDate").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("overduePeriods").ValueKind.ShouldBe(JsonValueKind.Array);
        item.GetProperty("overduePeriods").GetArrayLength().ShouldBe(0);
        item.GetProperty("maxDaysOverdue").GetInt32().ShouldBe(0);
        item.GetProperty("nextDueDate").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    // RN-19
    [Fact]
    public async Task GetSummary_Should_HaveNoNextDueDate_When_CurrentMonthIsLastOfLease()
    {
        // Arrange
        DateOnly today = Today();
        var m0 = new DateOnly(today.Year, today.Month, 1);
        DateOnly start = m0.AddMonths(-2);
        var propertyId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();

        // El contrato termina el último día del mes actual.
        await SeedAsync(
            NewProperty(propertyId, "Último mes del contrato", DateTime.UtcNow),
            NewLease(leaseId, propertyId, "Inquilino Saliente", start, DateTime.UtcNow, end: m0.AddMonths(1).AddDays(-1)),
            NewPayment(leaseId, start, 1000m, today, DateTime.UtcNow),
            NewPayment(leaseId, m0.AddMonths(-1), 1000m, today, DateTime.UtcNow),
            NewPayment(leaseId, m0, 1000m, today, DateTime.UtcNow));

        // Act
        JsonElement item = await GetSummaryItemAsync(propertyId);

        // Assert
        DateOf(item, "currentPeriodDueDate").ShouldBe(m0);
        item.GetProperty("nextDueDate").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("overduePeriods").GetArrayLength().ShouldBe(0);
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

    private DateOnly Today() => _factory.Services.GetRequiredService<IDateTimeProvider>().Today();

    private async Task SeedAsync(Property property, Lease? lease = null, params Payment[] payments)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Properties.Add(property);
        if (lease is not null)
        {
            context.Leases.Add(lease);
        }

        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();
    }

    private async Task<JsonElement> GetSummaryItemAsync(Guid propertyId)
    {
        (Guid _, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.GetAsync("portfolio/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("properties").EnumerateArray()
            .Single(p => p.GetProperty("propertyId").GetGuid() == propertyId);
    }

    private static string PeriodOf(DateOnly date) =>
        date.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    private static DateOnly? DateOf(JsonElement item, string name)
    {
        JsonElement value = item.GetProperty(name);
        return value.ValueKind == JsonValueKind.Null
            ? null
            : DateOnly.ParseExact(value.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static Property NewProperty(
        Guid id,
        string name,
        DateTime now,
        PropertyStatus status = PropertyStatus.Leased) => new()
    {
        Id = id,
        Name = name,
        Address = "Dirección de prueba 123",
        Type = PropertyType.Apartment,
        Status = status,
        CreatedAt = now
    };

    private static Lease NewLease(
        Guid id,
        Guid propertyId,
        string tenant,
        DateOnly start,
        DateTime now,
        int paymentDay = 1,
        DateOnly? end = null) => new()
    {
        Id = id,
        PropertyId = propertyId,
        TenantFullName = tenant,
        TenantDocumentType = DocumentType.Dni,
        TenantDocumentNumber = "12345678",
        StartDate = start,
        EndDate = end ?? start.AddMonths(12).AddDays(-1),
        MonthlyRent = 1000m,
        Deposit = 1000m,
        PaymentDay = paymentDay,
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
