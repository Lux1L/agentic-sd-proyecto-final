using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Leases;

public static class GetLeases
{
    public sealed record Query(
        Guid? PropertyId,
        LeaseStatus? Status,
        string? Search,
        int Page,
        int PageSize) : IQuery<PagedList<Response>>;

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

    internal sealed class Handler(ApplicationDbContext context)
        : IQueryHandler<Query, PagedList<Response>>
    {
        public async Task<Result<PagedList<Response>>> Handle(Query query, CancellationToken cancellationToken)
        {
            int page = query.Page < 1 ? 1 : query.Page;
            int pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

            IQueryable<Lease> leases = context.Leases;

            if (query.PropertyId is { } propertyId)
            {
                leases = leases.Where(l => l.PropertyId == propertyId);
            }

            if (query.Status is { } status)
            {
                leases = leases.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string term = $"%{query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
                leases = leases.Where(l =>
                    EF.Functions.ILike(l.TenantFullName, term) ||
                    EF.Functions.ILike(l.TenantDocumentNumber, term));
            }

            IQueryable<Response> projected = leases
                .OrderByDescending(l => l.StartDate)
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
                    });

            return await PagedList<Response>.CreateAsync(projected, page, pageSize, cancellationToken);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("leases", async (
                Guid? propertyId,
                LeaseStatus? status,
                string? search,
                int? page,
                int? pageSize,
                IQueryHandler<Query, PagedList<Response>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new Query(propertyId, status, search, page ?? 1, pageSize ?? 20);

                Result<PagedList<Response>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Leases)
            .RequireAuthorization();
        }
    }
}
