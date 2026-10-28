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
using RaccoonWarehouse.Domain.CustomerRequests;
using RaccoonWarehouse.Domain.CostCenters;
using RaccoonWarehouse.Domain.Currencies;
using RaccoonWarehouse.Domain.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.FinancialTransactions;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.LostCustomers;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Integration;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Domain.PurchaseOrders;
using RaccoonWarehouse.Domain.PurchaseRequests;
using RaccoonWarehouse.Domain.GoodsReceipts;
using RaccoonWarehouse.Domain.PurchaseInvoiceMatches;
using RaccoonWarehouse.Domain.SupplierPayments;
using RaccoonWarehouse.Domain.PurchaseReturns;
using RaccoonWarehouse.Domain.PurchaseControl;
using RaccoonWarehouse.Domain.Audit;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.POS;
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

namespace RaccoonWarehouse.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
        public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
        public DbSet<PurchaseRequestLine> PurchaseRequestLines => Set<PurchaseRequestLine>();
        public DbSet<CustomerRequest> CustomerRequests => Set<CustomerRequest>();
        public DbSet<CustomerRequestLine> CustomerRequestLines => Set<CustomerRequestLine>();
        public DbSet<LostCustomer> LostCustomers => Set<LostCustomer>();
        public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
        public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
        public DbSet<PurchaseInvoiceMatch> PurchaseInvoiceMatches => Set<PurchaseInvoiceMatch>();
        public DbSet<PurchaseInvoiceMatchLine> PurchaseInvoiceMatchLines => Set<PurchaseInvoiceMatchLine>();
        public DbSet<SupplierPaymentAllocation> SupplierPaymentAllocations => Set<SupplierPaymentAllocation>();
        public DbSet<SupplierAdvance> SupplierAdvances => Set<SupplierAdvance>();
        public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
        public DbSet<PurchaseReturnLine> PurchaseReturnLines => Set<PurchaseReturnLine>();
        public DbSet<PurchaseControlEntry> PurchaseControlEntries => Set<PurchaseControlEntry>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
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
        public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();
        public DbSet<InsuranceCompany> InsuranceCompanies => Set<InsuranceCompany>();
        public DbSet<InsuranceNetwork> InsuranceNetworks => Set<InsuranceNetwork>();
        public DbSet<InsuranceBeneficiaryCategory> InsuranceBeneficiaryCategories => Set<InsuranceBeneficiaryCategory>();
        public DbSet<InsuranceContract> InsuranceContracts => Set<InsuranceContract>();
        public DbSet<InsuranceContractVersion> InsuranceContractVersions => Set<InsuranceContractVersion>();
        public DbSet<InsurancePlan> InsurancePlans => Set<InsurancePlan>();
        public DbSet<InsuranceCoverageRule> InsuranceCoverageRules => Set<InsuranceCoverageRule>();
        public DbSet<InsuranceExclusion> InsuranceExclusions => Set<InsuranceExclusion>();
        public DbSet<InsuranceMemberPolicy> InsuranceMemberPolicies => Set<InsuranceMemberPolicy>();
        public DbSet<InsuranceEligibilityCheck> InsuranceEligibilityChecks => Set<InsuranceEligibilityCheck>();
        public DbSet<InsuranceInvoiceCoverage> InsuranceInvoiceCoverages => Set<InsuranceInvoiceCoverage>();
        public DbSet<InsuranceInvoiceCoverageLine> InsuranceInvoiceCoverageLines => Set<InsuranceInvoiceCoverageLine>();
        public DbSet<InsuranceClaimBatch> InsuranceClaimBatches => Set<InsuranceClaimBatch>();
        public DbSet<InsuranceClaim> InsuranceClaims => Set<InsuranceClaim>();
        public DbSet<InsuranceClaimLine> InsuranceClaimLines => Set<InsuranceClaimLine>();
        public DbSet<InsuranceClaimAdjustment> InsuranceClaimAdjustments => Set<InsuranceClaimAdjustment>();
        public DbSet<InsuranceClaimPayment> InsuranceClaimPayments => Set<InsuranceClaimPayment>();
        public DbSet<InsuranceClaimPaymentAllocation> InsuranceClaimPaymentAllocations => Set<InsuranceClaimPaymentAllocation>();
        public DbSet<InsurancePreAuthorization> InsurancePreAuthorizations => Set<InsurancePreAuthorization>();
        public DbSet<InsurancePreAuthorizationLine> InsurancePreAuthorizationLines => Set<InsurancePreAuthorizationLine>();
        public DbSet<InsuranceAttachment> InsuranceAttachments => Set<InsuranceAttachment>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(DatabaseConnectionStringProvider.GetConnectionString());
            }
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<decimal>()
                .HavePrecision(18, 6);
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

            modelBuilder.HasSequence<int>("UserNumberSequence", "dbo")
                .StartsAt(1)
                .IncrementsBy(1);
            modelBuilder.Entity<User>()
                .Property(x => x.UserNumber)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("NEXT VALUE FOR dbo.UserNumberSequence");
            modelBuilder.Entity<User>()
                .HasIndex(x => x.UserNumber)
                .IsUnique();

            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(x => x.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<PurchaseOrderLine>()
                .HasOne(x => x.PurchaseOrder)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PurchaseOrderLine>()
                .HasIndex(x => x.PurchaseRequestLineId);

            modelBuilder.Entity<PurchaseRequest>()
                .HasIndex(x => x.RequestNumber)
                .IsUnique();

            modelBuilder.Entity<PurchaseRequestLine>()
                .HasOne(x => x.PurchaseRequest)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PurchaseRequestLine>()
                .HasIndex(x => new { x.PurchaseRequestId, x.ProductId, x.ProductUnitId })
                .IsUnique();

            modelBuilder.Entity<PurchaseRequestLine>()
                .Property(x => x.ItemName)
                .HasMaxLength(500);

            modelBuilder.Entity<PurchaseRequest>()
                .Property(x => x.RequesterName)
                .HasMaxLength(200);

            modelBuilder.Entity<PurchaseRequestLine>()
                .Property(x => x.Barcode)
                .HasMaxLength(100);

            modelBuilder.Entity<PurchaseRequestLine>()
                .Property(x => x.UnitName)
                .HasMaxLength(100);

            modelBuilder.Entity<PurchaseRequestLine>()
                .Property(x => x.SupplierName)
                .HasMaxLength(200);

            modelBuilder.Entity<GoodsReceipt>()
                .HasIndex(x => x.ReceiptNumber)
                .IsUnique();

            modelBuilder.Entity<GoodsReceiptLine>()
                .HasOne(x => x.GoodsReceipt)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GoodsReceiptLine>()
                .HasIndex(x => new { x.GoodsReceiptId, x.PurchaseOrderLineId });

            modelBuilder.Entity<PurchaseInvoiceMatch>()
                .HasIndex(x => x.InvoiceId)
                .IsUnique();

            modelBuilder.Entity<PurchaseInvoiceMatchLine>()
                .HasOne(x => x.PurchaseInvoiceMatch)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.PurchaseInvoiceMatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PurchaseInvoiceMatchLine>()
                .HasIndex(x => new { x.PurchaseInvoiceMatchId, x.InvoiceLineId })
                .IsUnique();

            modelBuilder.Entity<SupplierPaymentAllocation>()
                .HasIndex(x => new { x.VoucherId, x.InvoiceId })
                .IsUnique();

            modelBuilder.Entity<SupplierPaymentAllocation>()
                .HasIndex(x => x.InvoiceId);

            modelBuilder.Entity<SupplierAdvance>()
                .HasIndex(x => x.VoucherId)
                .IsUnique();

            modelBuilder.Entity<PurchaseReturn>()
                .HasIndex(x => x.ReturnNumber)
                .IsUnique();

            modelBuilder.Entity<PurchaseReturnLine>()
                .HasOne(x => x.PurchaseReturn)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.PurchaseReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(x => new { x.CreatedDate, x.UserId });

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.Action)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<AuditLog>()
                .Property(x => x.EntityType)
                .HasMaxLength(200);

            modelBuilder.Entity<AppSetting>()
                .HasIndex(x => x.Key)
                .IsUnique();

            modelBuilder.Entity<InsuranceCompany>()
                .Property(x => x.ContractualDiscountPercent)
                .HasPrecision(18, 6)
                .HasDefaultValue(0m);
            modelBuilder.Entity<InsuranceCompany>()
                .HasIndex(x => x.Code)
                .IsUnique();
            modelBuilder.Entity<InsuranceNetwork>()
                .HasIndex(x => x.Code)
                .IsUnique();
            modelBuilder.Entity<InsuranceBeneficiaryCategory>()
                .HasIndex(x => x.Code)
                .IsUnique();
            modelBuilder.Entity<InsuranceContract>()
                .HasIndex(x => x.ContractNumber)
                .IsUnique();
            modelBuilder.Entity<InsuranceContractVersion>()
                .HasIndex(x => new { x.InsuranceContractId, x.VersionNumber })
                .IsUnique();
            modelBuilder.Entity<InsurancePlan>()
                .HasIndex(x => new { x.InsuranceContractId, x.Code })
                .IsUnique();
            modelBuilder.Entity<InsuranceMemberPolicy>()
                .HasIndex(x => new { x.InsuranceCompanyId, x.MemberNumber });
            modelBuilder.Entity<InsuranceEligibilityCheck>()
                .HasIndex(x => new { x.InsuranceMemberPolicyId, x.CheckedAt });
            modelBuilder.Entity<InsuranceInvoiceCoverage>()
                .HasIndex(x => x.InvoiceId)
                .IsUnique();
            modelBuilder.Entity<InsuranceInvoiceCoverageLine>()
                .HasIndex(x => new { x.InsuranceInvoiceCoverageId, x.InvoiceLineId })
                .IsUnique();
            modelBuilder.Entity<InsuranceClaim>()
                .HasIndex(x => x.ClaimNumber)
                .IsUnique();
            modelBuilder.Entity<InsuranceClaim>()
                .HasIndex(x => new { x.InsuranceCompanyId, x.Status, x.SubmissionDate });
            modelBuilder.Entity<InsuranceClaimLine>()
                .HasIndex(x => new { x.InsuranceClaimId, x.InvoiceLineId })
                .IsUnique();
            modelBuilder.Entity<InsuranceClaimPaymentAllocation>()
                .HasIndex(x => new { x.InsuranceClaimPaymentId, x.InsuranceClaimId })
                .IsUnique();
            modelBuilder.Entity<InsurancePreAuthorization>()
                .HasIndex(x => x.AuthorizationNumber)
                .IsUnique();
            modelBuilder.Entity<InsurancePreAuthorizationLine>()
                .HasIndex(x => new { x.InsurancePreAuthorizationId, x.ProductId })
                .IsUnique();
            modelBuilder.Entity<InsuranceAttachment>()
                .HasIndex(x => new { x.OwnerType, x.OwnerId });

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

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Delegate)
                .WithMany(d => d.Invoices)
                .HasForeignKey(i => i.DelegateId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoiceLine>().Property(x => x.BonusRatio).HasMaxLength(40);
            modelBuilder.Entity<InvoiceLine>().HasIndex(x => x.SourcePurchaseLineId);
            modelBuilder.Entity<StockLot>().HasIndex(x => x.SourcePurchaseLineId);

            modelBuilder.Entity<InvoiceLine>()
                .HasOne(il => il.Invoice)
                .WithMany(i => i.InvoiceLines)
                .HasForeignKey(il => il.InvoiceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoicePayment>()
                .HasOne(payment => payment.Invoice)
                .WithMany(invoice => invoice.Payments)
                .HasForeignKey(payment => payment.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoicePayment>()
                .HasIndex(payment => payment.InvoiceId);

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

            // POS search indexes: keep name/supplier/barcode lookups and stock
            // summary aggregation efficient for large product catalogs.
            modelBuilder.Entity<Product>().HasIndex(product => product.ITEMCODE);
            modelBuilder.Entity<Product>().HasIndex(product => product.Name);
            modelBuilder.Entity<Product>().HasIndex(product => product.SupplierId);
            modelBuilder.Entity<ProductUnit>().HasIndex(productUnit => productUnit.AlternateBarcode);
            modelBuilder.Entity<ProductUnit>().HasIndex(productUnit => new { productUnit.ProductId, productUnit.AlternateBarcode });
            modelBuilder.Entity<StockLot>().HasIndex(stockLot => new
            {
                stockLot.ProductId,
                stockLot.Status,
                stockLot.RemainingQuantity
            });
            modelBuilder.Entity<StockLot>().HasIndex(stockLot => new
            {
                stockLot.ExpiryDate,
                stockLot.RemainingQuantity
            });

            modelBuilder.Entity<PosProductCatalogEntry>()
                .HasIndex(entry => new { entry.ProductId, entry.ProductUnitId })
                .IsUnique();
            modelBuilder.Entity<PosProductCatalogEntry>()
                .HasIndex(entry => entry.AlternateBarcode);
            modelBuilder.Entity<PosProductCatalogEntry>()
                .HasIndex(entry => entry.ProductName);
            modelBuilder.Entity<PosProductCatalogEntry>()
                .HasIndex(entry => entry.AvailableToSale);

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
