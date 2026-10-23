# Pharmacy Purchase Bonus implementation — 2026-10-23

## Result
Purchase invoice rows accept `عرض البونص` such as `3+1`, with Apply; existing `مجاني` remains the only free-quantity column. An active ratio is automatically recalculated when paid quantity changes. Changed ratios with mismatching quantities block save until applied. Manual FreeQuantity editing clears the offer (explained on screen). Ratios persist and reopen with invoices.

A new purchase line is received into one independent StockLot, containing paid and free units at the line's net effective cost. Ratios and manual free quantities both qualify. Mixed invoices consistently allocate header discounts across all their lines. Ordinary invoices without bonus/free quantities retain the legacy costing path. No old rows are backfilled or revalued.

## Stored data
- `InvoiceLine.BonusRatio`: nullable nvarchar(40).
- `InvoiceLine.PurchaseAcquisitionCost`: nullable decimal(18,6), exact net acquisition snapshot.
- `InvoiceLine.SourcePurchaseLineId`: nullable int, original line for explicit purchase returns.
- `StockLot.PurchaseAcquisitionCost`: nullable decimal(18,6), total original acquisition cost.
- `StockLot.SourcePurchaseLineId`: nullable int, receipt lineage for strict source-lot reversals.
- Index on SourcePurchaseLineId on each table. No Batch Number or duplicated FreeQuantity/received-quantity fields were added.
- Existing `StockLot.Quantity`/`BaseQuantity` contain total received; `StockLot.PurchasePrice` contains effective unit cost at six decimals. Exact totals are retained to avoid repeating-decimal valuation drift. InvoiceLine.UnitCost remains the purchase-price snapshot; sale-line UnitCost comes from the selected lot.

## Calculations
| Paid | Offer / free | Price | Received | Acquisition | Effective cost |
|---:|---|---:|---:|---:|---:|
|12|3+1 → 4|2.000|16|24.000|1.500000|
|10|manual 5|2.000|15|20.000|1.333333|
|10|manual 10|3.000|20|30.000|1.500000|
|12|3+1 → 4; line discount 2.320, invoice discount 2.000|2.320 incl. 16% VAT|16|20.000|1.250000|

The first and later receipts remain separate lots. Selling 3 units at effective cost 1.50 yields COGS 4.50. No new product-level weighted average or sale-price update is introduced; legacy configured costing still applies to legacy lots.

## Returns and accounting
Select a bonus purchase in the POS return-invoice lookup. After the existing return password check, enter paid and free return quantities separately. The return service checks permissions, prior paid/free returns, original supplier and line, and deducts only the source lot. SQL Server uses serializable isolation for the composed return.

For a 12-paid + 4-free receipt costing 24:
- Return 2 paid: supplier refund 4, inventory reversal 3; the difference credits the existing COGS account.
- Return 2 free: supplier refund 0, inventory reversal 3; the difference debits COGS. No fake financial receipt is queued.
- Return all 12 paid + 4 free: supplier refund 24, inventory reversal 24.
Input VAT is reversed only with paid units using original tax snapshots. Cumulative rounding ensures partial returns together refund exactly the original cost/tax. The original unit conversion factor is preserved. Supplier credit is offered only when the original purchase was credit; otherwise cash/card/transfer methods are available. Posted bonus returns cannot be edited; consumed/returned bonus purchases require returns instead of rewriting costs.

The invoice module currently treats all purchase VAT as recoverable input VAT and has no non-recoverable-tax or landed-charge inputs. This feature follows that existing policy; it does not add a new tax/landed-cost subsystem.

## Verification
- 46 feature cases passed, 0 failed.
- Full regression: 215 passed, 8 failed, 223 total.
- Unchanged baseline: 169 passed, the same 8 failures, 177 total. No new failures.
- WPF C# checked in memory with existing source generators and XAML-name declarations: 347 source trees, zero errors. XAML/JSON valid; one FreeQuantity column.
- No ERP executable rebuild, application launch, or production migration/data update.

