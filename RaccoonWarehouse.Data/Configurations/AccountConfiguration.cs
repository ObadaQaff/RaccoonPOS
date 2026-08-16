using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonWarehouse.Domain.Accounting.Accounts;

namespace RaccoonWarehouse.Data.Configurations
{
    public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.HasOne(x => x.ParentAccount)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(x => x.AccountCode)
                .HasMaxLength(32);

            builder.Property(x => x.AccountNature)
                .HasMaxLength(10);

            builder.Property(x => x.AccountCategory)
                .HasMaxLength(20);

            builder.Property(x => x.AccountTypeCode)
                .HasMaxLength(2);

            builder.Property(x => x.CashFlowCategory)
                .HasConversion<int?>()
                .IsRequired(false);
        }
    }
}
