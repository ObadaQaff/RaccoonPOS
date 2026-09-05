# QA Testing Summary

## 2026-09-01 Invoice Payment Methods Report
- Scope:
  - date-filtered finalized sales, returns, and endpoint-order invoices
  - separate payment-method columns for cash, Visa, Master, debit, check, mobile, and credit
  - mixed-payment allocation display and invoice reference navigation
- Implementation:
  - added a dedicated financial report window without changing the voucher-only payments/receipts report
  - reused the existing sales-report payment allocation data, excluding purchases and stock documents
- Verification:
  - app compiled to a separate output directory with 0 errors and existing warnings
- Remaining manual checks:
  - compare a mixed-payment invoice row with its saved payment allocations
  - verify date boundaries include the complete selected end date
  - double-click a row and confirm the source invoice opens

## 2026-09-01 Mixed-Payment Accounting Operation Idempotency
- Scope:
  - mixed cash/Visa/credit POS invoice financial-operation enqueueing
  - unique accounting-operation reference behavior
  - compatibility with existing `PostFinancialTransaction` operations
- Implementation:
  - financial operation keys now include the payment method, so each allocation for one invoice has its own idempotency key
  - the accounting-operation processor handles both legacy and method-specific operation types
  - no database migration is required
- Verification:
  - full solution build: passed with 0 errors and existing warnings
  - filtered accounting test run: 12 passed, 5 failed
- QA findings:
  - the reported duplicate-key scenario is addressed by separating operation keys per payment method
  - the five failures are existing accounting-report/posting test failures and require separate investigation; they did not prevent compilation
- Remaining manual checks:
  - create a mixed cash + Visa invoice and confirm both payment allocations save without a duplicate-key error
  - process the pending accounting operations and confirm one financial journal is posted for each payment method
  - retry the same operation and confirm it does not create a duplicate journal

## 2026-08-04 Purchase Invoice Accounting and Issued Checks
- Scope:
  - purchase invoice accounting posting and mandatory supplier linkage
  - credit purchases flowing to supplier payables
  - purchase invoice check validation, persistence, and replacement on update
  - separate accounting treatment for received checks and issued supplier checks
  - issued-check clearing, cancellation, and bounce handling
- Implementation:
  - added `Issued Checks Payable / شيكات صادرة مستحقة` as a standard current-liability account
  - purchase invoices and supplier payment vouchers credit issued-check liability instead of Checks in Hand
  - clearing an issued check debits issued-check liability and credits bank
  - cancelling or bouncing a pending issued check debits issued-check liability and restores supplier Accounts Payable
  - incoming customer-check behavior remains unchanged
- Verification:
  - focused accounting, supplier-report, check-status, supplier validation, and check persistence tests: 8 passed, 0 failed, 0 skipped
  - isolated full WPF solution build: passed with 0 errors and existing warnings
- QA finding resolved:
  - the first update-path test exposed AutoMapper replacing the loaded check collection before old rows were captured
  - update ordering was corrected; the regression now proves the old check is removed and the replacement is persisted without duplication
- Remaining manual checks:
  - create and update a purchase invoice with one and multiple checks, then reopen it and inspect the Checks table
  - clear, cancel, and bounce representative issued checks against live SQL Server and inspect their journals
  - apply pending migrations and confirm a new credit purchase appears in Supplier Payables

## 2026-08-04 Credit Purchase Supplier-Link Repair
- Root cause:
  - the Purchase Invoice screen stored the selected supplier in `CustomerId`, leaving `SupplierId` empty
  - Accounts Payable journals and the Supplier Payables report therefore could not associate the credit purchase with its supplier
- Implementation:
  - new and updated purchase invoices now persist the selected party through `SupplierId`
  - a scoped data-repair migration moves the incorrect party link only for credit purchase invoices with an empty `SupplierId`
  - existing Customer Debts, Supplier Payables, Receipt Voucher, and Payment Voucher workflows remain the single credit-management path
- Verification:
  - focused credit-purchase posting, supplier payable, and supplier-payment tests: 3 passed, 0 failed, 0 skipped
  - EF migration discovery passed and lists `20260804010000_RepairCreditPurchaseSupplierLinks` as pending
  - isolated full WPF solution build: passed with 0 errors and existing warnings
- Remaining manual checks:
  - apply the migration against a backed-up live database and confirm an existing credit purchase appears under its supplier
  - create a new credit purchase and settle it partially and fully from Supplier Payables

## 2026-08-04 Supplier Credit Settlement by Payment Voucher
- Scope tested:
  - Supplier Payables report selection and bilingual `Pay supplier / دفع للمورد` action
  - payment voucher supplier and outstanding-balance prefill
  - partial payments, positive-amount validation, overpayment rejection, and invalid credit-method rejection
  - supplier linkage through `SupplierId` for Accounts Payable posting
  - automatic payment-voucher PDF export after a successful supplier settlement
- UI/UX findings:
  - the existing report action changes meaning by stable customer/supplier role, not translated display text
  - it remains disabled until a party with a positive outstanding balance is selected
  - supplier-payment mode locks the supplier, removes the credit payment method, and refreshes the report after closing
- Verification:
  - focused supplier/customer voucher and party-balance tests: 7 passed, 0 failed, 0 skipped
  - isolated full WPF solution build: passed with 0 errors and existing warnings
  - broader accounting-report selection exposed 2 pre-existing unrelated failures in balance-sheet earnings and financial-transaction reversal tests; 17 other tests passed
- Remaining manual checks:
  - pay a supplier using cash, card, and check against live SQL Server and confirm the payable balance decreases
  - cancel and complete the PDF save dialog and confirm the saved payment remains valid
  - visually verify Arabic/English text fit at supported window sizes

## 2026-08-04 Check Status Loading and Editable User Search
- Scope tested:
  - loading begins only after status-change confirmation
  - loading covers journal posting, status persistence, transaction commit, and grid reload
  - guarded `finally` hides loading on success, validation failure, posting failure, persistence failure, and exceptions
  - editable user ComboBox filters linked party names case-insensitively while typing
  - exact dropdown selection, clearing text, and customer/supplier type changes remain supported
- Verification:
  - focused check/accounting tests: 8 passed, 0 failed, 0 skipped
  - final isolated WPF solution build: passed with 0 errors and existing warnings
- Remaining manual checks:
  - visually confirm the loading overlay during a live SQL status transition
  - type partial Arabic and English customer/supplier names and verify dropdown plus grid behavior

## 2026-08-04 Check Status Persistence and Party Filters
- Scope tested:
  - dedicated check-status persistence for valid and missing check IDs
  - unique journal references per deposited, bounced, and cancelled transition
  - transaction boundary joining journal posting and status persistence on relational databases
  - customer/supplier tagging when bounced or cancelled checks restore party balances
  - linked party name/type columns, search, party-type filter, and individual-user filter
- UI/UX findings:
  - filters use stable internal keys rather than translated display text
  - party names fall back to the user ID when an old linked record cannot resolve a current name
  - unlinked checks remain visible and can be filtered explicitly
- Verification:
  - focused check/accounting tests: 8 passed, 0 failed, 0 skipped
  - final isolated WPF solution build: passed with 0 errors and existing warnings
- Remaining manual checks:
  - change each allowed status against live SQL Server and confirm both the journal and table status change together
  - verify customer, supplier, individual-user, Arabic, and English filters with production-sized data

## 2026-08-04 Check Receipt Loading and Save-Order Fix
- Bug:
  - check validation ran after the voucher was persisted and after the loading overlay was shown
  - a validation failure returned early, leaving loading visible and an invalid voucher already saved
- Fix verified by inspection:
  - check rows, required values, duplicate numbers, positive amounts, and total amount are validated before loading and persistence
  - invalid check payments now remain editable without creating a voucher
  - the existing `finally` cleanup remains the safety net for failures after loading starts
- Regression verification:
  - focused accounting/report tests and isolated WPF solution build
- Remaining manual check:
  - enter a mismatched check total and confirm the warning appears with no loading overlay and no new voucher, then correct it and save successfully

## 2026-08-04 Customer Credit Collection by Receipt Voucher
- Scope tested:
  - Customer Debts report selection and bilingual `Receive payment / تحصيل دفعة` action
  - receipt voucher customer and outstanding-balance prefill
  - partial payments, positive-amount validation, overpayment rejection, and invalid credit-method rejection
  - customer receipt posting to cash/bank versus Accounts Receivable
  - customer tagging on the receivable journal line and preservation through voucher persistence
  - existing credit-invoice and party-balance report regressions
