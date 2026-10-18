# Purchases Implementation Report

## Completed and integrated

- Read the supplied Word guide and all 11 Excel worksheets; demonstration data was not imported.
- Preserved the existing products, suppliers, purchase invoices, stock movements, vouchers, accounting, permissions, and audit infrastructure.
- Changed purchase-order approval to be non-posting: approval records metadata only and does not create inventory or accounting entries.
- Added Purchase Requests with draft, submit, approve, reject, permissions, positive-quantity validation, duplicate-line validation, mandatory rejection reason, EF model/configuration, DI, and a WPF list/action screen.
- Added Goods Receipts with partial quantities, accepted/rejected/damaged quantities, expiry validation, PO remaining-quantity enforcement, idempotent confirmation, accepted/free stock posting through the existing stock service, EF model/configuration, DI, and a WPF screen.
- Added supplier-invoice matching over the existing purchase invoice model, PO/GRN accepted-quantity comparison, price/quantity tolerances, disputed status, and duplicate supplier invoice number detection.
- Added supplier payment allocations over existing posted vouchers and purchase invoices, with duplicate and over-allocation protection.
- Added purchase-return entities/service with accepted-receipt validation and idempotent stock reversal guards.
- Added PR-to-PO line traceability and server-side refusal of PO creation from non-approved PR lines.
- Added guarded manual SQL Server schema script; it has not been executed.

## Remaining gaps

The full Spider Pharmacy purchasing system is not yet complete. Remaining work includes configurable threshold/multi-level approval policies and full approval history UI; PO creation UI from approved PR lines; full GRN quality/attachments/cold-chain UI; invoice posting/credit-note accounting and reversal workflow; supplier advances/aging/statements; return credit-note settlement; purchasing reports/exports; concurrency fixtures; and complete T01-T14 automated coverage.

## Database

No SQL migration was executed. Apply `SPIDER_PHARMACY_PURCHASES_DATABASE_UPDATE.sql` manually only after backup and staging validation.
