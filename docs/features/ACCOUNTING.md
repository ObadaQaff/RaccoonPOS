# Accounting

Last verified: 2026-06-13

## Purpose

Accounting provides double-entry journals over invoices, vouchers, financial
transactions, stock documents, and stock adjustments. It also includes accounts,
fiscal periods, currencies, tax, recurring journals, reconciliation, and reports.

## Entry Points

- `RaccoonWarehouse-master/Accounting/`
- Accounting dashboard module and action handlers
- Accounting settings window
- User/customer account statements

Startup seeds selected accounting reference data and processes due recurring journals.

## Main Workflows

- Create balanced manual journals.
- Browse/filter journals and inspect lines.
- Reverse posted journals with a reason.
- Maintain the account tree and posting accounts.
- Post supported operational documents automatically.
- Produce trial balance, ledger, balance sheet, statements, P&L, and cash-flow data.

The accounting feature flag controls navigation visibility; it does not disable
automatic operational posting.

## Invariants

- Journals require at least two non-zero lines.
- Total debit must equal total credit.
- A line cannot contain both debit and credit.
- Accounts must exist, be active, and permit posting.
- Dates on or before the posting lock date are rejected.
- Reversal creates a new journal with debit/credit swapped.
- Application-level duplicate prevention uses source reference type and ID.

## Operational Posting

- Sales post settlement, discount, revenue, tax, COGS, and inventory effects.
- Returns reverse the corresponding sale effects.
- Purchases post inventory/input-tax against settlement.
- Voucher posting currently supports receipt and payment.
- Stock documents use quantity multiplied by purchase price.
- Invoice/voucher-owned financial transactions are intended to skip duplicate posting.
- Stock replacement adjustments do not create value journals.

## Configuration

Database `AppSettings` include:

- `EnableAccountingSystem`
- `AccountingPostingLockDate`
- `Accounting.AccountCode.*` mappings

Default account mappings are seeded with fallback codes.

## Services And Models

Core services:

- `AccountingService`
- `AccountService`
- `AccountTreeService`

Supporting services cover fiscal years, opening balances, currency, tax, cost
centers, bank reconciliation, recurring journals, aging, statements, P&L, and cash flow.

Principal models include `Account`, `JournalEntry`, `JournalEntryLine`,
`FiscalYear`, `AccountingPeriod`, `AccountOpeningBalance`, `TaxRate`,
`ExchangeRate`, recurring journals, and bank reconciliation records.

## Verification

A focused run reported on 2026-06-13:

```powershell
dotnet test RaccoonWarehouse.Tests\RaccoonWarehouse.Tests.csproj --filter "FullyQualifiedName~AccountingServiceReportTests|FullyQualifiedName~EndpointOrderStatusServiceTests" --no-restore
```

Result: 19 total, 16 passed, 3 failed. Failures involved obsolete account-code
expectations, changed invoice line count, and a missing cashier-session setup.

## Known Risks

- Disabling accounting navigation does not disable posting.
- Duplicate protection is concurrency-sensitive without verified database uniqueness.
- Tax/currency lines may be added after initial balance validation.
- Reversal lookup may select the wrong journal after repeated reverse/repost cycles.
- Central posting may omit fiscal-year/period IDs used by some reports.
- Some reports still classify legacy account-code shapes.
- Dedicated voucher and stock-document posting/repost tests are limited.

## Related Paths

- `RaccoonWarehouse.Application/Service/Accounting/`
- `RaccoonWarehouse.Application/Service/Invoices/InvoiceService.cs`
- `RaccoonWarehouse.Application/Service/Vouchers/VoucherService.cs`
- `RaccoonWarehouse.Application/Service/StockDocuments/StockDocumentService.cs`
- `RaccoonWarehouse.Application/Service/FinancialTransactions/FinancialTransactionService.cs`
- `RaccoonWarehouse.Domain/Accounting/`
- `RaccoonWarehouse.Domain/Reports/Accounting/`
- `RaccoonWarehouse.Data/Configurations/AccountConfiguration.cs`
- `RaccoonWarehouse.Data/Migrations/20260411115659_Phase1AccountingFoundation.cs`
- `RaccoonWarehouse.Tests/AccountingServiceReportTests.cs`