- Verification:
  - focused accounting/report tests: 7 passed, 0 failed, 0 skipped
  - final isolated WPF solution build including the migration: passed with 0 errors and existing warnings
- UI/UX findings:
  - the action uses existing hero/secondary button styles and is hidden for supplier reports
  - it is disabled until a customer with a positive balance is selected
  - collection mode locks the selected customer, removes the credit payment method, and refreshes the report after closing
- Remaining manual checks:
  - apply the new migration, restart the application, and test cash/check/card collection with a live cashier session
  - visually verify Arabic/English text fit at supported window sizes
  - concurrent collections are not transactionally rechecked against the latest balance; refresh the report before collecting

## 2026-08-04 Automatic PDF Export After Customer Collection
- Scope tested:
  - successful customer collection opens the existing Receipt Voucher PDF save dialog
  - a completed export opens the PDF in the system viewer
  - canceling export leaves the saved payment intact and closes the collection dialog normally
  - PDF generation/viewer errors show a bilingual warning without reporting the payment as failed
- Verification:
  - code inspection confirms export runs only after voucher and financial posting succeed
  - isolated WPF solution build validates the updated collection-to-PDF integration
- Remaining manual check:
  - complete a live collection, choose a PDF path, and confirm the configured Windows PDF viewer opens the exported voucher

## 2026-08-04 Credit Invoice Party Balance Fix
- Scope tested:
  - credit sales count only the accounts-receivable settlement line as customer debt
  - debit-card sales do not create customer debt
  - future invoice journals retain explicit customer/supplier tags
  - historical untagged credit invoices are recovered through the configured receivable/payable control account
- Verification:
  - focused accounting/report tests: 6 passed, 0 failed, 0 skipped
  - isolated full solution build: passed with 0 errors and existing warnings
- Expected result:
  - a credit sale for 1,493 displays a customer balance of 1,493 instead of 0
- Remaining manual check:
  - restart the application and verify the reported balance against the live SQL Server invoice and customer record

## 2026-08-04 Compact Products and Journal Headers
- Scope tested:
  - Products table hero header and search/filter card
  - Journal Entries browser hero header and filter card
  - preservation of existing controls, handlers, colors, and RTL layout
- Implementation:
  - reduced both hero rows from 220px to 128px
  - reduced overlap, card padding, control spacing, and button/input heights
  - placed Products paging/filter status text on one compact row
  - estimated combined header and filter height is below 250px on both screens
- Verification:
  - isolated full solution build and WPF XAML compilation passed with 0 errors
  - existing repository warnings remain
- Remaining manual checks:
  - restart the application and visually confirm both screens at the supported window sizes
  - verify long English translations and narrow-window wrapping do not increase the Products filter beyond 250px

## 2026-08-03 Customer Debts and Supplier Payables Reports
- Scope tested:
  - Accounting dashboard Reports group and both report actions
  - customer balance calculation (`Debit - Credit`)
  - supplier balance calculation (`Credit - Debit`)
  - posted-entry and as-of-date filtering
  - name search and outstanding-only filtering
  - WPF compilation, bilingual labels, loading/error handling, printing, and statement drill-down wiring
- Verification:
  - focused `PartyBalanceReportServiceTests`: 3 passed, 0 failed, 0 skipped
  - isolated full solution build: passed with 0 errors and existing warnings
  - normal Debug output build was blocked only by DLL locks from the running ROCCOPOS process and Visual Studio debugger
- Key findings:
  - both reports derive balances from posted journal lines rather than the potentially stale `User.CurrentBalance` field
  - no database migration is required
  - the new UI reuses the existing accounting report styles and supports Arabic and English
- Remaining manual checks:
  - restart the running application and confirm Accounting > Reports shows both actions in Arabic and English
  - compare representative customer and supplier totals with their detailed account statements against the live SQL Server data
  - print a multi-page result and confirm printer-specific pagination

## 2026-08-01 Direct Panda App-Cart Creation
- Scope tested:
  - synchronous Panda cart-to-Raccoon application-service integration
  - explicit `InvoiceType.appCart` pending-order status workflow
  - one-time stock reservation for pending orders
  - completion accounting without duplicate stock deduction
  - cancellation stock restoration
  - insufficient-stock rejection
  - Panda integration regression suite
- Verification:
  - focused `EndpointOrderStatusServiceTests`: 6 passed, 0 failed, 0 skipped
  - Panda integration tests: 6 passed, 0 failed, 0 skipped
  - Panda solution sequential build: passed with 0 errors and existing warnings
- Key findings:
  - pending `appCart` orders reserve stock once
  - completion posts accounting once and does not deduct stock again
  - cancellation restores held stock once
  - no live database writes were performed during automated QA
- Remaining checks:
  - configure `ConnectionStrings__RaccoonConnection` securely in deployment
  - create one controlled cart against a non-production database and verify product/unit mapping, invoice lines, stock lots, and status transitions
  - rotate the database credential that was shared during implementation planning

## Date
- 2026-03-09
- 2026-03-11
- 2026-03-24
- 2026-03-27
- 2026-04-11
- 2026-06-18

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
- `Accounting integration audit` (posting coverage, reversal safety, duplicate-post protection, inventory/accounting consistency review)
- `Box import request page` (pending cart preview, explicit import trigger, summary and error handling)

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
- Accounting integration integrity:
  - posting owner coverage by module
  - balanced journal generation expectations
  - source-reference traceability
  - duplicate posting prevention
  - reverse/repost behavior on update/cancel
  - inventory value vs accounting movement expectations
  - reporting readiness checks

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
- Phase 3 note:
  - this pass produced a deep audit and validation plan for accounting integration
  - no new automated tests were executed in this documentation-only update
  - latest solution build status for the Phase 2 integration slice:
    - `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.sln -v minimal`
    - passed with warnings only

## 2026-06-04 Notification Slice
- Scope tested:
  - notification delivery targeting the current logged-in user
  - admin-targeted notification routing
  - non-target user suppression
- Automated tests:
  - `NotificationServiceTests`
  - Passed: `2`
  - Failed: `0`
  - Skipped: `0`
- Build verification:
  - `dotnet build .\RaccoonWarehouse.sln`
  - `0 Error(s)`
- Key findings:
  - notification routing is now user/role aware
  - toast display is handled by the WPF app on the active UI thread
- Remaining risks:
  - no persistence or cross-device push backend yet
  - toast stacking/queuing is minimal and may need refinement for burst alerts

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

### Accounting Integration Audit
- Phase 3 objective:
  - validate that accounting posting is complete, balanced, non-duplicated, reversible, and reconcilable with operational inventory and money movement
- Posting owners reviewed:
  - `Invoice`
  - `Voucher`
  - `StockDocument`
  - `FinancialTransaction`
  - `StockAdjustment`
- Supporting operational rows that should not post independently:
  - `InvoiceLine`
  - `StockItem`
  - `StockTransaction`
  - `Check`
- Core audit checklist:
  - every posted journal must be balanced
  - every posted journal must reference its source document
  - each business event must have one posting owner only
  - cancel/update flows must reverse and repost instead of mutating posted entries
  - stock value movements must use cost, not sales price
  - returns must reverse revenue side and restore inventory side
  - damaged/internal stock out must not use sales-return accounts
  - `ReceiptVoucher` and `PaymentVoucher` must not double-post through `FinancialTransaction`
- Coverage matrix summary:
  - `Sales Invoice / POS Sale`
    - expected accounting: revenue, tax, settlement, COGS, inventory
  - `Sales Return / POS Return`
    - expected accounting: sales return, settlement reversal, inventory recovery, COGS reversal
  - `Stock In`
    - expected accounting: inventory increase vs payable/cash/gain fallback depending owner/classification
  - `Stock Out`
    - expected accounting: internal consumption or loss vs inventory
  - `Damaged Stock`
    - expected accounting: damaged stock loss vs inventory
  - `Receipt Voucher`
    - expected accounting: cash/bank/POS cash vs receivable/fallback
  - `Payment Voucher`
    - expected accounting: payable/expense vs cash/bank/POS cash
  - `Standalone FinancialTransaction`
    - expected accounting only when not already owned by another posted source flow
- Required transaction scenarios for manual/automated validation:
  - `Stock In from supplier on credit`
  - `Stock In from supplier paid cash`
  - `Sales Invoice cash`
  - `Sales Invoice credit`
  - `POS sale`
  - `Sales return cash refund`
  - `Sales return against receivable`
  - `POS return`
  - `Stock out for internal use`
  - `Damaged stock`
  - `Receipt voucher from customer`
  - `Payment voucher to supplier`
  - `Cancellation of posted document`
  - `Reversal entry generation`
  - `Duplicate post attempt`
  - `Retry after failure in mid-process`
