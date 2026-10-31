using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Properties;

public static class DeleteProperty
{
    public sealed record Command(Guid Id) : ICommand;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Id).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext context) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            Property? property = await context.Properties
                .SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (property is null)
            {
                return Result.Failure(PropertyErrors.NotFound(command.Id));
            }

            bool hasLeaseHistory = await context.Leases.AnyAsync(
                l => l.PropertyId == command.Id,
                cancellationToken);

            if (hasLeaseHistory)
            {
                return Result.Failure(PropertyErrors.HasLeaseHistory);
            }

            context.Properties.Remove(property);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("properties/{id:guid}", async (
                Guid id,
                ICommandHandler<Command> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new Command(id), cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(Tags.Properties)
            .RequireAuthorization();
        }
    }
}
