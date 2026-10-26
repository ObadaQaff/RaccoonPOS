using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonWarehouse.Domain.Employees;
using RaccoonWarehouse.Domain.Payroll;
namespace RaccoonWarehouse.Data.Configurations;
public sealed class PayrollModelConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.Property(x=>x.SocialSecurityNumberEncrypted).HasMaxLength(512);
        b.Property(x=>x.NationalId).HasMaxLength(100);
        b.HasIndex(x=>x.NationalId).IsUnique().HasFilter("[NationalId] IS NOT NULL AND [NationalId] <> N''");
    }
    public static void ConfigurePayroll(ModelBuilder m)
    {
        m.Entity<PayrollPolicy>(b=>{b.ToTable("PayrollPolicy");b.Property(x=>x.Name).HasMaxLength(150);b.Property(x=>x.Category).HasMaxLength(50);b.Property(x=>x.SourceReference).HasMaxLength(2000);b.Property(x=>x.ReviewReason).HasMaxLength(1000);b.HasIndex(x=>new{x.Category,x.EffectiveFrom}).IsUnique();});
        m.Entity<EmployeeSalaryVersion>(b=>{b.ToTable("EmployeeSalaryVersion");b.Property(x=>x.ContributionCategory).HasMaxLength(50);b.Property(x=>x.ChangeReason).HasMaxLength(1000);b.HasIndex(x=>new{x.EmployeeId,x.EffectiveFrom}).IsUnique();b.HasOne(x=>x.Employee).WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict);});
        m.Entity<EmployeePayroll>(b=>{b.ToTable("EmployeePayroll");b.HasIndex(x=>new{x.EmployeeId,x.Year,x.Month}).IsUnique();b.Property(x=>x.Revision).IsConcurrencyToken();b.HasOne(x=>x.Employee).WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.SalaryVersion).WithMany().HasForeignKey(x=>x.SalaryVersionId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.Policy).WithMany().HasForeignKey(x=>x.PolicyId).OnDelete(DeleteBehavior.Restrict);});
        m.Entity<PayrollAdjustment>(b=>{b.ToTable("PayrollAdjustment");b.Property(x=>x.Reason).HasMaxLength(1000);b.Property(x=>x.LegalReference).HasMaxLength(1000);b.Property(x=>x.Notes).HasMaxLength(2000);b.HasOne(x=>x.EmployeePayroll).WithMany(x=>x.Adjustments).HasForeignKey(x=>x.EmployeePayrollId).OnDelete(DeleteBehavior.Cascade);});
        m.Entity<PayrollCorrection>(b=>{b.ToTable("PayrollCorrection");b.Property(x=>x.Reason).HasMaxLength(1000);b.HasIndex(x=>x.EmployeePayrollId);b.HasOne(x=>x.EmployeePayroll).WithMany().HasForeignKey(x=>x.EmployeePayrollId).OnDelete(DeleteBehavior.Restrict);});
        m.Entity<EmployeeEmploymentHistory>(b=>{b.ToTable("EmployeeEmploymentHistory");b.Property(x=>x.Reason).HasMaxLength(1000);b.HasIndex(x=>x.EmployeeId);b.HasOne(x=>x.Employee).WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict);});
    }
}
