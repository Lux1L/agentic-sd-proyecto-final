using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Properties;

public static class GetProperties
{
    public sealed record Query(string? Search, PropertyStatus? Status, int? Page, int? PageSize)
        : IQuery<PagedList<Response>>;

    public sealed record Response
    {
        public Guid Id { get; init; }

        public string Name { get; init; }

        public string Address { get; init; }

        public PropertyType Type { get; init; }

        public PropertyStatus Status { get; init; }

        public DateTime CreatedAt { get; init; }
    }

    internal sealed class Handler(ApplicationDbContext context)
        : IQueryHandler<Query, PagedList<Response>>
    {
        public async Task<Result<PagedList<Response>>> Handle(Query query, CancellationToken cancellationToken)
        {
            int page = query.Page is null or < 1 ? 1 : query.Page.Value;
            int pageSize = query.PageSize is null or < 1 or > 100 ? 20 : query.PageSize.Value;

            IQueryable<Property> properties = context.Properties.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string pattern = $"%{query.Search.Trim()}%";
                properties = properties.Where(p =>
                    EF.Functions.ILike(p.Name, pattern) || EF.Functions.ILike(p.Address, pattern));
            }

            if (query.Status.HasValue)
            {
                properties = properties.Where(p => p.Status == query.Status.Value);
            }

            IQueryable<Response> responses = properties
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Select(p => new Response
                {
                    Id = p.Id,
                    Name = p.Name,
                    Address = p.Address,
                    Type = p.Type,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                });

            return await PagedList<Response>.CreateAsync(
                responses,
                page,
                pageSize,
                cancellationToken);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("properties", async (
                string? search,
                PropertyStatus? status,
                int? page,
                int? pageSize,
                IQueryHandler<Query, PagedList<Response>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new Query(search, status, page, pageSize);

                Result<PagedList<Response>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Properties)
            .RequireAuthorization();
        }
    }
}