Existing baseline failures:
- `RaccoonWarehouse.Tests.AccountingServiceReportTests.FinancialTransactionVoidAsync_ShouldReverseAccountingEntry`
- `RaccoonWarehouse.Tests.AccountingServiceReportTests.InvoiceUpdateAsync_ShouldReverseOldJournalAndPostFreshJournal`
- `RaccoonWarehouse.Tests.PurchaseExpiryValidationTests.PurchaseInvoiceChecks_ShouldPersistAndReplaceOnUpdate`
- `RaccoonWarehouse.Tests.PurchaseExpiryValidationTests.PurchaseInvoiceWithoutExpiry_ShouldFailBeforePersistence`
- `RaccoonWarehouse.Tests.PurchaseExpiryValidationTests.PurchaseInvoiceWithoutSupplier_ShouldFailBeforePersistence`
- `RaccoonWarehouse.Tests.SalesReportServiceTests.GetSalesReportAsync_ShouldDisplayEndpointOrdersButOnlyTotalCompletedOnes`
- `RaccoonWarehouse.Tests.VoucherServiceCrudTests.Create_Voucher_ShouldAllowNullableFields`
- `RaccoonWarehouse.Tests.VoucherServiceCrudTests.Create_Voucher_WithChecks_ShouldPersistVoucherAndChecks`

## Manual checks still required
Apply `RaccoonWarehouse.Data/Migrations/AddPharmacyPurchaseBonus.sql` manually to a test copy before starting updated code. The SQL was not executed here. Rebuild/start using the normal Windows workflow; verify RTL layout, ratio Apply/manual override, reopen/edit, multiple receipts, sale COGS, and paid/free cash/card/supplier-credit returns. SQL Server concurrency/rollback and rendered UI have not been exercised in this macOS session. Existing unrelated EF snapshot/model drift was detected and left outside this feature; the supplied migration contains only the five nullable columns and two indexes, with a data-preserving rollback guard.

## Modified/created files
- `RaccoonWarehouse-master/App.xaml.cs`
- `RaccoonWarehouse-master/Invoices/POS.ProductHelpers.cs`
- `RaccoonWarehouse-master/Invoices/POS.xaml.cs`
- `RaccoonWarehouse-master/Invoices/PayInvoice.xaml`
- `RaccoonWarehouse-master/Invoices/PayInvoice.xaml.cs`
- `RaccoonWarehouse-master/Invoices/PurchaseBonusReturnWindow.cs`
- `RaccoonWarehouse-master/Localization/LiteralStrings.en.json`
- `RaccoonWarehouse.Application/Service/Accounting/AccountingService.cs`
- `RaccoonWarehouse.Application/Service/Invoices/InvoiceService.cs`
- `RaccoonWarehouse.Application/Service/Purchases/PurchaseBonusCosting.cs`
- `RaccoonWarehouse.Application/Service/Purchases/PurchaseBonusReturnService.cs`
- `RaccoonWarehouse.Application/Service/Purchases/PurchaseBonusReturns.cs`
- `RaccoonWarehouse.Application/Service/Sales/SaleCheckoutService.cs`
- `RaccoonWarehouse.Application/Service/Stocks/StockReportService.cs`
- `RaccoonWarehouse.Application/Service/Stocks/StockService.cs`
- `RaccoonWarehouse.Data/ApplicationDbContext.cs`
- `RaccoonWarehouse.Data/Migrations/20261023010000_AddPharmacyPurchaseBonus.Designer.cs`
- `RaccoonWarehouse.Data/Migrations/20261023010000_AddPharmacyPurchaseBonus.cs`
- `RaccoonWarehouse.Data/Migrations/AddPharmacyPurchaseBonus.sql`
- `RaccoonWarehouse.Data/Migrations/ApplicationDbContextModelSnapshot.cs`
- `RaccoonWarehouse.Domain/InvoiceLines/DTOs/InvoiceLineReadDto.cs`
- `RaccoonWarehouse.Domain/InvoiceLines/DTOs/InvoiceLineWriteDto.cs`
- `RaccoonWarehouse.Domain/InvoiceLines/InvoiceLine.cs`
- `RaccoonWarehouse.Domain/InvoiceLines/PurchaseBonusRatio.cs`
- `RaccoonWarehouse.Domain/StockLots/StockLot.cs`
- `RaccoonWarehouse.Tests/PharmacyPurchaseBonusReport.md`
- `RaccoonWarehouse.Tests/PharmacyPurchaseBonusTests.cs`
- `RaccoonWarehouse.Tests/QA_Testing_Summary.md`
