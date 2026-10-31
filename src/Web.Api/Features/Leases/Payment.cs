using Web.Api.Common;

namespace Web.Api.Features.Leases;

public sealed class Payment : Entity
{
    public Guid Id { get; set; }
    public Guid LeaseId { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime CreatedAt { get; set; }
}
