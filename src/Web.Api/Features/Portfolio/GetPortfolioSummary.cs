using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;
using Web.Api.Features.Leases;
using Web.Api.Features.Properties;

namespace Web.Api.Features.Portfolio;

/// <summary>CU-11: resumen de cartera (RF-07, RN-16, RN-17).</summary>
public static class GetPortfolioSummary
{
    public sealed record Query : IQuery<Response>;

    public sealed record PropertySummary(
        Guid PropertyId,
        string Name,
        PropertyType Type,
        PropertyStatus Status,
        Guid? LeaseId,
        string? TenantFullName,
        decimal? MonthlyRent,
        PeriodStatus? CurrentPeriodStatus,
        decimal OverdueDebt);

    public sealed record Response(
        DateOnly Today,
        int TotalProperties,
        int LeasedProperties,
        decimal TotalOverdueDebt,
        IReadOnlyList<PropertySummary> Properties);

    internal sealed class Handler(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            DateOnly today = dateTimeProvider.Today();

            List<Property> properties = await context.Properties
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<Lease> leases = await context.Leases
                .AsNoTracking()
                .Where(l => l.Status == LeaseStatus.Active)
                .ToListAsync(cancellationToken);

            List<Guid> leaseIds = [.. leases.Select(l => l.Id)];

            List<Payment> payments = await context.Payments
                .AsNoTracking()
                .Where(p => leaseIds.Contains(p.LeaseId))
                .ToListAsync(cancellationToken);

            ILookup<Guid, Payment> paymentsByLease = payments.ToLookup(p => p.LeaseId);
            // Con una renovación ya firmada puede haber dos contratos activos: se muestra el vigente hoy
            // y, si ninguno lo está, el que empieza antes.
            var leaseByProperty = leases
                .GroupBy(l => l.PropertyId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(l => l.StartDate <= today && today <= l.EndDate)
                        .ThenBy(l => l.StartDate)
                        .First());

            var summaries = properties
                .OrderBy(p => p.Status switch
                {
                    PropertyStatus.Leased => 0,
                    PropertyStatus.Available => 1,
                    _ => 2
                })
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(p => Summarize(p, leaseByProperty.GetValueOrDefault(p.Id), paymentsByLease, today))
                .ToList();

            return new Response(
                today,
                summaries.Count,
                summaries.Count(s => s.LeaseId is not null),
                summaries.Sum(s => s.OverdueDebt),
                summaries);
        }

        private static PropertySummary Summarize(
            Property property,
            Lease? lease,
            ILookup<Guid, Payment> paymentsByLease,
            DateOnly today)
        {
            if (lease is null)
            {
                return new PropertySummary(
                    property.Id, property.Name, property.Type, property.Status, null, null, null, null, 0m);
            }

            var statement = LeaseStatement.Calculate(lease, paymentsByLease[lease.Id], today);

            return new PropertySummary(
                property.Id,
                property.Name,
                property.Type,
                property.Status,
                lease.Id,
                lease.TenantFullName,
                lease.MonthlyRent,
                statement.CurrentPeriod?.Status,
                statement.OverdueDebt);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("portfolio/summary", async (
                IQueryHandler<Query, Response> handler,
                CancellationToken cancellationToken) =>
            {
                Result<Response> result = await handler.Handle(new Query(), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Portfolio)
            .RequireAuthorization();
        }
    }
}
