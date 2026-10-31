using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Properties;

public static class UpdateProperty
{
    public sealed record Command(Guid Id, string Name, string Address, PropertyType Type) : ICommand;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Id).NotEmpty();
            RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
            RuleFor(c => c.Address).NotEmpty().MaximumLength(200);
            RuleFor(c => c.Type).IsInEnum();
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

            property.Name = command.Name;
            property.Address = command.Address;
            property.Type = command.Type;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public sealed record Request(string Name, string Address, PropertyType Type);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("properties/{id:guid}", async (
                Guid id,
                Request request,
                ICommandHandler<Command> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Command(id, request.Name, request.Address, request.Type);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(Tags.Properties)
            .RequireAuthorization();
        }
    }
}
