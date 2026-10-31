using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Leases;

public static class RecordPayment
{
    public sealed record Command(Guid LeaseId, string Period, decimal Amount, DateOnly PaidOn, PaymentMethod Method)
        : ICommand<Guid>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.LeaseId).NotEmpty();

            // RN-12
            RuleFor(c => c.Period)
                .Must(p => Leases.Period.TryParse(p, out _))
                .WithMessage("El período debe tener el formato YYYY-MM");
            RuleFor(c => c.Amount).GreaterThan(0);
            RuleFor(c => c.Method).IsInEnum();
            RuleFor(c => c.PaidOn).NotEqual(default(DateOnly));
        }
    }

    internal sealed class Handler(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command command, CancellationToken cancellationToken)
        {
            Lease? lease = await context.Leases
                .SingleOrDefaultAsync(l => l.Id == command.LeaseId, cancellationToken);

            if (lease is null)
            {
                return Result.Failure<Guid>(LeaseErrors.NotFound(command.LeaseId));
            }

            if (lease.Status != LeaseStatus.Active)
            {
                return Result.Failure<Guid>(LeaseErrors.NotActive(lease.Id));
            }

            // RN-12
            if (command.PaidOn > dateTimeProvider.Today())
            {
                return Result.Failure<Guid>(LeaseErrors.PaymentDateInFuture);
            }

            if (!Leases.Period.TryParse(command.Period, out Period period))
            {
                return Result.Failure<Guid>(LeaseErrors.PeriodOutsideLease(command.Period));
            }

            // RN-13
            if (period < Leases.Period.FromDate(lease.StartDate) || period > Leases.Period.FromDate(lease.EndDate))
            {
                return Result.Failure<Guid>(LeaseErrors.PeriodOutsideLease(command.Period));
            }

            // RN-14
            decimal alreadyPaid = await context.Payments
                .Where(p => p.LeaseId == lease.Id && p.PeriodYear == period.Year && p.PeriodMonth == period.Month)
                .SumAsync(p => p.Amount, cancellationToken);

            if (alreadyPaid + command.Amount > lease.MonthlyRent)
            {
                return Result.Failure<Guid>(LeaseErrors.PaymentExceedsRent(lease.MonthlyRent - alreadyPaid));
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                LeaseId = lease.Id,
                PeriodYear = period.Year,
                PeriodMonth = period.Month,
                Amount = command.Amount,
                PaidOn = command.PaidOn,
                Method = command.Method,
                CreatedAt = dateTimeProvider.UtcNow
            };

            context.Payments.Add(payment);
            await context.SaveChangesAsync(cancellationToken);

            return payment.Id;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public sealed record Request(string Period, decimal Amount, DateOnly PaidOn, PaymentMethod Method);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("leases/{id:guid}/payments", async (
                Guid id,
                Request request,
                ICommandHandler<Command, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Command(id, request.Period, request.Amount, request.PaidOn, request.Method);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Leases)
            .WithIdempotency()
            .RequireAuthorization();
        }
    }
}
