using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data.Configurations;
using RaccoonWarehouse.Domain.Accounting.AccountOpeningBalances;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Banks;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Accounting.Operations;
using RaccoonWarehouse.Domain.Accounting.Periods;
using RaccoonWarehouse.Domain.Accounting.RecurringJournals;
using RaccoonWarehouse.Domain.Accounting.TaxRates;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Branches;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.CostCenters;
using RaccoonWarehouse.Domain.Currencies;
using RaccoonWarehouse.Domain.Employees;
using RaccoonWarehouse.Domain.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.FinancialTransactions;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Integration;
using RaccoonWarehouse.Domain.Permissions;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Relations;
using RaccoonWarehouse.Domain.Settings;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.StockAdjustments;
using RaccoonWarehouse.Domain.StockDocuments;
using RaccoonWarehouse.Domain.StockItems;
using RaccoonWarehouse.Domain.StockLots;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Vouchers;
using System.Linq.Expressions;
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
        public DbSet<AccountingOperation> AccountingOperations => Set<AccountingOperation>();
        public DbSet<RecurringJournal> RecurringJournals => Set<RecurringJournal>();
        public DbSet<RecurringJournalLine> RecurringJournalLines => Set<RecurringJournalLine>();
        public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
        public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
        public DbSet<AccountOpeningBalance> AccountOpeningBalances => Set<AccountOpeningBalance>();
        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
        public DbSet<BankStatement> BankStatements => Set<BankStatement>();
        public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();
        public DbSet<TaxRate> TaxRates => Set<TaxRate>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<CostCenter> CostCenters => Set<CostCenter>();
        public DbSet<Currency> Currencies => Set<Currency>();
        public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
        public DbSet<IntegrationInbox> IntegrationInbox => Set<IntegrationInbox>();

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
            modelBuilder.ApplyConfiguration(new AccountConfiguration());
            modelBuilder.ApplyConfiguration(new IntegrationInboxConfiguration());
            modelBuilder.ApplyConfiguration(new AccountingOperationConfiguration());

            var assembly = typeof(BaseEntity).Assembly;
            var entityTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(BaseEntity)));

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

            modelBuilder.Entity<JournalEntry>()
                .HasIndex(x => x.EntryNumber)
                .IsUnique();

            modelBuilder.Entity<JournalEntryLine>()
                .HasIndex(x => new { x.JournalEntryId, x.LineNumber })
                .IsUnique();

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

            modelBuilder.Entity<RecurringJournal>()
                .HasIndex(x => x.NextRunDate);

            modelBuilder.Entity<RecurringJournalLine>()
                .HasOne(x => x.RecurringJournal)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.RecurringJournalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecurringJournalLine>()
                .HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RecurringJournalLine>()
                .HasOne(x => x.CostCenter)
                .WithMany()
                .HasForeignKey(x => x.CostCenterId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FiscalYear>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<AccountingPeriod>()
                .HasIndex(x => new { x.FiscalYearId, x.PeriodNumber })
                .IsUnique();

            modelBuilder.Entity<AccountingPeriod>()
                .HasOne(x => x.FiscalYear)
                .WithMany(x => x.AccountingPeriods)
                .HasForeignKey(x => x.FiscalYearId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AccountOpeningBalance>()
                .HasIndex(x => new { x.FiscalYearId, x.AccountId, x.BranchId, x.CostCenterId, x.WarehouseId, x.PartyUserId });

            modelBuilder.Entity<AccountOpeningBalance>()
                .HasOne(x => x.FiscalYear)
                .WithMany()
                .HasForeignKey(x => x.FiscalYearId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AccountOpeningBalance>()
                .HasOne(x => x.Account)
                .WithMany(x => x.OpeningBalances)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BankAccount>()
                .HasIndex(x => x.AccountNumber)
                .IsUnique();

            modelBuilder.Entity<BankAccount>()
                .HasOne(x => x.GlAccount)
                .WithMany()
                .HasForeignKey(x => x.GlAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BankAccount>()
                .HasOne(x => x.Currency)
                .WithMany()
                .HasForeignKey(x => x.CurrencyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BankStatement>()
                .HasIndex(x => new { x.BankAccountId, x.StatementDate });

            modelBuilder.Entity<BankStatement>()
                .HasOne(x => x.BankAccount)
                .WithMany(x => x.Statements)
                .HasForeignKey(x => x.BankAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BankStatementLine>()
                .HasIndex(x => new { x.BankStatementId, x.TransactionDate });

            modelBuilder.Entity<BankStatementLine>()
                .HasOne(x => x.BankStatement)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.BankStatementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BankStatementLine>()
                .HasOne(x => x.MatchedJournalEntryLine)
                .WithMany()
                .HasForeignKey(x => x.MatchedJournalEntryLineId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TaxRate>()
                .HasOne(x => x.TaxAccount)
                .WithMany()
                .HasForeignKey(x => x.TaxAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TaxRate>()
                .HasIndex(x => new { x.Name, x.TaxType });

            modelBuilder.Entity<JournalEntryLine>()
                .HasIndex(x => x.TaxRateId);

            modelBuilder.Entity<Branch>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<CostCenter>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<CostCenter>()
                .HasOne(x => x.ParentCostCenter)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentCostCenterId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Currency>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<ExchangeRate>()
                .HasIndex(x => new { x.FromCurrencyId, x.ToCurrencyId, x.EffectiveDate });

            modelBuilder.Entity<ExchangeRate>()
                .HasOne(x => x.FromCurrency)
                .WithMany()
                .HasForeignKey(x => x.FromCurrencyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExchangeRate>()
                .HasOne(x => x.ToCurrency)
                .WithMany()
                .HasForeignKey(x => x.ToCurrencyId)
                .OnDelete(DeleteBehavior.NoAction);

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

            modelBuilder.Entity<Invoice>()
                .HasIndex(x => x.ReferenceNumber);

            modelBuilder.Entity<Invoice>()
                .HasIndex(x => x.DueDate);

            modelBuilder.Entity<Voucher>()
                .HasIndex(x => x.ReferenceNumber);

            modelBuilder.Entity<FinancialTransaction>()
                .HasIndex(x => new { x.SourceType, x.SourceId });

            modelBuilder.Entity<StockDocument>()
                .HasIndex(x => x.DocumentNumber)
                .IsUnique();

            modelBuilder.Entity<StockItem>()
                .HasIndex(x => new { x.StockDocumentId, x.LineNumber });

            modelBuilder.Entity<Stock>()
                .HasIndex(x => new { x.WarehouseId, x.ProductId, x.ProductUnitId });

            modelBuilder.Entity<StockTransaction>()
                .HasIndex(x => new { x.SourceType, x.SourceId });

            modelBuilder.Entity<StockTransaction>()
                .HasIndex(x => x.StockDocumentId);

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

            modelBuilder.Entity<StockItem>()
                .HasOne(si => si.StockDocument)
                .WithMany(sd => sd.Items)
                .HasForeignKey(si => si.StockDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Invoice)
                .WithMany()
                .HasForeignKey(st => st.InvoiceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Voucher)
                .WithMany()
                .HasForeignKey(st => st.VoucherId)
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
            return Expression.Lambda(condition, param);
        }
    }
}
