using System.Globalization;
using Web.Api.Common;

namespace Web.Api.Features.Leases;

public static class LeaseErrors
{
    public static readonly Error Overlap = Error.Conflict(
        "Leases.Overlap",
        "Ya existe un contrato vigente para esta propiedad que se cruza con las fechas indicadas");

    public static readonly Error PaymentDateInFuture = Error.Problem(
        "Leases.PaymentDateInFuture",
        "La fecha de pago no puede ser posterior a hoy");

    public static Error NotFound(Guid leaseId) => Error.NotFound(
        "Leases.NotFound",
        $"No se encontró el contrato con Id = '{leaseId}'");

    public static Error NotActive(Guid leaseId) => Error.Conflict(
        "Leases.NotActive",
        $"El contrato con Id = '{leaseId}' no está vigente");

    public static Error HasOutstandingBalance(decimal balance) => Error.Conflict(
        "Leases.HasOutstandingBalance",
        string.Create(
            CultureInfo.InvariantCulture,
            $"El contrato tiene un saldo pendiente de S/ {balance:0.00}"));

    public static Error PeriodOutsideLease(string period) => Error.Problem(
        "Leases.PeriodOutsideLease",
        $"El período '{period}' está fuera de la vigencia del contrato");

    public static Error PaymentExceedsRent(decimal remaining) => Error.Problem(
        "Leases.PaymentExceedsRent",
        string.Create(
            CultureInfo.InvariantCulture,
            $"El pago excede la renta del período; el saldo restante es S/ {remaining:0.00}"));
}
