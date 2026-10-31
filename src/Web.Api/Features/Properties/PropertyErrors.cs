using Web.Api.Common;

namespace Web.Api.Features.Properties;

public static class PropertyErrors
{
    public static readonly Error HasActiveLease = Error.Conflict(
        "Properties.HasActiveLease",
        "La propiedad tiene un contrato vigente");

    public static readonly Error HasLeaseHistory = Error.Conflict(
        "Properties.HasLeaseHistory",
        "La propiedad tiene historial de contratos y no puede eliminarse");

    public static Error NotFound(Guid propertyId) => Error.NotFound(
        "Properties.NotFound",
        $"No se encontró la propiedad con Id = '{propertyId}'");

    public static Error Inactive(Guid propertyId) => Error.Conflict(
        "Properties.Inactive",
        $"La propiedad con Id = '{propertyId}' está inactiva");
}
