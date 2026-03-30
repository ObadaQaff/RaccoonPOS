using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data.Configurations;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.Employees;
using RaccoonWarehouse.Domain.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Permissions;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Relations;
using RaccoonWarehouse.Domain.Settings;
using RaccoonWarehouse.Domain.StockAdjustments;
using RaccoonWarehouse.Domain.StockLots;
using RaccoonWarehouse.Domain.Users;
using System.Linq.Expressions;
using System.Reflection;
using DelegateEntity = RaccoonWarehouse.Domain.Delegates.Delegate;
using EmployeeEntity = RaccoonWarehouse.Domain.Employees.Employee;

namespace RaccoonWarehouse.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<ReportPermission> ReportPermissions => Set<ReportPermission>();
        public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<AppSetting> AppSettings => Set<AppSetting>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(DatabaseConnectionStringProvider.GetConnectionString());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            var assembly = typeof(BaseEntity).Assembly;
            var entityTypes = assembly?.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(BaseEntity)));
            if (entityTypes == null) return;

            foreach (var type in entityTypes)
            {
                modelBuilder.Entity(type);
            }

            modelBuilder.Entity<ReportPermission>()
                .HasIndex(x => new { x.ReportKey, x.Role })
                .IsUnique();

            modelBuilder.Entity<PermissionDefinition>()
                .HasIndex(x => x.Key)
                .IsUnique();

            modelBuilder.Entity<RolePermission>()
                .HasIndex(x => new { x.Role, x.PermissionKey })
                .IsUnique();

            modelBuilder.Entity<AppSetting>()
                .HasIndex(x => x.Key)
                .IsUnique();

            modelBuilder.Entity<Account>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<JournalEntry>()
                .HasIndex(x => x.EntryNumber)
                .IsUnique();

            modelBuilder.Entity<DelegateEntity>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<DelegateEntity>()
                .HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            modelBuilder.Entity<EmployeeEntity>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<EmployeeEntity>()
                .HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            modelBuilder.Entity<EmployeeEntity>()
                .HasIndex(x => x.BranchId);

            modelBuilder.Entity<EmployeeEntity>()
                .HasIndex(x => x.DepartmentId);

            modelBuilder.Entity<EmployeeEntity>()
                .HasIndex(x => x.Status);

            modelBuilder.Entity<Invoice>()
                .HasIndex(x => x.DelegateId);

            modelBuilder.Entity<DelegateEntity>()
                .HasOne(d => d.User)
                .WithOne(u => u.DelegateProfile)
                .HasForeignKey<DelegateEntity>(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<EmployeeEntity>()
                .HasOne(e => e.User)
                .WithOne(u => u.EmployeeProfile)
                .HasForeignKey<EmployeeEntity>(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<EmployeeEntity>()
                .HasOne(e => e.Manager)
                .WithMany(e => e.DirectReports)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Delegate)
                .WithMany(d => d.Invoices)
                .HasForeignKey(i => i.DelegateId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Account>()
                .HasOne(x => x.ParentAccount)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<JournalEntryLine>()
                .HasOne(x => x.JournalEntry)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<JournalEntryLine>()
                .HasOne(x => x.Account)
                .WithMany(x => x.JournalEntryLines)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoiceLine>()
                .HasOne(il => il.Invoice)
                .WithMany(i => i.InvoiceLines)
                .HasForeignKey(il => il.InvoiceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoiceLine>()
                .HasOne(il => il.Product)
                .WithMany()
                .HasForeignKey(il => il.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoiceLine>()
                .HasOne(il => il.ProductUnit)
                .WithMany()
                .HasForeignKey(il => il.ProductUnitId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StockLot>()
                .HasOne(x => x.ReplacesStockLot)
                .WithOne(x => x.ReplacedByStockLot)
                .HasForeignKey<StockLot>(x => x.ReplacesStockLotId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StockAdjustment>()
                .HasOne(x => x.StockLot)
                .WithMany(x => x.StockAdjustments)
                .HasForeignKey(x => x.StockLotId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StockAdjustment>()
                .HasOne(x => x.NewStockLot)
                .WithMany()
                .HasForeignKey(x => x.NewStockLotId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductUnit>()
                .HasOne(pu => pu.Product)
                .WithMany(p => p.ProductUnits)
                .HasForeignKey(pu => pu.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductUnit>()
                .HasOne(pu => pu.Unit)
                .WithMany()
                .HasForeignKey(pu => pu.UnitId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SubCategoryBrand>()
                .HasOne(sb => sb.SubCategory)
                .WithMany(s => s.SubCategoryBrands)
                .HasForeignKey(sb => sb.SubCategoryId);

            modelBuilder.Entity<SubCategoryBrand>()
                .HasOne(sb => sb.Brand)
                .WithMany(b => b.SubCategoryBrands)
                .HasForeignKey(sb => sb.BrandId);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(GetIsDeletedRestriction(entityType.ClrType));
                }
            }
        }

        private static LambdaExpression GetIsDeletedRestriction(Type type)
        {
            var param = Expression.Parameter(type, "e");
            var prop = Expression.Property(param, nameof(ISoftDelete.IsDeleted));
            var condition = Expression.Equal(prop, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, param);
            return lambda;
        }
    }
}