- Expected validations for each posted transaction:
  - one active journal only for the source reference
  - debit total equals credit total
  - accounts used are active posting accounts
  - source `PostingStatus` matches the journal state
  - journal `ReferenceType` and `ReferenceId` match the source
- Edge cases to test:
  - partial return
  - mixed payment methods
  - cash + receivable split
  - supplier purchase paid immediately
  - negative stock scenario
  - retry after account-mapping/config failure
  - document update after posting
  - cancelled/deleted posted document behavior
  - concurrent posting attempts
- Recommended validation SQL / verification logic:
  - detect unbalanced journals
  - detect duplicate active journals per `(ReferenceType, ReferenceId)`
  - detect posted sources with no active journal
  - reconcile `Inventory` account balance against stock valuation
  - reconcile `COGS` account against posted sales cost totals
  - reconcile AR/AP movement against credit sales, receipts, purchases, and payments
- Reporting readiness checks:
  - trial balance debits must equal credits
  - general ledger running balance must reflect posted journals only
  - inventory account must reconcile to stock valuation or approved timing difference
  - P&L sales, returns, COGS, and expenses must match posted source activity
  - reversed entries must not overstate balances
- Common mistakes to watch for:
  - posting both source document and linked financial transaction
  - using selling price instead of cost for inventory postings
  - restoring return inventory at wrong cost
  - using wrong settlement account for cash vs credit
  - updating posted journals in place instead of reversing
  - `PostingStatus` set to `Posted` with no active journal
  - duplicate journals for one source
- Current result for this slice:
  - produced a complete audit and test plan for accounting integration
  - identified the highest-risk validation areas as:
    - duplicate posting across overlapping modules
    - inventory value reconciliation
    - update/cancel reverse-repost safety
    - fallback account-mapping correctness
- References:
  - `RaccoonWarehouse.Application/Service/Accounting/AccountingService.cs`
  - `RaccoonWarehouse.Application/Service/Invoices/InvoiceService.cs`
  - `RaccoonWarehouse.Application/Service/Vouchers/VoucherService.cs`
  - `RaccoonWarehouse.Application/Service/StockDocuments/StockDocumentService.cs`
  - `RaccoonWarehouse.Application/Service/FinancialTransactions/FinancialTransactionService.cs`
  - `RaccoonWarehouse.Tests/AccountingServiceReportTests.cs`

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
- Falcon stock API import slice (2026-06-09):
  - Added automated coverage for barcode normalization, including leading-zero API barcodes.
  - Added automated coverage for positive decimal quantity parsing and rejection of zero, negative, and invalid quantities.
  - Fixed the Current Stock import loading lifecycle:
    - loading now closes before success and error dialogs
    - loading closes on exceptions and timeout
    - the complete import has a three-minute cancellation timeout
    - the import button is restored on every exit path
  - Corrected Falcon increases to represent warehouse movement rather than purchasing:
    - positive stock movements use `Adjustment`, not `Purchase`
    - imported stock-in cost and movement unit price are zero
    - existing product sale prices are retained
    - zero-value Falcon stock-in vouchers do not create accounting journal entries
  - Verification:
    - Falcon-focused tests: 10 passed, 0 failed.
    - Localization JSON validation: passed.
    - Isolated desktop app build: passed with 0 errors and existing warnings.
    - Loading lifecycle regression verification on 2026-06-09:
      - Falcon-focused tests: 10 passed, 0 failed.
      - Isolated desktop app build: passed with 0 errors.
      - Localization JSON validation: passed.
    - Zero-cost warehouse movement correction verification on 2026-06-09:
      - Falcon-focused tests: 10 passed, 0 failed.
      - Isolated desktop app build: passed with 0 errors.
  - Existing unrelated test failures:
    - Running the full `StockServiceStockOutRulesTests` class also executed two pre-existing allocation tests that failed:
      - `AllocateOutgoingAsync_ShouldIgnoreExpiredLots`
      - `AllocateOutgoingAsync_ShouldUseSoonestExpiryFirst`
  - Remaining manual checks:
    - run the import against the live Falcon endpoint and a non-production database
    - verify warehouse assignment and voucher notes `from valcon api`
    - verify positive differences create one stock-in voucher
    - verify reductions create audited stock adjustment movements
    - verify unmatched barcodes and products without units are reported without crashing
- Accounting integration audit slice update (2026-04-11):
  - Added a Phase 3 audit and validation plan covering:
    - database integrity
    - business flow integrity
    - inventory/accounting integrity
    - voucher/financial integrity
    - edge/failure scenarios
    - reporting readiness
  - Current pass/fail snapshot for this slice:
    - Pass:
      - Phase 2 accounting integration build
      - audit plan documented
    - Fail:
      - none in this documentation pass
    - Blocked:
      - end-to-end execution of all accounting scenarios on a live or seeded QA database
      - concurrency validation under real parallel posting attempts
      - full automated scenario suite for voucher and stock-document posting/reversal
  - Remaining manual checks for this slice:
    - verify voucher create/update/reversal accounting on live DB
    - verify stock document create/update/reversal accounting on live DB
    - verify no duplicate journal is created when overlapping financial transaction rows exist
    - verify inventory account balance matches stock valuation after sales, returns, and damaged stock flows
    - verify AR/AP balances match voucher and invoice activity
  - Recommended next automated tests:
    - add service tests for voucher posting
    - add service tests for stock document posting
    - add duplicate-post regression tests
    - add reverse/repost regression tests
    - add reconciliation assertions for inventory and COGS totals
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
- Box order integration update (2026-06-09):
  - New Box carts are exposed through the isolated Raccoon pending-orders endpoint.
  - The Raccoon Orders dashboard synchronizes pending carts before loading local endpoint orders.
  - Imported carts are matched by normalized barcode and deduplicated by `BOX-CART-{id}`.
  - Synchronization failures close the loading window and still load existing local orders.
  - Verification:
    - Box solution isolated build: passed with 0 errors and existing warnings.
    - Raccoon desktop app isolated build: passed with 0 errors and existing warnings.
    - Box order import focused tests: 4 passed, 0 failed.
    - Configuration and localization JSON validation: passed.
  - Remaining manual checks:
    - run Box and Raccoon against a non-production database and confirm a new cart appears once in Orders
    - confirm unmatched product barcodes are skipped and reported without creating a partial invoice
    - configure the deployed Box API URL and add endpoint authentication before exposing it outside a trusted network
- Endpoint order stock and completion update (2026-06-09):
  - Endpoint orders reserve available stock when imported.
  - Repeated endpoint status saves do not duplicate stock movements.
  - `Completed` creates the standard sale invoice accounting entry without deducting stock again.
  - Endpoint orders default to credit payment so completion posts against customer receivables.
  - `Cancelled` restores reserved quantities to their consumed stock lots and reverses posted accounting.
  - Imports with insufficient stock are skipped and the newly created local invoice is removed.
  - The Orders status dialog now routes transitions through the service layer and always closes loading before dialogs.
  - Verification:
    - Endpoint order and Box import focused tests: 8 passed, 0 failed.
    - Isolated desktop app build: passed with 0 errors and existing warnings.
    - Localization JSON validation: passed.
  - Remaining manual checks:
    - verify the live database shows the stock reduction immediately after a Box order is imported
    - complete the order and verify one active invoice journal exists with AR, sales, COGS, and inventory lines
    - cancel a held and a completed order and verify stock/accounting restoration
- Box stock shortage message update (2026-06-09):
  - Replaced internal product/unit ID errors with a multi-line operational message containing:
    - Box cart number
    - product name
    - barcode
    - unit name
    - requested quantity
    - available quantity
    - missing quantity
    - corrective action
  - Verification:
    - Endpoint order and Box import focused tests: 8 passed, 0 failed.
    - Insufficient-stock assertions verified every displayed quantity and product field.
- Sales report initial-load update (2026-06-09):
  - The report now determines its initial period from the earliest through latest sale/return invoice.
  - The full report loads automatically when the window opens.
  - Customer, cashier, invoice type, and POS filters remain available for manual filtering.
  - Loading closes before initialization or report errors are displayed.
  - Verification:
    - Sales report focused tests: 2 passed, 0 failed.
    - Isolated desktop app build: passed with 0 errors and existing warnings.
    - Localization JSON validation: passed.
  - Endpoint-order follow-up:
    - Endpoint orders are included in the report rows and initial date range.
    - The invoice-type filter includes a localized endpoint-orders option.
    - Held endpoint orders remain visible but do not affect sales, tax, COGS, profit, count, or average totals.
    - Completed and posted endpoint orders are included in sales totals.
