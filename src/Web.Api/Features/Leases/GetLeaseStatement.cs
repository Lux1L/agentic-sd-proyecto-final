using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Leases;

public static class GetLeaseStatement
{
    public sealed record Query(Guid LeaseId) : IQuery<Response>;

    public sealed record Response(
        Guid LeaseId,
        DateOnly Today,
        decimal MonthlyRent,
        decimal OverdueDebt,
        decimal OutstandingBalance,
        IReadOnlyList<PeriodLine> Periods);

    internal sealed class Handler(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            Lease? lease = await context.Leases
                .AsNoTracking()
                .SingleOrDefaultAsync(l => l.Id == query.LeaseId, cancellationToken);

            if (lease is null)
            {
                return Result.Failure<Response>(LeaseErrors.NotFound(query.LeaseId));
            }

            List<Payment> payments = await context.Payments
                .AsNoTracking()
                .Where(p => p.LeaseId == lease.Id)
                .ToListAsync(cancellationToken);

            var statement = LeaseStatement.Calculate(lease, payments, dateTimeProvider.Today());

            return new Response(
                lease.Id,
                statement.Today,
                statement.MonthlyRent,
                statement.OverdueDebt,
                statement.OutstandingBalance,
                statement.Periods);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("leases/{id:guid}/statement", async (
                Guid id,
                IQueryHandler<Query, Response> handler,
                CancellationToken cancellationToken) =>
            {
                Result<Response> result = await handler.Handle(new Query(id), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Leases)
            .RequireAuthorization();
        }
    }
}
