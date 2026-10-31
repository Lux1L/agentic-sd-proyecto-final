using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;
using Web.Api.Features.Properties;

namespace Web.Api.Features.Leases;

public static class CreateLease
{
    public sealed record Command(
        Guid PropertyId,
        string TenantFullName,
        DocumentType TenantDocumentType,
        string TenantDocumentNumber,
        string? TenantPhone,
        string? TenantEmail,
        DateOnly StartDate,
        DateOnly EndDate,
        decimal MonthlyRent,
        decimal Deposit,
        int PaymentDay) : ICommand<Guid>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.PropertyId).NotEmpty();
            RuleFor(c => c.TenantFullName).NotEmpty().MaximumLength(150);
            RuleFor(c => c.TenantDocumentType).IsInEnum();

            // RN-09
            RuleFor(c => c.TenantDocumentNumber)
                .Must((c, number) => TenantDocument.IsValid(c.TenantDocumentType, number))
                .WithMessage("El número de documento no es válido para el tipo indicado");

            When(c => !string.IsNullOrWhiteSpace(c.TenantPhone), () =>
                RuleFor(c => c.TenantPhone)
                    .Must(TenantDocument.IsValidPeruvianMobile)
                    .WithMessage("El teléfono debe ser un celular peruano de 9 dígitos que empiece con 9"));

            When(c => !string.IsNullOrWhiteSpace(c.TenantEmail), () =>
                RuleFor(c => c.TenantEmail).EmailAddress());

            // RN-06
            RuleFor(c => c.EndDate)
                .Must((c, end) => end >= c.StartDate.AddMonths(1))
                .WithMessage("La fecha de fin debe ser al menos un mes posterior a la de inicio");

            // RN-07
            RuleFor(c => c.MonthlyRent).GreaterThan(0);
            RuleFor(c => c.Deposit).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Deposit)
                .Must((c, deposit) => deposit <= 2 * c.MonthlyRent)
                .WithMessage("La garantía no puede exceder 2 veces la renta mensual");

            // RN-08
            RuleFor(c => c.PaymentDay).InclusiveBetween(1, 28);
        }
    }

    internal sealed class Handler(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command command, CancellationToken cancellationToken)
        {
            Property? property = await context.Properties
                .SingleOrDefaultAsync(p => p.Id == command.PropertyId, cancellationToken);

            if (property is null)
            {
                return Result.Failure<Guid>(PropertyErrors.NotFound(command.PropertyId));
            }

            // RN-04
            if (property.Status == PropertyStatus.Inactive)
            {
                return Result.Failure<Guid>(PropertyErrors.Inactive(property.Id));
            }

            // RN-05
            bool overlaps = await context.Leases.AnyAsync(
                l => l.PropertyId == command.PropertyId &&
                     l.Status == LeaseStatus.Active &&
                     l.StartDate <= command.EndDate &&
                     command.StartDate <= l.EndDate,
                cancellationToken);

            if (overlaps)
            {
                return Result.Failure<Guid>(LeaseErrors.Overlap);
            }

            var lease = new Lease
            {
                Id = Guid.NewGuid(),
                PropertyId = command.PropertyId,
                TenantFullName = command.TenantFullName,
                TenantDocumentType = command.TenantDocumentType,
                TenantDocumentNumber = command.TenantDocumentNumber,
                TenantPhone = string.IsNullOrWhiteSpace(command.TenantPhone) ? null : command.TenantPhone,
                TenantEmail = string.IsNullOrWhiteSpace(command.TenantEmail) ? null : command.TenantEmail,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                MonthlyRent = command.MonthlyRent,
                Deposit = command.Deposit,
                PaymentDay = command.PaymentDay,
                Status = LeaseStatus.Active,
                CreatedAt = dateTimeProvider.UtcNow
            };

            // RN-10
            property.Status = PropertyStatus.Leased;

            context.Leases.Add(lease);
            await context.SaveChangesAsync(cancellationToken);

            return lease.Id;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("leases", async (
                Command command,
                ICommandHandler<Command, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Leases)
            .RequireAuthorization();
        }
    }
}
