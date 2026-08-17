using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonWarehouse.Domain.Accounting.Operations;

namespace RaccoonWarehouse.Data.Configurations;

public sealed class AccountingOperationConfiguration : IEntityTypeConfiguration<AccountingOperation>
{
    public void Configure(EntityTypeBuilder<AccountingOperation> builder)
    {
        builder.ToTable("AccountingOperations");
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId, x.OperationType }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextAttemptDate });
        builder.Property(x => x.ReferenceType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OperationType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(4000);
    }
}
