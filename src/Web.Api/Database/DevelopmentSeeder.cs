using Microsoft.EntityFrameworkCore;
using Web.Api.Authentication;
using Web.Api.Common;
using Web.Api.Features.Leases;
using Web.Api.Features.Properties;
using Web.Api.Features.Users;

namespace Web.Api.Database;

/// <summary>Datos de demostración para Development. Idempotente: no hace nada si el administrador ya existe.</summary>
public static class DevelopmentSeeder
{
    private const string AdminEmail = "admin@arriendo.pe";

    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        IPasswordHasher passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        IDateTimeProvider dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        if (await context.Users.AnyAsync(u => u.Email == AdminEmail))
        {
            return;
        }

        DateTime now = dateTimeProvider.UtcNow;
        DateOnly today = dateTimeProvider.Today();
        var m0 = new DateOnly(today.Year, today.Month, 1);

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = AdminEmail,
            FirstName = "Administrador",
            LastName = "Familiar",
            PasswordHash = passwordHasher.Hash("Demo1234!")
        });

        Property apartment = NewProperty("Dpto 201", "Av. Arequipa 1234, Lince", PropertyType.Apartment, PropertyStatus.Leased, now);
        Property room = NewProperty("Habitación 3", "Jr. Junín 455, Cercado de Lima", PropertyType.Room, PropertyStatus.Leased, now);
        Property local = NewProperty("Local comercial A", "Av. Grau 870, Barranco", PropertyType.Commercial, PropertyStatus.Leased, now);
        Property house = NewProperty("Casa Chorrillos", "Calle Los Pinos 120, Chorrillos", PropertyType.House, PropertyStatus.Available, now);
        context.Properties.AddRange(apartment, room, local, house);

        Lease l1 = NewLease(apartment, "María Quispe Huamán", DocumentType.Dni, "45678912", "987654321",
            m0.AddMonths(-3), 12, 1500m, 3000m, 5, now);
        Lease l2 = NewLease(room, "Carlos Mamani Flores", DocumentType.Ce, "001234567", null,
            m0.AddMonths(-2), 12, 650m, 650m, 10, now);
        Lease l3 = NewLease(local, "Inversiones Rímac SAC", DocumentType.Ruc, "20601234565", null,
            m0.AddMonths(-4), 24, 2800m, 5600m, 1, now);
        context.Leases.AddRange(l1, l2, l3);

        // Dpto 201: todos los períodos hasta el actual, pagados completos.
        PaymentMethod[] methods = [PaymentMethod.Yape, PaymentMethod.BankTransfer];
        for (int i = 0; i <= 3; i++)
        {
            context.Payments.Add(NewPayment(l1, m0.AddMonths(-3 + i), l1.MonthlyRent, methods[i % 2], today, now));
        }

        // Habitación 3: períodos anteriores pagados, mes actual con abono parcial.
        context.Payments.Add(NewPayment(l2, m0.AddMonths(-2), l2.MonthlyRent, PaymentMethod.Cash, today, now));
        context.Payments.Add(NewPayment(l2, m0.AddMonths(-1), l2.MonthlyRent, PaymentMethod.Yape, today, now));
        context.Payments.Add(NewPayment(l2, m0, 300m, PaymentMethod.Plin, today, now, today));

        // Local comercial A: solo los dos primeros períodos pagados.
        context.Payments.Add(NewPayment(l3, m0.AddMonths(-4), l3.MonthlyRent, PaymentMethod.BankTransfer, today, now));
        context.Payments.Add(NewPayment(l3, m0.AddMonths(-3), l3.MonthlyRent, PaymentMethod.BankTransfer, today, now));

        await context.SaveChangesAsync();
    }

    private static Property NewProperty(string name, string address, PropertyType type, PropertyStatus status, DateTime now) =>
        new() { Id = Guid.NewGuid(), Name = name, Address = address, Type = type, Status = status, CreatedAt = now };

    private static Lease NewLease(
        Property property, string tenant, DocumentType docType, string docNumber, string? phone,
        DateOnly start, int months, decimal rent, decimal deposit, int paymentDay, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            TenantFullName = tenant,
            TenantDocumentType = docType,
            TenantDocumentNumber = docNumber,
            TenantPhone = phone,
            StartDate = start,
            EndDate = start.AddMonths(months).AddDays(-1),
            MonthlyRent = rent,
            Deposit = deposit,
            PaymentDay = paymentDay,
            Status = LeaseStatus.Active,
            CreatedAt = now
        };

    private static Payment NewPayment(
        Lease lease, DateOnly period, decimal amount, PaymentMethod method, DateOnly today, DateTime now,
        DateOnly? paidOn = null)
    {
        DateOnly due = new Period(period.Year, period.Month).DueDate(lease.PaymentDay);
        DateOnly date = paidOn ?? (due < lease.StartDate ? lease.StartDate : due);

        return new Payment
        {
            Id = Guid.NewGuid(),
            LeaseId = lease.Id,
            PeriodYear = period.Year,
            PeriodMonth = period.Month,
            Amount = amount,
            PaidOn = date > today ? today : date,
            Method = method,
            CreatedAt = now
        };
    }
}
