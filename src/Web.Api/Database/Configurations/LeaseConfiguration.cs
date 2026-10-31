using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Web.Api.Features.Leases;
using Web.Api.Features.Properties;

namespace Web.Api.Database.Configurations;

internal sealed class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.TenantFullName).HasMaxLength(150).IsRequired();
        builder.Property(l => l.TenantDocumentType).HasConversion<string>().HasMaxLength(10);
        builder.Property(l => l.TenantDocumentNumber).HasMaxLength(12).IsRequired();
        builder.Property(l => l.TenantPhone).HasMaxLength(9);
        builder.Property(l => l.TenantEmail).HasMaxLength(200);
        builder.Property(l => l.MonthlyRent).HasPrecision(12, 2);
        builder.Property(l => l.Deposit).HasPrecision(12, 2);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(l => new { l.PropertyId, l.Status });

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(l => l.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