- Invoice profit browser update (2026-06-09):
  - The invoice profit browser now loads the same date span used by sales reporting.
  - Endpoint orders are available in the browser filter and row list.
  - Held endpoint orders remain visible but show zero profit/COGS.
  - Completed and posted endpoint orders contribute to profit calculations.
  - Verification:
    - Sales report focused tests: 2 passed, 0 failed.
    - Isolated desktop app build: passed with 0 errors and existing warnings.
    - Localization JSON validation: passed.
- Orders notification badge update (2026-06-09):
  - Added an order-received notification category to the existing app notification pipeline.
  - Imported Box orders now raise an in-app notification and can be localized by the WPF shell before the toast is shown.
  - The dashboard Orders tab now shows an unread badge for order-received notifications and clears the badge when Orders is opened.
  - Verification:
    - `NotificationServiceTests`: 3 passed, 0 failed.
    - Desktop app build: passed with 0 errors and existing warnings.
  - Remaining manual checks:
    - confirm the badge increments only for imported orders and not for the generic test notification
    - confirm the badge clears when the Orders window is opened
    - confirm the toast text is localized correctly in Arabic and English
- Box pending-order polling and status synchronization update (2026-06-10):
  - The dashboard now performs a lightweight pending-cart API check every 10 seconds while it is open.
  - The Orders badge displays the current API pending count instead of an in-memory unread counter.
  - Newly observed pending cart IDs raise one localized order notification; the initial snapshot establishes the baseline without producing a notification burst.
  - Polling does not show the loading window, access the local database, or import carts.
  - Full cart import and loading remain limited to opening Orders or clicking Refresh.
  - The badge automatically increases for new pending carts and decreases when the Box dashboard moves carts out of pending status.
  - Desktop status changes synchronize to Box using the published full-cart update contract:
    - `Unknown` -> Box status `0`
    - `Completed` -> Box status `1`
    - `InProcess` -> Box status `2`
    - `Cancelled` -> Box status `3`
  - Import-time `Unknown` handling skips the redundant Box update because the cart already has its initial API status.
  - Automated verification:
    - Box API/pending/status, import, endpoint status, and notification focused tests: 18 passed, 0 failed.
    - Solution Release build: passed with 0 errors and existing warnings.
    - App configuration and localization JSON validation: passed.
  - Remaining manual checks:
    - leave the dashboard open and confirm the badge refreshes within 10 seconds after a new Box cart is created
    - confirm one toast appears for each newly observed cart and does not repeat on later polling cycles
    - change a cart from the Box dashboard and confirm the desktop badge decreases within 10 seconds
    - complete or cancel a Box order from the desktop and confirm the Box dashboard status changes
    - confirm opening Orders still shows loading only for the full import/load operation
- Product search debounce update (2026-06-10):
  - Replaced cancellation-token debounce in the Products window with the version-counter pattern used by Current Stock.
  - Rapid name or barcode typing no longer throws expected `TaskCanceledException` instances into the Visual Studio debugger.
  - Stale delayed searches are ignored before loading, and stale database results are ignored before updating the grid.
  - Product filtering and pagination behavior remain unchanged.
  - Verification:
    - Desktop Release build: passed with 0 errors and existing warnings.
    - Serial solution Release build: passed with 0 errors and existing warnings.
  - Remaining manual checks:
    - type product names and barcodes rapidly while debugging and confirm Visual Studio no longer breaks on task cancellation
    - confirm only the latest entered search value updates the product grid
- Orders loading and synchronization review (2026-06-10):
  - Review findings:
    - opening Orders previously waited for the Box API and full import before displaying local orders
    - dashboard polling and Orders synchronization independently call the pending-orders endpoint
    - the Box API repository is not part of this solution, so a server-side incremental `changes?after=` endpoint remains a separate deployment task
  - Desktop update:
    - local orders now load immediately when the window opens
    - the existing Box import runs afterward in the background using an isolated dependency-injection scope and database context
    - the table reloads only when the background import creates new orders
    - the Refresh button remains an explicit synchronized refresh with the loading window
    - order table queries are serialized separately from synchronization, preventing overlapping queries on the window database context
    - added English translations for the new Orders search and filter controls
  - Verification:
    - focused Box API, import, endpoint status, and notification tests: 18 passed, 0 failed
    - full test suite: 80 passed, 5 failed; failures are existing accounting and stock test failures outside this change
    - solution Release build: passed with 0 errors and existing warnings
    - localization JSON validation: passed
  - Remaining manual checks:
    - open Orders with a slow or unavailable Box API and confirm local orders appear without waiting for the API timeout
    - create a pending Box cart and confirm the open Orders table refreshes after background import
    - click Refresh and confirm the loading window remains visible for the explicit synchronization
    - confirm Arabic and English filter labels switch correctly
- Endpoint order status workflow correction (2026-06-10):
  - API orders now use only these operational statuses:
    - `Unknown`: newly received; stock is reserved immediately
    - `InProcess`: preparation started; existing stock reservation is retained
    - `Completed`: preparation finished; accounting is posted and the order is closed
    - `Cancelled`: stock reservation is restored and accounting is reversed when applicable
  - `Draft`, `OnHold`, `Posted`, and `Returned` remain outside the API-order workflow.
  - Existing enum numeric values were preserved; `Unknown` and `InProcess` were appended, so no schema migration is required.
  - Box status mapping is `Unknown=0`, `Completed=1`, `InProcess=2`, and `Cancelled=3`.
  - Import creates the local endpoint invoice as `Unknown`, reserves stock once, and does not send a redundant Box update.
  - Orders filters and the order status dialog expose only the four API-order statuses.
  - Accounting ignores `Unknown` and `InProcess`; completion posts once.
  - Verification:
    - focused Box API, import, and endpoint status tests: 14 passed, 0 failed
    - solution Release build: passed with 0 errors and existing warnings
    - localization JSON validation: passed
  - Remaining manual checks:
    - import a new Box cart and verify its local status is `Unknown` and stock decreases immediately
    - change it to `InProcess` and verify stock does not decrease again
    - complete it and verify Box status `1` plus one accounting entry
    - cancel an unknown or in-process order and verify reserved stock is restored
  - Orders table follow-up:
    - the status column now uses the same localized formatter as the status filter
    - `Unknown` displays as `Unknown` / `غير معروف`
    - `InProcess` displays as `In Process` / `قيد التجهيز`
  - Orders unread badge follow-up:
    - superseded by the live pending-count behavior below
- Endpoint order details editing (2026-06-10):
  - API orders now use the local invoice editor after import.
  - Users can add products, replace a selected line, delete lines, change units, and edit quantity or unit price.
  - Editing is available only while the order is `Unknown` or `InProcess`.
  - Completed and cancelled orders are read-only.
  - Quantities must be positive; prices must be zero or greater; at least one line is required.
  - Saving details:
    - validates every selected local product and product unit
    - releases the previous stock reservation
    - replaces the local invoice lines and recalculates taxes, costs, profit, and invoice totals
    - reserves stock for the new complete line set
    - restores the previous local lines and stock reservation if the new reservation fails
  - Box product details are not modified; Box synchronization remains status-only.
  - Verification:
    - focused Box API, import, status, and local details-edit tests: 16 passed, 0 failed
    - solution Release build: passed with 0 errors and existing warnings
    - localization JSON validation: passed
  - Remaining manual checks:
    - add, replace, and delete lines on an unknown order and verify totals
    - change product unit and quantity and verify the new stock reservation
    - request more stock than available and verify the old invoice and reservation are restored
    - verify Box item details remain unchanged while later status updates still synchronize
    - verify completed and cancelled orders disable all line-editor controls
  - Legacy order compatibility:
    - endpoint orders previously stored as `OnHold` can use the local line editor
    - the details window displays legacy `OnHold` as `Unknown` for the current API workflow
    - completed and cancelled orders remain read-only
    - focused endpoint status/edit tests: 6 passed, 0 failed
    - solution Release build: passed with 0 errors and existing warnings
- Orders pending-count and explicit loading correction (2026-06-10):
  - the dashboard checks the Box pending endpoint every 10 seconds and displays pending carts not yet acknowledged by opening Orders
  - polling does not import carts or access the local orders database
  - newly observed cart IDs still publish one notification after the initial baseline
  - opening Orders performs one explicit Box import followed by the local table load with the loading indicator
  - clicking Refresh performs the same import/load operation
  - automatic background import after opening Orders was removed
  - opening Orders clears the badge and acknowledges all cart IDs currently known by the dashboard
  - later polls keep acknowledged cart IDs cleared; only newly detected pending cart IDs restore the badge
  - pending carts removed by an API-side status change are also removed from the unread badge
  - focused Box API, import, and endpoint-order tests: 17 passed, 0 failed
  - solution Release build: passed with 0 errors and existing warnings
