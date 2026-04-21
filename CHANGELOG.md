# Changelog

## 2026-04-11 - Phase 1 Accounting Schema Foundation

### Summary
- Added Phase 1 database/schema groundwork to prepare the system for future accounting integration without implementing posting logic yet.
- Preserved the current operational model where `Invoice` remains the commercial document, `StockTransaction` remains the inventory movement ledger, `FinancialTransaction` remains the money movement ledger, `Voucher` remains the manual money document, and `StockDocument` remains the inventory voucher.
- Added a migration-ready accounting foundation and extended selected operational tables with future accounting reference fields.

### Added
- New accounting enums:
  - `AccountingPostingStatus`
  - `AccountingSourceType`
  - `NormalBalanceType`
  - `FiscalYearStatus`
  - `AccountingPeriodStatus`
- New master/foundation entities:
  - `Branch`
  - `CostCenter`
  - `Currency`
  - `FiscalYear`
  - `AccountingPeriod`
  - `AccountOpeningBalance`
- Extended accounting entities:
  - `Account`
  - `JournalEntry`
  - `JournalEntryLine`

### Extended Operational Schema
- `Invoice`
  - added accounting-readiness fields such as `BranchId`, `WarehouseId`, `CurrencyId`, `ExchangeRate`, `DocumentDate`, `ReferenceNumber`, `PostingStatus`, `CreatedBy`, `UpdatedBy`
- `Voucher`
  - added `VoucherDate`, `BranchId`, `WarehouseId`, `CurrencyId`, `ExchangeRate`, `ReferenceNumber`, `PostingStatus`, `CreatedBy`, `UpdatedBy`
- `FinancialTransaction`
  - preserved legacy columns
  - added future accounting fields such as `Direction`, `SourceType`, `SourceId`, `ReferenceNumber`, `BranchId`, `WarehouseId`, `CurrencyId`, `ExchangeRate`, `PostingStatus`, `CreatedBy`, `UpdatedBy`
- `StockDocument`
  - added `DocumentDate`, `ReferenceNumber`, `WarehouseId`, `BranchId`, `PostingStatus`, `CreatedBy`, `UpdatedBy`
- `StockTransaction`
  - added `WarehouseId`, `StockLotId`, `StockAdjustmentId`, `StockDocumentId`, `BranchId`, `ReferenceNumber`, `SourceType`, `SourceId`, `CreatedBy`
- `Stock`
  - added `WarehouseId`, `LastMovementDate`
- `StockItem`
  - added `LineNumber`, `StockLotId`
- `CashierSession`
  - added `SessionNumber`, `BranchId`, `ExpectedClosingBalance`, `DifferenceAmount`, `CreatedBy`, `UpdatedBy`
- `Warehouse`
  - added `Code`, `BranchId`, `CreatedBy`, `UpdatedBy`

### EF Core Model
- Updated `ApplicationDbContext` to register and index the new accounting and support tables.
- Added relationship and index configuration for:
  - account hierarchy
  - fiscal year and accounting periods
  - opening balances
  - journal entry lines
  - stock and source tracking indexes

### Migration
- Generated EF Core migration:
  - `RaccoonWarehouse.Data/Migrations/20260411115659_Phase1AccountingFoundation.cs`
- Updated EF snapshot:
  - `RaccoonWarehouse.Data/Migrations/ApplicationDbContextModelSnapshot.cs`

### Verification
- Build passed:
  - `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.sln -v minimal`
- EF migration scaffolding passed using the data project as startup:
  - `dotnet ef migrations add Phase1AccountingFoundation --project RaccoonWarehouse.Data --startup-project RaccoonWarehouse.Data`

### Notes / Risks
- The generated migration is broader than a pure accounting-only migration because the existing EF snapshot was already behind the current domain model in several places.
- `FinancialTransaction` required explicit legacy column preservation to avoid incorrect rename behavior in migration scaffolding.
- The migration should be reviewed carefully before production apply, especially around existing operational table drift.
- Build still reports pre-existing project warnings, including:
  - `AutoMapper 14.0.0` vulnerability warning
  - existing nullable warnings
  - existing legacy migration naming warnings

### Recommended Next Step
- Review and, if needed, trim the generated migration into a stricter production-safe Phase 1 rollout before running database update on the live environment.

## 2026-04-11 - Phase 2 Accounting Posting Integration

### Summary
- Extended the existing accounting service into a practical posting layer instead of introducing a second accounting stack.
- Connected vouchers and stock documents to accounting so they can create linked journal entries and participate in reverse/repost flows.
- Added configurable account-code resolution through `AppSettings` while preserving the existing default chart-of-accounts codes as fallback behavior.

### Added
- New accounting posting methods:
  - `PostVoucherEntryAsync`
  - `PostStockDocumentEntryAsync`
- New configurable account setting keys inside `AccountingService` for:
  - cash
  - POS cash
  - bank
  - receivables
  - payables
  - inventory
  - tax
  - sales revenue
  - sales returns
  - discounts
  - COGS
  - stock gain/loss
  - internal consumption
  - general expense

### Updated Posting Flow
- `AccountingService`
  - now seeds default accounting account-code mappings into `AppSettings`
  - resolves posting accounts from settings first, then falls back to the legacy hardcoded account codes
  - now skips financial-transaction journal posting for `ReceiptVoucher` and `PaymentVoucher` source types to reduce double-posting risk when vouchers are the posting owner
- `VoucherService`
  - now posts accounting on create
  - reverses and reposts accounting on update when the voucher was already posted
  - updates voucher `PostingStatus` based on posting outcome
- `StockDocumentService`
  - now posts accounting on create
  - reverses and reposts accounting on update when the stock document was already posted
  - updates stock document `PostingStatus` based on posting outcome

### Posting Rules Implemented
- `Voucher`
  - `Receipt`: debit settlement account, credit customer receivable or fallback receivable account
  - `Payment`: debit supplier payable or expense fallback, credit settlement account
- `StockDocument`
  - `In`: debit inventory, credit supplier payable when supplier exists, otherwise stock gain fallback
  - `Out`: credit inventory, debit internal consumption fallback or stock loss fallback

### Verification
- Build passed:
  - `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.sln -v minimal`

### Notes / Risks
- Voucher posting is intentionally limited to `Receipt` and `Payment` types in this phase. Other voucher types still require a clearer business posting owner before enabling automatic journals.
- Stock document posting currently uses the stored item `PurchasePrice` as the accounting value source. If operational valuation later moves to lot-based costing only, posting should be aligned to that service.
- Existing invoice and financial transaction posting behavior was preserved; this patch only filled the missing voucher and stock-document integration points.

## 2026-04-11 - Arabic Demo Presentation Seed

### Summary
- Replaced the old English demo seed with a fuller Arabic demo dataset for live presentation and walkthrough use.
- Seed now covers:
  - Arabic users, customers, and suppliers
  - Arabic categories, subcategories, brands, units, and products
  - warehouse and cashier session demo setup
  - stock balances, stock documents, and stock transactions
  - sales invoices, POS sale, purchase invoice, vouchers, and financial transactions
  - chart of accounts, account-code settings, fiscal year, accounting period, opening balances, and sample journal entries when accounting tables exist

### File
- `RaccoonWarehouse.Tests/DemoData_Seed.sql`

### Notes
- The script is guarded so optional accounting tables are seeded only if they already exist in the target database.
- This is intended for demo/presentation environments, not production data refresh.
