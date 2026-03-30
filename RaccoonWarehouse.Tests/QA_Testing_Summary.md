# QA Testing Summary

## Date
- 2026-03-09
- 2026-03-11
- 2026-03-24
- 2026-03-27

## Scope Tested and Evaluated
- `Category`
- `SubCategory`
- `Brand`
- `Product + ProductUnit` (related unit sync and tax behavior)
- `Voucher` (`CreateVoucher`, `PaymentVoucher`, `SearchVoucherWindow`)
- `Stock In/Out Vouchers` (`StockIn`, `StockOut`)
- `POS` (`Invoices/POS`)
- `Delegates` (entity, service, feature toggle bootstrap, invoice linkage surface)
- `Employees` (entity, service, feature toggle bootstrap, employee dialogs/report surface)
- `Unified Permissions` (permission definitions, role-permission mapping, redesigned manager UI, report permission compatibility)
- `Crash hardening scan` (global exception handling, null/empty result guards, stock document update safety)
- `Accounting reporting` (trial balance, general ledger, balance sheet)

## What Was Evaluated
- CRUD behavior for service-layer operations.
- UI validation behavior for required vs nullable fields.
- Exception safety (`try/catch`) in UI event handlers.
- Loading indicator usage around async UI operations.
- Correct async flow (`await`) for destructive operations (delete/update).
- Runtime crash exposure from:
  - missing global unhandled exception handlers
  - unsafe `.First()` usage on possibly empty collections
  - unsafe `result.Data` dereferences after service calls
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
- Total tests: `30`
- Passed: `30`
- Verification for the new employee module is currently blocked by SDK mismatch:
  - `global.json` requests `.NET 10.0.103`
  - installed locally: `.NET 8.0.418`
- Additional accounting report tests were added, and the test assembly builds inside the solution build.
- Direct `dotnet test` execution remains blocked intermittently in this repo because MSBuild exits with `Build FAILED` and `0 Error(s)` before running tests.
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

### Delegates
- Added service tests for:
  - delegate create
  - duplicate code validation
  - update status/type
  - analytics aggregation over linked invoices
- Implemented:
  - separate `Delegate` business entity linked to `User` by `UserId`
  - invoice linkage through nullable `DelegateId`
  - delegate feature toggle through `AppSettings` and centralized `DelegateFeatureService`
  - dialog-based delegate CRUD/details/settings windows
  - delegate selector in sales invoice when feature is enabled
  - delegate display in invoice search/profit browsing
  - startup schema bootstrap and SQL migration script fallback because `dotnet ef` CLI was unavailable locally
- References:
  - `DelegateServiceCrudTests.cs`
  - `RaccoonWarehouse.Data/Migrations/AddDelegateModule.sql`

### Employees
- Added service tests for:
  - employee create
  - duplicate code validation
  - update status/job title
  - analytics aggregation by status and branch
- Implemented:
  - separate `Employee` business entity linked to `User` by `UserId`
  - employee feature toggle through `AppSettings` and centralized `EmployeeFeatureService`
  - dialog-based employee CRUD/details/settings windows
  - dashboard visibility and navigation only when feature is enabled
  - startup schema bootstrap and SQL migration script fallback
- References:
  - `EmployeeServiceCrudTests.cs`
  - `RaccoonWarehouse.Data/Migrations/AddEmployeeModule.sql`

### Unified Permissions
- Implemented:
  - unified `PermissionDefinition` + `RolePermission` model
  - centralized `PermissionService` for matrix loading, checks, saving, and legacy report permission migration
  - redesigned permissions manager UI over roles, modules, resources, and actions
  - compatibility layer so existing report permission checks keep working through the new model
  - user-management actions now use permission checks instead of raw role checks
- Added automated tests for:
  - permission definition seeding
  - default allow behavior
  - explicit deny override persistence
  - matrix row generation
- References:
  - `PermissionServiceTests.cs`
  - `RaccoonWarehouse.Data/Migrations/AddUnifiedPermissions.sql`

### Crash Hardening Scan
- Implemented:
  - added global exception capture in `App.xaml.cs` for:
    - `DispatcherUnhandledException`
    - `AppDomain.CurrentDomain.UnhandledException`
    - `TaskScheduler.UnobservedTaskException`
  - added local crash logging to `%LocalAppData%\RaccoonWarehouse\crash.log`
  - fixed stock document update crash risk caused by `_originalItems.First()` in:
    - `Stocks/StockIn.xaml.cs`
    - `Stocks/StockOut.xaml.cs`
  - hardened update dialogs against null service payloads in:
    - `Units/UpdateUnit.xaml.cs`
    - `Products/UpdateProduct.xaml.cs`
    - `Categories/UpdateCategory.xaml.cs`