- Box import request page update (2026-06-18):
  - Repurposed the legacy Import Order window into an explicit Box import request page.
  - The page previews pending Box carts before import and shows received/imported/skipped counts after import.
  - Refresh and import operations use the existing Box API/import services and disable repeated actions while loading.
  - Import errors are shown in an on-screen list without bypassing the service-layer deduplication, stock reservation, or accounting workflow.
  - Added English localization entries for the new import request labels.
  - Verification:
    - localization JSON validation: passed
    - `dotnet build RaccoonWarehouse-master\RaccoonWarehouse.sln`: passed with 0 errors and existing warnings
    - focused Box API, import, and endpoint-order tests: 17 passed, 0 failed
  - Remaining manual checks:
    - open Import Orders from the Sales dashboard and confirm pending Box carts render correctly
    - import against a non-production Box API and verify the summary counts match created local orders
    - confirm skipped carts show operational error messages without creating partial orders
    - confirm View Orders opens the existing Orders table after import
- Falcon API pause (2026-06-18):
  - Disabled the Falcon stock import entry point in the Current Stock window and removed the Falcon DI registration.
  - The stock browser, search, export, and adjustment flows remain active.
  - Verification:
    - isolated solution build with `BaseOutputPath` override: passed with 0 errors and existing warnings
  - Remaining risk:
    - `FalconStockImportService` still exists in the application layer for future reactivation, but the UI can no longer launch it
- Box API pause (2026-06-18):
  - Temporarily disabled Box polling, Box order refresh/import sync, and Box status push-back paths.
  - Dashboard order loading now remains local only while the Box connection is paused.
  - Import Order shows a disabled-state message instead of calling the Box server.
  - Orders table refresh now loads local orders only.
  - Verification:
    - isolated solution build with `BaseOutputPath` override: passed with 0 errors and existing warnings
  - Remaining risk:
    - Box service classes still exist and can be re-enabled later, but no live Box request should be made from the UI paths now

## 2026-07-21 Gemini Chatbot and Box Polling Fix
- Scope tested:
  - Gemini settings model validation and fallback
  - English and Arabic invalid-model feedback
  - dashboard automatic Box pending-order polling
- Findings and fixes:
  - the stored chatbot model was `gpt-5.4-mini`, which is incompatible with the Gemini API
  - incompatible stored models now fall back to `gemini-3.5-flash`
  - non-Gemini model names are blocked before a connection test
  - automatic Box polling was still active despite the Box integration pause and repeatedly raised handled `HttpRequestException` instances when the remote host closed the connection
  - dashboard startup and the 10-second timer no longer call the paused Box endpoint; manual Box service operations remain available
- Verification:
  - focused `BoxCartApiServiceTests` and `BoxOrderImportServiceTests`: 11 passed, 0 failed, 0 skipped
  - final solution build passed with 0 errors and existing warnings
- Remaining risks:
  - live Gemini authentication was not executable in the automation shell because the saved API key is protected for the interactive Windows user with DPAPI
  - re-enabling Box polling requires confirming endpoint availability and setting an appropriate failure backoff

## 2026-07-21 Chat Assistant Inline Thinking State
- Scope tested:
  - inline loading state after sending a chat message
  - English and Arabic loading text
  - success and error cleanup paths
  - message ordering, scrolling, and input disabled/enabled states
- Implementation:
  - an assistant bubble displays `Thinking…` / `جارٍ التفكير…` immediately after the user message
  - the temporary bubble is removed before the response or error message remains in the conversation
  - cleanup in `finally` prevents a stale loading bubble after unexpected failures
- Verification:
  - code inspection passed for loading, success, error, and final-cleanup states
  - final solution build passed with 0 errors and existing warnings
- Remaining manual check:
  - confirm the temporary bubble renders and scrolls correctly in the running WPF window in both languages

## 2026-07-22 Gemini API Rejection Diagnostics
- Scope tested:
  - non-success Gemini generation responses
  - API error parsing without exposing the API key
  - settings connection test behavior
  - bilingual rejection guidance
  - inline thinking-message replacement on rejected requests
- Findings and fixes:
  - the app previously threw `InvalidOperationException` for expected Gemini HTTP rejection responses, causing Visual Studio to break before the UI catch completed
  - rejected responses now return an assistant message containing the HTTP status and Google-provided reason
  - Settings now tests a small `generateContent` request rather than only checking that the model exists
  - Settings guidance now covers API key, model access, quota, region, and Google AI Studio billing
- Verification:
  - isolated full solution build passed with 0 errors and existing warnings
  - the normal build was blocked only because the running Visual Studio/debugger process locked application DLLs
  - code inspection passed for accepted and rejected response paths and English/Arabic settings text
- Remaining manual check:
  - restart the application, send one request, and use the displayed Gemini HTTP status/reason to resolve the Google AI Studio project restriction

## 2026-07-22 Animated Chat Thinking Indicator
- Scope tested:
  - thinking-only visual state
  - animated loading indicator
  - smaller muted loading text
  - English/Arabic text and RTL compatibility
  - normal message and timestamp regression
- Implementation:
  - added a stable `IsThinking` message-state flag
  - the temporary message shows a built-in indeterminate WPF progress animation beside 12px text using `AppMutedTextBrush`
  - the temporary timestamp is hidden while normal message text and timestamps remain unchanged
- Verification:
  - isolated full solution build passed with 0 errors and existing warnings
  - XAML compilation passed for the state triggers, binding, and indeterminate animation
- Remaining manual check:
  - visually confirm animation speed, spacing, and RTL placement in the running chat window

## 2026-07-22 Gemini Chat Latency Tuning
- Scope tested:
  - Gemini 3.5 Flash request configuration
  - reduced reasoning effort for chat-style prompts
  - response-length constraint
- Implementation:
  - set `thinkingConfig.thinkingLevel` to `minimal`
  - limited generated responses to 1,024 output tokens
- Verification:
  - isolated full solution build passed with 0 errors and existing warnings
  - request serialization was inspected for both latency settings
- Remaining manual check:
  - restart the running debugger and compare response time using the same prompt and network connection

## 2026-07-22 Circular Chat Loading Spinner
- Scope tested:
  - circular thinking indicator geometry
  - continuous rotation animation
  - thinking-state start/stop behavior
  - existing text, color, and localization regression
- Implementation:
  - replaced the linear indeterminate progress bar with a 16px circular ring and rotating accent arc
  - animation runs at one rotation per 0.8 seconds only for the thinking message
- Verification:
  - isolated full solution build passed with 0 errors and existing warnings
  - XAML storyboard, trigger, and transform compilation passed
- Remaining manual check:
  - confirm the spinner direction, speed, and spacing in both English and Arabic at runtime

## 2026-07-22 ROCCOPOS Chat Assistant Knowledge Guide
- Scope tested:
  - local bilingual workflow documentation loading and keyword matching
  - Gemini prompt restriction to the single matched workflow
  - English/Arabic answer and action-label selection
  - optional stable dashboard action keys and chat action button
  - existing loading spinner regression through XAML compilation
- Implementation:
  - added a maintainable `ChatAssistant/Knowledge/ROCCOPOS_HELP.json` file with 13 initial workflows
  - only the best matching topic is sent to Gemini; unmatched questions instruct Gemini not to invent product behavior
  - assistant responses can expose an existing dashboard action such as customer creation, sales return, or stock adjustment
  - no live database data is sent to Gemini
- Verification:
  - full solution build passed with 0 errors and existing warnings
  - knowledge JSON parsed successfully with 13 topics and was copied to the build output
  - existing solution tests: 82 passed and 5 failed in pre-existing accounting and stock allocation areas unrelated to this change
- Remaining manual checks:
  - ask matching English and Arabic questions and confirm the documented steps are returned
  - confirm each displayed Open Window button launches its existing dashboard action and respects its existing permission check
  - review and refine workflow wording in `ROCCOPOS_HELP.json` with product owners as procedures change

## 2026-07-22 Sidebar and POS Knowledge Expansion
- Scope tested:
  - documented navigation paths against the main dashboard sidebar and module actions
  - POS guidance against the actual POS left-sidebar controls and keyboard shortcuts
  - bilingual topic completeness and JSON packaging
- Implementation:
  - updated existing workflows to start from their real main-sidebar section and displayed action group
  - added 8 POS topics: opening/closing sessions, new sales, product search, hold/resume, returns, receipts/payments, and shift summary
  - documentation-only change; chatbot code, navigation handlers, Gemini configuration, and loading UI were not changed
