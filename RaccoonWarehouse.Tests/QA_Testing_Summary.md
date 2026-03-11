# QA Testing Summary

## Date
- 2026-03-09

## Scope Tested and Evaluated
- `Category`
- `SubCategory`
- `Brand`
- `Product + ProductUnit` (related unit sync and tax behavior)
- `Voucher` (`CreateVoucher`, `PaymentVoucher`, `SearchVoucherWindow`)
- `Stock In/Out Vouchers` (`StockIn`, `StockOut`)
- `POS` (`Invoices/POS`)

## What Was Evaluated
- CRUD behavior for service-layer operations.
- UI validation behavior for required vs nullable fields.
- Exception safety (`try/catch`) in UI event handlers.
- Loading indicator usage around async UI operations.
- Correct async flow (`await`) for destructive operations (delete/update).
- Product-unit relation integrity during update:
  - add/update/remove unit rows
  - duplicate unit validation
  - tax recalculation

## Rule Applied
- Nullable-field UI rule is now documented and followed:
  - If a field is nullable in entity/DTO, UI allows null/empty input.
  - Only non-nullable fields are required in UI validation.

## Automated Test Status
- Test project: `RaccoonWarehouse.Tests`
- Total tests: `26`
- Passed: `26`
- Failed: `0`
- Skipped: `0`

## Module-by-Module Notes

### Category
- Added CRUD tests and invalid-input coverage.
- Fixed UI:
  - added `try/catch`
  - added loading service
  - fixed delete to await and handle result
  - added create-name validation
- References:
  - `CategoryServiceCrudTests.cs`
  - `CategoryUiQaReport.md`

### SubCategory
- Added CRUD tests and not-found delete case.
- Fixed UI:
  - added `try/catch`
  - added loading service
  - fixed delete to await and handle result
  - enforced required fields only (`Name`, `ParentCategoryId`)
  - allowed nullable fields (`Description`, `ImageUrl`)
- References:
  - `SubCategoryServiceCrudTests.cs`
  - `SubCategoryUiQaReport.md`

### Brand
- Added CRUD tests and not-found delete case.
- Fixed UI:
  - added `try/catch`
  - added loading service
  - fixed delete to await and handle result
  - enforced required `Name`
  - allowed nullable `ImageUrl`
- References:
  - `BrandServiceCrudTests.cs`
  - `BrandUiQaReport.md`

### Product + ProductUnit
- Added relationship-focused tests:
  - update product + unit sync add/update/remove
  - duplicate unit validation
  - product-not-found failure
  - tax apply recalculation
- Reference:
  - `ProductWithUnitsCrudTests.cs`
  - `ProductWithUnitsUiQaReport.md`

### Voucher
- Added service tests for:
  - create voucher with checks
  - nullable-field create handling
  - search filtering (voucher number, type, payment type, date range)
- Fixed UI:
  - added loading service to load/save/search
  - added `try/catch` around risky flows
  - fixed check-entry crash risk by replacing unsafe parse
  - added payment-by-check validation:
    - at least one check
    - positive check amounts
    - duplicate check number prevention
    - checks total must equal voucher amount
  - added safer null checks for selected voucher and check deletion
- References:
  - `VoucherServiceCrudTests.cs`
  - `VoucherUiQaReport.md`

### Stock In/Out Vouchers
- Added stock service rule tests for stock-out behavior:
  - fail when stock-out is posted for non-existing stock
  - fail when stock-out qty is greater than available
  - pass and decrease stock when qty is within available
- Fixed StockOut UI:
  - product list now filters to `Quantity > 0` only
  - deduplicated product list from stock rows
  - unit list filtered to units with positive stock
  - add-item validation blocks qty greater than available
  - add-item validation blocks zero available unit
- References:
  - `StockServiceStockOutRulesTests.cs`
  - `StockInOutUiQaReport.md`

### POS
- QA-evaluated critical POS paths:
  - barcode/search add-product
  - hold/resume flow
  - payment save + stock/financial posting
  - cancel flow
  - all POS action buttons (`Click` handlers)
- Fixed UI:
  - barcode null-safety for empty search results
  - hold now updates existing held invoice instead of creating duplicates
  - loading indicator around hold/payment/cancel async actions
  - line validation before save/payment for invalid product/unit/qty
  - null-safe stock availability validation
  - deduplicated loaded product list by `ProductId`
  - aligned payment methods with desktop sales invoice:
    - added `Debit`, `Check`, `MobilePayment` payment actions in POS
    - mapped these methods into financial posting
  - added try/catch coverage for high-risk action buttons
  - added loading and null-safe checks in return-item action
- References:
  - `PosUiQaReport.md`
  - `PosDesktopParityChecklist.md`
  - `PosActionButtonsQaReport.md`

## Files Created for QA Documentation
- `RaccoonWarehouse.Tests/CategoryUiQaReport.md`
- `RaccoonWarehouse.Tests/SubCategoryUiQaReport.md`
- `RaccoonWarehouse.Tests/BrandUiQaReport.md`
- `RaccoonWarehouse.Tests/ProductWithUnitsUiQaReport.md`
- `RaccoonWarehouse.Tests/VoucherUiQaReport.md`
- `RaccoonWarehouse.Tests/StockInOutUiQaReport.md`
- `RaccoonWarehouse.Tests/PosUiQaReport.md`
- `RaccoonWarehouse.Tests/PosDesktopParityChecklist.md`
- `RaccoonWarehouse.Tests/PosActionButtonsQaReport.md`
- `RaccoonWarehouse.Tests/DemoData_Seed.sql`
- `RaccoonWarehouse.Tests/UI_TestCases_DemoData.md`
- `RaccoonWarehouse.Tests/QA_Testing_Summary.md`

## Demo Data Seed (2026-03-09)
- Seed script: `RaccoonWarehouse.Tests/DemoData_Seed.sql`
- UI cases: `RaccoonWarehouse.Tests/UI_TestCases_DemoData.md`
- Seeded counts:
  - Categories: `2`
  - SubCategories: `3`
  - Brands: `3`
  - Units: `3`
  - Products: `3`
  - ProductUnits: `5`
  - Stocks: `5`
  - Invoices: `3`
  - InvoiceLines: `6`
  - Vouchers: `2`
  - Checks: `1`
  - FinancialTransactions: `4`
  - Users: `4`
  - CashierSessions: `1`
  - StockDocuments: `1`
  - StockItems: `3`
  - StockTransactions: `3`

## Remaining Coverage Gaps
- Full UI automation execution (click-path runtime tests) is still pending.
- Manual end-to-end verification in running app for Product UI flows is recommended next.
- Manual end-to-end verification for voucher financial posting/void on real DB is recommended next.
- Manual end-to-end verification for stock-out with concurrent updates is recommended next.
