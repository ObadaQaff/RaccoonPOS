# Purchases Test Results

## Verification completed

- Both mandatory specifications were read; all 11 Excel worksheets were inspected without importing demo records.
- Full solution build passed with 0 errors after the purchase workflow changes.
- Focused purchase-order tests passed: 4 passed, 0 failed, including non-posting approval and mandatory rejection handling.

Command:

`dotnet test RaccoonWarehouse.Tests\\RaccoonWarehouse.Tests.csproj --no-restore --filter FullyQualifiedName~PurchaseOrderServiceTests --logger "console;verbosity=minimal"`

## Not yet claimable

T01-T14 are not all automated yet. GRN, matching, payment, return, accounting, concurrency, and UI scenarios still require isolated fixtures and manual verification.

Required next tests include partial receipt/duplicate confirmation, over-receipt authorization, three-way matching tolerances, duplicate supplier invoices, payment allocation and advances, returns/reversals, permissions, expiry, unit conversion, tax, rounding, POS regression, and journal reconciliation.