- Verification:
  - JSON parsed successfully with 21 total topics, including 8 POS topics and 0 incomplete topics
  - full solution build passed with 0 errors and existing warnings
- Remaining manual check:
  - ask the chatbot POS questions in English and Arabic and compare each answer with the visible sidebar labels in the running application

## 2026-07-22 Full Chat Assistant Feature Documentation Audit
- Scope tested:
  - all dashboard module definitions and stable action keys
  - all 19 entries in `ReportCatalog`
  - operational windows for products, catalog setup, customers, users, employees, delegates, invoices, vouchers, stock, orders, accounting, checks, settings, sessions, and POS
  - bilingual knowledge-topic structure and output packaging
- Implementation:
  - expanded `ROCCOPOS_HELP.json` from 21 to 65 bilingual workflows
  - added missing create/search/edit, session, settings, accounting, check-status, operational, and reporting guidance
  - associated existing stable navigation keys only where a registered application action already exists
  - documentation-only change; no chatbot service, UI, Gemini configuration, database, or navigation implementation changed
- Verification:
  - JSON parsed successfully with 65 topics, 0 duplicate IDs, and 0 incomplete topics
  - all 19 registered report catalog entries have documented action keys
  - full solution build passed with 0 errors and 5 existing warnings
- Remaining manual checks:
  - restart the application so the cached documentation reloads
  - sample questions from each module in English and Arabic and verify terminology against the running UI
  - periodically repeat the documentation audit whenever new screens or dashboard actions are added

## 2026-07-22 Product Navigation Consolidation
- Scope tested:
  - main sidebar visibility for Categories and Brands
  - Products dashboard All / الكل group
  - routing of category, subcategory, and brand create/list actions
  - English/Arabic labels and chatbot navigation paths
- Implementation:
  - removed Categories and Brands from the visible main sidebar
  - added six existing actions to the Products dashboard under the localized All / الكل group
  - reused existing dashboard button styles, action keys, and handlers without changing window behavior
  - updated three chatbot help topics to route users through Products > All
- Verification:
  - confirmed both obsolete sidebar buttons are collapsed without leaving layout space
  - confirmed all six stable action keys are present in the Products module
  - knowledge JSON parsed successfully and all three moved help topics remain available
  - full solution build and WPF XAML compilation passed with 0 errors and existing warnings
- Remaining manual check:
  - open Products in English and Arabic and confirm the All / الكل group wraps cleanly and each of its six buttons opens the expected window

## 2026-07-22 Product Dashboard Group Ordering
- Scope tested:
  - Products dashboard group order and action membership
  - removal of the temporary All / الكل group
  - English/Arabic headings and chatbot navigation paths
- Implementation:
  - reordered the page as Products, Categories and Subcategories, Brands, then Reports
  - moved the four category actions and two brand actions into their dedicated groups
  - combined the existing pricing, profitability, inactive-product, and inventory-control report actions under Reports
  - updated five chatbot topics so no guidance references All or Cards and Items
- Verification:
  - confirmed all four localized group headings appear in the requested source order
  - knowledge JSON parsed with 65 topics and 0 stale references to the replaced group names
  - normal build was blocked by DLLs locked by the running debugger; isolated full solution and WPF build passed with 0 errors and existing warnings
- Remaining manual check:
  - restart the running application and confirm the four groups display in order and wrap cleanly in both languages

## 2026-07-30 Panda Live Order Synchronization
- Scope tested:
  - Raccoon SignalR/HTTP client compilation and dependency-injection wiring
  - inbox entity/configuration and isolated migration operations
  - database-backed Orders window refresh after a live import
  - existing automated regression suite
- Implementation:
  - added an outbound SignalR client with reconnect catch-up through Panda's durable pending-event API
  - added transactional/idempotent Panda order processing, strict product/unit mapping, invoice creation, and stock effects
  - removed the Orders window's dependency on manually calling Panda during refresh
  - kept synchronization disabled by default and kept the API key outside source/configuration
- Verification:
  - WPF project build passed with 0 errors; existing repository warnings remain
  - regression suite: 82 passed, 5 failed, 0 skipped (87 total)
  - failures are in pre-existing accounting and stock-rule tests; no live-sync test failed because dedicated processor tests are not yet present
  - generated migration was reviewed and its executable operations were restricted to IntegrationInbox only
- Remaining checks:
  - reconcile the pre-existing EF model/snapshot drift before approving any database migration
  - add focused processor tests for duplicate delivery, invalid mapping rollback, and successful invoice/stock creation
  - run one isolated end-to-end order with synchronization explicitly enabled and a securely supplied API key

## 2026-07-30 Panda-to-Raccoon Invoice Confirmation Without API Key
- Scope tested:
  - unauthenticated Panda integration endpoints and SignalR connection
  - Panda cart wait behavior for completed, failed, and unconfirmed Raccoon imports
  - Raccoon invoice-import acknowledgement and rejection reporting
  - affected application builds and Panda outbox regression tests
- Implementation:
  - removed the integration-key requirement from the Panda middleware and Raccoon synchronization client
  - kept invoice persistence inside Raccoon through `PandaOrderProcessor` and Raccoon's own database context
  - made Panda wait up to 30 seconds for Raccoon to acknowledge or reject the invoice
  - returned a clean rejection or timeout message to the cart client instead of unconditional success
  - added operator-visible startup synchronization errors and structured import error logging
- Verification:
  - Panda integration tests: 6 passed, 0 failed, 0 skipped
  - Panda API Release build passed with 0 errors and existing warnings
  - Raccoon WPF Release build passed with 0 errors and existing warnings
  - source inspection confirmed no remaining API-key guard in the active Panda/Raccoon synchronization path
- Remaining checks and risks:
  - run a live cart with both applications connected to their intended databases and confirm the Raccoon invoice ID is recorded
  - stop Raccoon and confirm Panda returns the 30-second unconfirmed-invoice message
  - force a product or unit mapping failure and confirm Panda returns Raccoon's rejection reason
  - integration endpoints are unauthenticated and must be restricted to a trusted network

## 2026-08-01 Endpoint Order Save-Changes Stock Movement Guard
- Scope tested:
  - app-cart order detail editing from the order-details window
  - unchanged detail saves and stock-movement idempotency
  - changed quantity/price persistence and stock reservation replacement
  - invalid pending editor values and localized no-change feedback
- Implementation:
  - applies pending selected-line editor values when Save Changes is clicked
  - detects unchanged UI details before calling the application service
  - adds a service-layer no-op guard before any stock release or reservation
  - preserves the existing stock replacement behavior for genuine detail changes
- Verification:
  - focused `EndpointOrderStatusServiceTests`: 7 passed, 0 failed, 0 skipped
  - Raccoon WPF project build passed with 0 errors; existing warnings remain
  - full solution build passed with 0 errors; existing warnings remain
- Remaining manual checks:
  - restart the desktop application, open an Unknown/In Process app-cart order, and verify an unchanged Save Changes click reports no changes and creates no movement
  - edit a selected line without first clicking Add/Replace, click Save Changes, reopen the order, and confirm the new values persisted

## 2026-08-01 Required Purchase Expiry and Admin Average Cost
- Scope tested:
  - Stock In and Purchase Invoice expiry-date validation
  - propagation of purchase expiry into stock movements and lots
  - Admin-triggered weighted-average ProductUnit purchase cost
  - service-boundary rejection before invalid invoice/document persistence
- Implementation:
  - removed the silent six-month expiry fallback from both purchase entry screens
  - added bilingual UI validation and application-service validation
  - purchase stock movements now carry the selected line expiry date
  - Admin inbound purchases recalculate catalog cost from remaining base quantities and lot values
  - individual lot purchase prices remain unchanged for audit and allocation history
- Verification:
  - focused new regression tests: 4 passed, 0 failed, 0 skipped
  - stock-rule class: 17 passed, 2 failed; the remaining failures are pre-existing allocation fixtures with missing product-unit setup and stale fixed expiry dates
  - full solution build passed with 0 errors; existing warnings remain
- Remaining manual checks:
  - verify both screens block adding a line when expiry is empty in Arabic and English
  - create an Admin purchase against existing stock and confirm the ProductUnit purchase price matches the weighted average
  - create the same purchase as a non-Admin and confirm the catalog purchase price is unchanged

## 2026-08-01 Weighted-Average Stock and Sales Cost
- Scope tested:
  - stock summary purchase cost after purchases at different costs
  - outgoing sale allocation cost across multiple active lots
  - preservation of expiry-first physical stock allocation
