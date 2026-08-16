# Stock And Inventory

Last verified: 2026-06-13

## Purpose

Inventory covers current balances, lots, expiry, prices, movement history,
documents, adjustments, external reconciliation, and stock reporting.
`Stock` is a derived current summary; lots and transactions preserve history.

## Entry Points

- `RaccoonWarehouse-master/Stocks/StockIn.*`
- `RaccoonWarehouse-master/Stocks/StockOut.*`
- `RaccoonWarehouse-master/Stocks/CurrentStock.*`
- `RaccoonWarehouse-master/Stocks/StockAdjustmentWindow.*`
- `RaccoonWarehouse-master/Stocks/Reports/`

## Main Workflows

- Stock In creates an inbound document, lot, positive transaction, and summary update.
- Stock Out creates an outbound document and consumes lots using FEFO.
- Adjustments increase, decrease, replace, or close/recreate a selected batch.
- Falcon import reconciles remote quantities against active unexpired local lots.
- Reports cover balances, movement, valuation, low stock, and reconciliation.

## Business Rules

- Quantity and unit-conversion snapshots must be non-zero.
- Inbound purchase and sale prices must be positive.
- Outbound movements cannot exceed active, unexpired stock.
- Allocation orders by earliest expiry and then creation date.
- Fully consumed lots are closed.
- Used batches cannot have historical metadata rewritten.
- Increase/decrease requires an active batch and reason.
- Current stock aggregates units using configured conversion factors.

## Services And Models

- `IStockService`: movement posting, FEFO, summaries, lots, and adjustments.
- `IStockDocumentService`: document persistence, search, and accounting.
- `IStockTransactionService`: validated transaction operations.
- `IStockReportService`: inventory reporting.
- `IFalconStockImportService`: temporary reconciliation integration.

Models include `Stock`, `StockLot`, `StockTransaction`, `StockDocument`,
`StockItem`, and `StockAdjustment`.

## Accounting

- Stock In: Inventory debit; payable or stock-gain credit.
- Stock Out: loss/internal-consumption debit; Inventory credit.
- Increase adjustment: Inventory / Stock Gain.
- Decrease adjustment: Stock Loss / Inventory.
- Replacement and zero-value documents do not post value journals.
- Updates may reverse and repost accounting.

## Verification

Relevant evidence:

- `RaccoonWarehouse.Tests/StockServiceStockOutRulesTests.cs`
- `RaccoonWarehouse.Tests/StockInOutUiQaReport.md`
- `RaccoonWarehouse.Tests/AccountingServiceReportTests.cs`
- `RaccoonWarehouse.Tests/QA_Testing_Summary.md`

The QA summary records passing Falcon-focused tests and known lot-allocation
failures. Verify current results before relying on the totals.

## Known Risks

- Document/accounting persistence and inventory movement are separate operations.
- Stock Out update can partially complete across document and inventory steps.
- Some UI paths may report success despite service failure.
- Warehouse identifiers are not consistently propagated through movement/summary logic.
- Outbound document lines may not retain direct lot traceability.
- Falcon endpoint authentication and production suitability require review.
- Some stock localization text appears encoding-damaged.

## Related Paths

- `RaccoonWarehouse.Application/Service/Stocks/`
- `RaccoonWarehouse.Application/Service/StockDocuments/`
- `RaccoonWarehouse.Application/Service/StockTransactions/`
- `RaccoonWarehouse.Domain/Stock/`
- `RaccoonWarehouse.Domain/StockLots/`
- `RaccoonWarehouse.Domain/StockTransactions/`
- `RaccoonWarehouse.Domain/StockDocuments/`
- `RaccoonWarehouse.Domain/StockItems/`
- `RaccoonWarehouse.Domain/StockAdjustments/`

