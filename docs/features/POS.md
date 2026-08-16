# POS

Last verified: 2026-06-13

## Purpose

The WPF POS supports cashier sales, returns, exchanges, held invoices, payments,
stock movements, accounting, receipt/payment entry, PDF output, and daily
cashier reporting.

## Entry Points

- `RaccoonWarehouse-master/Invoices/POS.xaml`
- `RaccoonWarehouse-master/Invoices/POS.xaml.cs`
- Child windows in `RaccoonWarehouse-master/POS/`
- Dashboard POS action in `RaccoonWarehouse-master/Home/Dashboard.xaml.cs`

The screen supports function keys, `Esc`, `Delete`, and `Ctrl+1` through
`Ctrl+7` shortcuts.

## Main Workflows

1. Load requires an active cashier session and loads customers and stocked products.
2. Products are added by barcode, search, category cards, unit selection, or creation.
3. Sale validation checks lines, stock, cost floor, payment, and customer credit rules.
4. Completion saves the invoice, posts stock, and creates a financial transaction
   for non-credit payment.
5. Held invoices can be resumed; returns and exchanges reference an original invoice.
6. Daily reporting combines invoices, vouchers, financial transactions, and sessions.

## Business Rules

- Lines require valid product/unit identifiers and non-zero quantity.
- Positive quantity cannot exceed available, active, unexpired stock.
- Selling below recorded unit cost is blocked.
- Outbound stock uses FEFO allocation.
- Returns cannot exceed the matching original product/unit quantity.
- Credit sales require a customer and enforce credit status/limit.
- Check payment requires captured check details.
- Completed invoices store POS, cashier, session, status, and close-time metadata.
- Discount action is currently disabled.

## Dependencies

Services include `IInvoiceService`, `IStockService`, `IProductService`,
`IProductUnitService`, `IUserService`, `ICashierSessionService`,
`IFinancialTransactionService`, and loading/report helpers.

Principal models are `Invoice`, `InvoiceLine`, `CashierSession`, stock lots and
transactions, financial transactions, checks, users, products, and product units.

## Permissions And Sessions

An in-memory active cashier session is required for primary POS operations.
No explicit POS-specific permission gate was verified on the dashboard action
or POS window. Broader indirect restrictions are **Needs verification**.

## Localization And Errors

The main window uses the shared `UiText` flow, but some hardcoded or
inconsistently encoded messages remain. High-risk actions generally use
`try/catch` and loading cleanup.

## Verification

Relevant evidence:

- `RaccoonWarehouse.Tests/PosUiQaReport.md`
- `RaccoonWarehouse.Tests/PosActionButtonsQaReport.md`
- `RaccoonWarehouse.Tests/PosDesktopParityChecklist.md`
- `RaccoonWarehouse.Tests/PosKeyboardFlowQaReport.md`

Keyboard scenarios remain pending manual execution, and there is no dedicated
automated full POS workflow suite.

## Known Risks

- Invoice, accounting, stock, and financial posting are not one atomic operation.
- Invoice and linked financial-transaction posting may overlap accounting effects.
- Retry after partial failure may duplicate stock movement.
- Resumed held invoices do not restore all original metadata.
- POS access lacks a verified explicit permission gate.
- Daily reporting may show both a source invoice and its linked transaction.

## Related Paths

- `RaccoonWarehouse.Application/Service/Invoices/InvoiceService.cs`
- `RaccoonWarehouse.Application/Service/Stocks/StockService.cs`
- `RaccoonWarehouse.Application/Service/FinancialTransactions/FinancialTransactionService.cs`
- `RaccoonWarehouse.Application/Service/Accounting/AccountingService.cs`
- `RaccoonWarehouse.Application/Service/Cashers/CashierSessionService.cs`
- `RaccoonWarehouse.Domain/Invoices/`
- `RaccoonWarehouse.Domain/InvoiceLines/`
- `RaccoonWarehouse.Domain/POS/`