- Verification:
  - `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.sln --no-restore -v minimal`
  - environment still reports `Build FAILED` with `0 Error(s)` and existing warnings only
- Key findings:
  - app-level crash logging was missing before this pass
  - several additional risky `First()` / direct `result.Data` access patterns still exist, especially in `POS`, `StockIn`, `StockOut`, and some update/load dialogs
- more defensive hardening is still recommended for intermittent customer-only crashes

### Accounting Reporting
- Implemented:
  - service-layer accounting reports driven from posted journal entries:
    - `Trial Balance`
    - `General Ledger`
    - `Balance Sheet`
  - richer default account seeding/backfill for common accounting usage
  - new accounting report windows wired into the dashboard accounting section
- Added automated tests for:
  - balanced trial balance totals
  - opening/running balance behavior in general ledger
  - balance sheet grouping by assets/liabilities/equity
- Verification:
  - `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.sln -v minimal /p:UseAppHost=false /p:BaseOutputPath=C:\Users\obadaqafisheh\tmp_roccopos_build\`
  - build passed with warnings only
  - `dotnet test RaccoonWarehouse.Tests/RaccoonWarehouse.Tests.csproj` remains blocked by the repo's existing MSBuild anomaly (`Build FAILED` with `0 Error(s)`)
- References:
  - `AccountingServiceReportTests.cs`
  - `RaccoonWarehouse-master/Accounting/TrialBalanceReport.xaml`
  - `RaccoonWarehouse-master/Accounting/GeneralLedgerReport.xaml`
  - `RaccoonWarehouse-master/Accounting/BalanceSheetReport.xaml`

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
- `RaccoonWarehouse.Tests/PosKeyboardFlowQaReport.md`
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
- Accounting control slice update (2026-03-27):
  - Added service coverage for:
    - posting lock date enforcement
    - journal reversal flow
  - Added accountant UI coverage targets for:
    - accounting settings lock-date workflow
    - journal browser loading and reversal action
  - Verification:
    - `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.sln -v minimal /p:UseAppHost=false /p:BaseOutputPath=C:\Users\obadaqafisheh\tmp_roccopos_build\`
    - build passed with warnings only
    - `dotnet test RaccoonWarehouse.Tests/RaccoonWarehouse.Tests.csproj -v minimal /p:UseAppHost=false /p:BaseOutputPath=C:\Users\obadaqafisheh\tmp_roccopos_test_build\` still exits early during restore/build in this environment and did not execute tests
- Current pass/fail snapshot for this slice:
  - Pass: solution build
  - Fail: none in build
  - Blocked: automated test execution in CLI environment
- Remaining manual checks for this slice:
  - verify the lock date blocks manual journal posting and auto-posting from invoices/POS on the live DB
  - verify reversing a posted journal updates the original row to `معكوس` and creates a new reversal row
  - verify accounting settings window saves both enable flag and lock date correctly
- Additional crash-prone paths still need hardening review in:
  - `Invoices/POS.xaml.cs`
  - `Stocks/StockIn.xaml.cs`
  - `Stocks/StockOut.xaml.cs`
  - edit dialogs that still assume non-null service payloads
- Manual verification is needed for:
  - accounting report UI rendering and empty-data behavior in the running app
  - crash log creation after a forced exception
  - app behavior after unhandled UI-thread exceptions
  - customer environments with different SQL/data states
- Manual WPF verification is still needed for:
  - delegate dialogs
  - feature toggle visibility behavior in dashboard and invoice screen
  - employee dialogs
  - employee feature toggle visibility behavior in dashboard
  - unified permissions manager screen layout and save workflow
  - dashboard/settings/user flows under denied permissions
  - live SQL Server schema upgrade path on a real customer database
- Full UI automation execution (click-path runtime tests) is still pending.
- Manual end-to-end verification in running app for Product UI flows is recommended next.
- Manual end-to-end verification for voucher financial posting/void on real DB is recommended next.
- Manual end-to-end verification for stock-out with concurrent updates is recommended next.
