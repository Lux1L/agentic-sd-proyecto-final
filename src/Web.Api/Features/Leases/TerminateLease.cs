using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;
using Web.Api.Features.Properties;

namespace Web.Api.Features.Leases;

public static class TerminateLease
{
    public sealed record Command(Guid LeaseId) : ICommand;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.LeaseId).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            Lease? lease = await context.Leases
                .SingleOrDefaultAsync(l => l.Id == command.LeaseId, cancellationToken);

            if (lease is null)
            {
                return Result.Failure(LeaseErrors.NotFound(command.LeaseId));
            }

            if (lease.Status != LeaseStatus.Active)
            {
                return Result.Failure(LeaseErrors.NotActive(lease.Id));
            }

            DateOnly today = dateTimeProvider.Today();

            List<Payment> payments = await context.Payments
                .Where(p => p.LeaseId == lease.Id)
                .ToListAsync(cancellationToken);

            var statement = LeaseStatement.Calculate(lease, payments, today);

            // RN-11
            if (statement.OutstandingBalance > 0)
            {
                return Result.Failure(LeaseErrors.HasOutstandingBalance(statement.OutstandingBalance));
            }

            lease.Status = LeaseStatus.Terminated;
            lease.TerminatedOn = today;

            bool hasOtherActive = await context.Leases.AnyAsync(
                l => l.PropertyId == lease.PropertyId && l.Status == LeaseStatus.Active && l.Id != lease.Id,
                cancellationToken);

            if (!hasOtherActive)
            {
                Property? property = await context.Properties
                    .SingleOrDefaultAsync(p => p.Id == lease.PropertyId, cancellationToken);

                if (property is { Status: PropertyStatus.Leased })
                {
                    property.Status = PropertyStatus.Available;
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("leases/{id:guid}/terminate", async (
                Guid id,
                ICommandHandler<Command> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new Command(id), cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(Tags.Leases)
            .RequireAuthorization();
        }
    }
}
