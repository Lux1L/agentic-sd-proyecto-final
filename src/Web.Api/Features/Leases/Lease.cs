using Web.Api.Common;

namespace Web.Api.Features.Leases;

public sealed class Lease : Entity
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string TenantFullName { get; set; }
    public DocumentType TenantDocumentType { get; set; }
    public string TenantDocumentNumber { get; set; }
    public string? TenantPhone { get; set; }
    public string? TenantEmail { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal Deposit { get; set; }
    public int PaymentDay { get; set; }
    public LeaseStatus Status { get; set; }
    public DateOnly? TerminatedOn { get; set; }
    public DateTime CreatedAt { get; set; }
}
