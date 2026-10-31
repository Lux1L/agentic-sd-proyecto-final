using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Leases;

public static class GetLeaseById
{
    public sealed record Query(Guid LeaseId) : IQuery<Response>;

    public sealed record Response
    {
        public Guid Id { get; init; }
        public Guid PropertyId { get; init; }
        public string PropertyName { get; init; }
        public string TenantFullName { get; init; }
        public DocumentType TenantDocumentType { get; init; }
        public string TenantDocumentNumber { get; init; }
        public string? TenantPhone { get; init; }
        public string? TenantEmail { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public decimal MonthlyRent { get; init; }
        public decimal Deposit { get; init; }
        public int PaymentDay { get; init; }
        public LeaseStatus Status { get; init; }
        public DateOnly? TerminatedOn { get; init; }
    }

    internal sealed class Handler(ApplicationDbContext context) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            Response? lease = await context.Leases
                .Where(l => l.Id == query.LeaseId)
                .Join(
                    context.Properties,
                    l => l.PropertyId,
                    p => p.Id,
                    (l, p) => new Response
                    {
                        Id = l.Id,
                        PropertyId = l.PropertyId,
                        PropertyName = p.Name,
                        TenantFullName = l.TenantFullName,
                        TenantDocumentType = l.TenantDocumentType,
                        TenantDocumentNumber = l.TenantDocumentNumber,
                        TenantPhone = l.TenantPhone,
                        TenantEmail = l.TenantEmail,
                        StartDate = l.StartDate,
                        EndDate = l.EndDate,
                        MonthlyRent = l.MonthlyRent,
                        Deposit = l.Deposit,
                        PaymentDay = l.PaymentDay,
                        Status = l.Status,
                        TerminatedOn = l.TerminatedOn
                    })
                .SingleOrDefaultAsync(cancellationToken);

            return lease is null
                ? Result.Failure<Response>(LeaseErrors.NotFound(query.LeaseId))
                : lease;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("leases/{id:guid}", async (
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
