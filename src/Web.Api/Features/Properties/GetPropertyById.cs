using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Properties;

public static class GetPropertyById
{
    public sealed record Query(Guid Id) : IQuery<Response>;

    public sealed record Response
    {
        public Guid Id { get; init; }

        public string Name { get; init; }

        public string Address { get; init; }

        public PropertyType Type { get; init; }

        public PropertyStatus Status { get; init; }

        public DateTime CreatedAt { get; init; }
    }

    internal sealed class Handler(ApplicationDbContext context) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            Response? property = await context.Properties
                .AsNoTracking()
                .Where(p => p.Id == query.Id)
                .Select(p => new Response
                {
                    Id = p.Id,
                    Name = p.Name,
                    Address = p.Address,
                    Type = p.Type,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (property is null)
            {
                return Result.Failure<Response>(PropertyErrors.NotFound(query.Id));
            }

            return property;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("properties/{id:guid}", async (
                Guid id,
                IQueryHandler<Query, Response> handler,
                CancellationToken cancellationToken) =>
            {
                Result<Response> result = await handler.Handle(new Query(id), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Properties)
            .RequireAuthorization();
        }
    }
}
