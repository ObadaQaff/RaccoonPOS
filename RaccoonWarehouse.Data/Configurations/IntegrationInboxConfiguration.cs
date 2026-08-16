using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonWarehouse.Domain.Integration;

namespace RaccoonWarehouse.Data.Configurations;

public sealed class IntegrationInboxConfiguration : IEntityTypeConfiguration<IntegrationInbox>
{
    public void Configure(EntityTypeBuilder<IntegrationInbox> builder)
    {
        builder.ToTable("IntegrationInbox");
        builder.HasIndex(x => x.EventId).IsUnique();
        builder.HasIndex(x => new { x.SourceSystem, x.ExternalOrderId }).IsUnique();
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SourceSystem).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ExternalOrderId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LastErrorCode).HasMaxLength(100);
        builder.Property(x => x.LastErrorSummary).HasMaxLength(1000);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
