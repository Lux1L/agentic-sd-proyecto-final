using FluentValidation;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Properties;

public static class CreateProperty
{
    public sealed record Command(string Name, string Address, PropertyType Type) : ICommand<Guid>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
            RuleFor(c => c.Address).NotEmpty().MaximumLength(200);
            RuleFor(c => c.Type).IsInEnum();
        }
    }

    internal sealed class Handler(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command command, CancellationToken cancellationToken)
        {
            var property = new Property
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Address = command.Address,
                Type = command.Type,
                Status = PropertyStatus.Available,
                CreatedAt = dateTimeProvider.UtcNow
            };

            context.Properties.Add(property);

            await context.SaveChangesAsync(cancellationToken);

            return property.Id;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public sealed record Request(string Name, string Address, PropertyType Type);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("properties", async (
                Request request,
                ICommandHandler<Command, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Command(request.Name, request.Address, request.Type);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Properties)
            .RequireAuthorization();
        }
    }
}
