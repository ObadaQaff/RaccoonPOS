# Purchases Deployment Guide

The database update is manual only. Do not run it against production before a backup and staging validation.

1. Back up the application SQL Server database.
2. Open `SPIDER_PHARMACY_PURCHASES_DATABASE_UPDATE.sql` in SQL Server Management Studio or use `sqlcmd` against a staging database.
3. Review the preflight section and execute the script.
4. Run the validation query at the end and confirm the new tables, column, indexes, and constraints exist.
5. Confirm existing table row counts are unchanged.
6. Start the application, grant the required permissions, and run the smoke test below.

The script uses guarded object creation and is intended to be rerunnable, but it does not provide automatic rollback. If validation fails, stop and restore the staging backup; do not drop existing tables.

## Manual smoke test

1. Purchasing > Purchase Requests: create a positive-quantity draft, submit, approve, and verify no stock/AP entry is created.
2. Create a PO line referencing the approved PR line; verify a rejected PR line is refused.
3. Goods Receipts: create and confirm a partial receipt; verify only accepted/free quantity enters stock.
4. Confirm the same receipt again; verify no duplicate stock movement.
5. Match a purchase invoice to PO/GRN. Test an out-of-tolerance price and a duplicate supplier invoice number.
6. Allocate a posted supplier payment across purchase invoices and verify remaining balances.
7. Create and confirm a purchase return against accepted receipt lines; confirm it posts once.
