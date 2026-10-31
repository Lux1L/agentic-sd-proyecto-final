using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Web.Api.Features.Leases;

namespace Web.Api.Database.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(12, 2);
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => new { p.LeaseId, p.PeriodYear, p.PeriodMonth });

        builder.HasOne<Lease>()
            .WithMany()
            .HasForeignKey(p => p.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