- Implementation:
  - stock summaries now use the quantity-weighted average cost of remaining active lots
  - outgoing allocations use that same average cost for invoice cost and profit calculations
  - stock valuation and item-cost reports use the summarized weighted cost instead of selecting the newest or nearest-expiry lot cost
  - lot prices remain unchanged, and expiry dates continue to control which physical lot is deducted first
- Verification:
  - focused weighted-average regression tests: 2 passed, 0 failed, 0 skipped
  - stock-rule class: 17 passed, 2 failed; both failures are the same pre-existing expiry/allocation fixture failures
  - full Release solution build passed with 0 errors; existing warnings remain
  - Debug build was not usable for final verification because the running Raccoon process locked its output DLLs
- Remaining manual checks:
  - add 100 units at cost 4 and another 100 units at cost 2, then confirm item cost is 3
  - complete a sale and confirm its cost uses 3 while the soonest-expiring lot is deducted first

## 2026-08-01 Fresh ROCCOPOS Installer Export
- Scope tested:
  - Release solution build from current source
  - self-contained Windows x64 single-file publish
  - Inno Setup installer compilation from the fresh publish output
  - installer existence, size, and SHA-256 integrity evidence
- Verification:
  - Release solution build passed with 0 errors; existing warnings remain
  - self-contained single-file publish passed
  - Inno Setup 6.7.3 compilation passed
  - installer generated at `publish/installer/ROCCOPOS-Setup.exe`
  - installer size: 118,134,136 bytes (112.66 MB)
  - SHA-256: `3EE1CA7250092575AC7C5004253ED7B7C65DA345E39EAD87DD21FF9A915E9241`
- Remaining manual checks:
  - install on a clean Windows x64 machine and verify application launch and database connectivity
  - confirm the weighted-average stock-cost workflow with representative production-like data

## 2026-08-01 Item Cost Details Dashboard Action
- Scope tested:
  - Items and Classifications dashboard report-button definition
  - stable action-key handling and existing report-window navigation
  - Arabic and English button text
- Implementation:
  - added `Products.ItemCostDetails` to the Products Reports group
  - routed the action to the existing `ItemCostDetailReport` window
  - reused the dashboard's existing button style, layout, and report-window behavior
- Verification:
  - source inspection confirmed the action is declared and handled exactly once
  - Release solution build passed with 0 errors; existing warnings remain
- Remaining manual checks:
  - open Items and Classifications in Arabic and English and confirm the button text fits
  - click Item Cost Details and confirm the report opens, loads data, filters, and closes normally

## 2026-08-19 Partial Imported Invoice Notes
- Scope:
  - Panda cart/export and `OrderSubmitted.v1` note propagation
  - ROCCOPOS partial imported-invoice creation when one or more items are unmatched
  - missing barcode, product name, quantity, and price note formatting
  - invoice-level `Notes` persistence and EF migration
- Implementation:
  - matched items continue through normal invoice, stock reservation, and accounting processing
  - unmatched items are excluded from stock/accounting lines and recorded in invoice notes
  - all-missing orders remain skipped because a valid stock/accounting invoice requires at least one matched line
  - migration `20260819073117_AddInvoiceNotes` adds only `Invoice.Notes`; unrelated model drift was excluded from the migration operations
- Verification:
  - Panda integration tests: 6 passed, 0 failed, 0 skipped
  - ROCCOPOS importer tests: 5 passed, 0 failed, 0 skipped
  - Panda solution build: passed with 0 errors and existing warnings
  - ROCCOPOS solution build: passed with 0 errors and existing warnings
- Remaining manual checks:
  - apply the new migration to a backed-up database
  - create a Panda order containing one item that cannot map in ROCCOPOS and confirm the invoice is created with the matched line plus the missing-item note
  - verify the imported invoice note is visible through the invoice/order details workflow

## 2026-08-19 Flexible Decimal Input in Product Search Windows
- Scope:
  - POS product search sale-price editing
  - purchase product search purchase-price editing
  - Arabic and English decimal separators and Arabic numerals
  - preservation of existing search, keyboard, and validation behavior
- Implementation:
  - added a locale-tolerant decimal converter accepting `.`, `,`, Arabic decimal separators, and Arabic/Persian numerals
  - applied it only to the editable sale and purchase price columns
- Verification:
  - full ROCCOPOS solution build: passed with 0 errors; existing warnings remain
  - source inspection: both search windows reference the converter
- Remaining manual checks:
  - edit a price as `12.500` in POS and purchase search windows and confirm it remains after leaving the cell
  - repeat with `12,500` and confirm both values are accepted
  - confirm quantity editing and Add/Enter keyboard navigation remain unchanged
## 2026-08-19 Below-Cost Sales and Voucher Party Quick Creation
- Scope:
  - POS and Create Sales Invoice below-cost price behavior
  - customer quick creation from receipt voucher
  - supplier quick creation from payment voucher
  - preservation of stock, quantity, accounting, and permission validation
- Implementation:
  - removed automatic below-cost price restoration and save blocking; negative profit is retained in calculations
  - added account plus buttons to receipt/payment voucher pages
  - receipt creates Customer users; payment creates Supplier users, then refreshes and selects the new party
- Verification:
  - full ROCCOPOS solution build: passed with 0 errors; existing warnings remain
- Remaining manual checks:
  - sell an item below cost from POS and confirm invoice saves and profit is negative
  - sell below cost from Create Sales Invoice and confirm invoice saves
  - add a customer from receipt voucher and supplier from payment voucher; confirm each is selected and saved correctly
  - test the new buttons with a role lacking Users.Create permission
## 2026-08-19 POS Product Search Keyboard Navigation
- Scope:
  - POS product-search grid keyboard navigation and Add-button activation
  - Enter, Left/Right, Up/Down, Escape, focus restoration, and existing text-entry behavior
- Implementation:
  - centralized grid navigation through the window preview-key handler
  - Enter advances through visible cells and adds the product at the final action cell
  - Enter on the row Add (+) button adds that row without closing the search window
  - Left/Right move across cells in the same row; Up/Down move between rows; Up from the first row returns to the search box
  - only the relevant navigation keys are intercepted, preserving normal typing and other shortcuts
- Verification:
  - Release application build passed with 0 errors; existing warnings remain
  - source inspection confirmed the duplicate legacy Escape-only handler was removed
- Manual checks remaining:
  - open POS product search, press Down, move Left/Right across unit/quantity/price/Add, press Enter through the row, and confirm the product is added
  - press Enter on the + button and confirm the window stays open, search text is cleared, and focus returns to the search box
  - press Up from the first row and confirm focus returns to search
  - verify Arabic/English RTL layout and decimal price editing remain unchanged
## 2026-08-19 Voucher Selectors, Product Units, and Purchase Tax
- Scope:
  - receipt customer selector and payment supplier selector
  - product-create unit search and quick unit creation
  - purchase-invoice tax calculation and total persistence
- Implementation:
  - voucher selectors now load and search the complete user list locally without role filtering
  - typing clears the current party selection; the user must explicitly choose a result
  - product unit selection is searchable and includes a plus action that opens Create Unit, refreshes units, and selects the newly created unit
  - purchase lines snapshot product tax exemption/rate; subtotal, tax, and total recalculate after add/edit/delete
  - purchase total uses subtotal plus tax and saves SubTotal and TotalTax on the invoice
- Verification:
  - Release application build passed with 0 errors; existing warnings remain
  - no database migration was required because invoice tax fields already exist
- Manual checks remaining:
  - type a customer/supplier name, confirm no automatic selection, then explicitly select and save
  - confirm all users are available in both voucher selectors
  - search an existing unit, create a new unit from Product Create, and confirm it is selected after closing
  - add taxable and tax-exempt products to a purchase invoice and verify subtotal, tax, total, cash, credit, and check amounts
  - edit quantity and purchase price and confirm tax and total update correctly
## 2026-08-19 Voucher Search Selection Fix
- Scope:
  - customer search in receipt vouchers
  - supplier search in payment vouchers
  - no auto-complete or automatic first-result selection
- Implementation:
  - routed text-change handling now listens to the editable ComboBox template text box
  - filtering uses the cached complete user list
  - typed text and caret are restored after refreshing the result list
  - selection is cleared while typing and remains empty until the user explicitly selects a result
- Verification:
  - Release application build passed with 0 errors; existing warnings remain
- Manual checks remaining:
  - type a partial customer/supplier name and confirm filtered rows appear
  - confirm the combo remains unselected until a result is clicked
  - clear the text and confirm all users reappear
## 2026-08-21 Account Balance Filters and Export UI
- Scope tested: compact header, role filter, debit/credit/zero-balance filter, user-name search, and Excel export flow.
- Verification: focused PartyBalanceReportServiceTests 5 passed; full solution build passed with 0 errors and existing warnings.
- Remaining manual checks: verify Arabic/English layout and compare exported filtered rows with the grid.

## 2026-08-22 Customer Balance Sign Convention
- Scope tested: customer balances now display credit-sale debt as negative; supplier payables remain positive; customer payment action uses the absolute debt amount.
- Verification: PartyBalanceReportServiceTests 5 passed, 0 failed. Full solution build was blocked by running RaccoonWarehouse/debugger DLL locks, with no compilation errors reported before the lock failures.
- Remaining manual checks: restart the app, rebuild, create a credit sale, confirm the customer row is negative, and collect a partial payment.

## 2026-08-22 - Accounts Balances party relationship alignment

- Scope: allow the same user to be used in sales and purchases by treating `CustomerId` and `SupplierId` as transaction relationships; align account balances and statements; rename the combined report to Accounts Balances; preserve role, debit/credit, name, and phone filtering.
- Changes verified: Party Balance no longer excludes users based on `User.Role`; User Statement uses role-specific party links and the common signed balance `credit - debit`; combined supplier loading now applies the selected balance filter.
- Focused test: `dotnet test RaccoonWarehouse.Tests/RaccoonWarehouse.Tests.csproj --filter FullyQualifiedName~PartyBalanceReportServiceTests --no-restore -v:minimal`
- Result: 6 passed, 0 failed, 0 skipped.
- WPF build verification: isolated build to `C:\Users\obadaqafisheh\roccopos-build-check\` succeeded with 0 errors; normal output build was also attempted but its final copy was blocked by the running application/debugger locking output DLLs.
- Remaining risk: existing historical records with the wrong party relationship require data review/repair; no database records were modified automatically.

## 2026-08-22 - Prevent duplicate zero-balance party rows

- Fixed the combined Accounts Balances report so a zero-balance user appears only under the profile role selected for that user.
- A second customer/supplier row appears only when posted movements exist under the other transaction relationship.
- Focused PartyBalanceReportServiceTests: 6 passed, 0 failed, 0 skipped.

## 2026-08-23 - Unified party account balances

- Combined Accounts Balances now aggregates customer and supplier movements into one row per UserId.
- Example covered: customer debit 50 and supplier credit 50 produces total debit 50, total credit 50, and net balance 0.
- Double-clicking a combined row opens a statement containing both customer and supplier relationships.
- Focused PartyBalanceReportServiceTests: 7 passed, 0 failed, 0 skipped.
- Isolated WPF build: succeeded with 0 errors; existing warnings remain.

## 2026-08-23 - Balance amount filters and Excel export

- Added explicit balance comparison filters: greater than zero, equal to zero, and less than zero.
- Preserved compatibility with existing debit/credit/zero filter keys.
- Verified unified Accounts Balances Excel export uses the AccountsBalances filename, correct net outstanding total, and the currently filtered rows.
- Focused PartyBalanceReportServiceTests: 7 passed, 0 failed, 0 skipped.
- Isolated WPF build: succeeded with 0 errors; existing warnings remain.


## 2026-08-23 - Stock, accounting, voucher reload, and stock-in totals

- Explicitly wired invoice, voucher, stock, and stock-document services with IAccountingService in the application DI registration.
- Stock-in save now reports stock-document/accounting failures instead of showing false success.
- Payment voucher reload restores the selected customer/supplier account.
- Loading a saved stock-in document recalculates subtotal, discount, and total.
- Isolated solution build: succeeded with 0 errors; existing warnings remain.
- Focused purchase/accounting/voucher test command: 15 passed, 10 failed. The failures are existing fixture/expectation issues, including invalid non-numeric or non-3-to-5-digit invoice/voucher numbers and in-memory accounting setup failures; no new compilation failures were introduced.
- Manual UI verification remains required against the live database for stock-in journal visibility and supplier selection.


## 2026-08-23 - Inventory report quantity filters and Excel export

### Scope
- Renamed the current stock window to `تقرير الجرد`.
- Added quantity filters for all quantities, greater than zero, equal to zero, and less than zero.
- Kept the loaded/search result collection separate from the filtered display collection.
- Updated Excel export to export the same filtered rows currently displayed in the grid.

### Verification
- Isolated WPF build: `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.csproj --no-restore -v:minimal -p:OutDir=_codex_stock_report_build2` — passed, 0 errors.
- Excel export path was reviewed: it iterates `FilteredStockItems`, so changing the quantity filter changes both the grid and exported rows.
- Manual UI verification still recommended with live data: test each filter and open the generated `.xlsx` to confirm row count and values.

### Known build warnings
The solution still reports existing warnings, including package compatibility/security and nullable-code warnings; no new compile errors were introduced by this change.
## 2026-08-23 - Inventory dashboard label and displayed quantity total

### Scope
- Renamed the dashboard action that opens the inventory report to `تقرير الجرد`.
- Renamed the opened report header to `تقرير الجرد`.
- Added a header total that sums `CurrentQuantity` for all rows displayed by the report.

### Verification
- Isolated WPF build: `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.csproj --no-restore -v:q -p:OutDir=_codex_dashboard_inventory_build` — passed, 0 errors.
- The total is calculated from the same `rows` collection assigned to the report grid, preventing a mismatch between displayed rows and the header total.
- Manual dashboard click-through remains recommended to confirm the updated label appears in the running localization/session state.
## 2026-08-23 - Reports-tab current stock label and quantity total

### Scope
- Renamed the Reports-tab `current-stock` catalog item to `تقرير الجرد`.
- Renamed the current-stock window title/header to `تقرير الجرد`.
- Added a displayed quantity total that recalculates after loading, searching, and quantity filtering.

### Verification
- Isolated WPF build: `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.csproj --no-restore -v:q -p:OutDir=_codex_current_stock_build` — passed, 0 errors.
- The total is calculated from `FilteredStockItems`, matching the rows visible in the grid and the rows exported to Excel.
## 2026-08-23 - Current stock product-card double-click

### Scope
- Added double-click handling to the current inventory grid.
- Double-clicking a product row opens the existing `UpdateProduct` card using that row's `ProductId`.

### Verification
- Isolated WPF build: `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.csproj --no-restore -v:q -p:OutDir=_codex_current_stock_doubleclick_build` — passed, 0 errors.
- Manual UI check recommended: open Reports > تقرير الجرد, double-click a row, and confirm the correct product card opens.
## 2026-08-23 - Product profit report calculation correction

### Scope
- Updated product-profit report filtering to exclude draft, held, cancelled, unknown, and in-process documents from profit calculations.
- Sales and returns with valid completed/posted/legacy-null status remain included.
- Endpoint orders contribute only when completed or posted.
- Existing discount allocation, return signs, stored unit costs, and unit grouping remain unchanged.

### Verification
- Isolated WPF build: `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.csproj --no-restore -v:q -p:OutDir=_codex_profit_report_build` — passed, 0 errors.
- Manual financial reconciliation is still recommended against posted invoices, returns, discounts, and COGS for a known date range.

## 2026-08-30 - Stock-out operation types and standalone returns

### Scope
- Added explicit stock-out operation types for damage, expiry, internal use, purchase-invoice return, stock-in return, and customer-sale return.
- Added supplier/customer selection rules for return documents.
- Customer-sale returns restore inventory and use sale-price accounting; supplier/stock-in returns reduce inventory using purchase cost.
- Customer-sale returns can be entered even when the product currently has zero stock; ordinary stock-out operations retain FEFO allocation and availability checks.
- Added EF model/migration support for operation type, source-document reference, and customer relationship.

### Verification
- Isolated solution build: `dotnet build RaccoonWarehouse.sln --no-restore -p:OutDir=..\\_codex_stock_out_returns_final_build4 -v:minimal` — passed, 0 errors.
- Focused `StockServiceStockOutRulesTests`: 17 passed, 2 failed, 0 skipped, 19 total.
- The two failures are the existing FEFO expectations `AllocateOutgoingAsync_ShouldIgnoreExpiredLots` and `AllocateOutgoingAsync_ShouldUseSoonestExpiryFirst`; they fail before the new standalone customer-return path is exercised.

### Remaining risks
- The Stock Out screen does not yet provide a source-invoice selector; `SourceDocumentId` remains optional/null, so purchase-invoice and stock-in returns currently use the entered/product purchase price rather than importing a selected source line price.
- Manual live-database verification is required for migration application, journal balancing, supplier/customer balances, and quantity effects across each operation type.
