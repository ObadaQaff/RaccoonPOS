# Spider Pharmacy Purchasing Implementation Plan

## Current gate

The required Word guide and Excel workbook have now been read from the supplied Downloads path. The workbook contains 11 worksheets and its sample values are explicitly demonstration-only. No production database operation has been performed.

## Baseline audit

- Stack: .NET 8 WPF, EF Core, SQL Server, existing dependency injection and service layers.
- Existing purchasing foundation: `PurchaseOrder`, `PurchaseOrderLine`, `PurchaseOrderService`, purchase invoices, stock transactions, vouchers, accounting journal services, permissions, and audit logging.
- Existing pharmacy inventory foundation: product units/conversions, stock lots, expiry fields, stock movement sources, and supplier-linked products.
- Existing approval foundation: permission-gated purchase-order approval/rejection with timestamps and mandatory rejection reason.
- Critical defect corrected: purchase-order approval previously created a posted purchase invoice and inventory movements. Approval is now non-posting; receiving and supplier-invoice stages remain separate implementation work.
- First PR slice added: non-posting PR draft/submission/approval/rejection service with line snapshots, validation, navigation, and a basic BO screen.
- GRN, invoice matching, payment allocation, advances, purchase returns, PR-to-PO traceability, and manual SQL delivery are now implemented as domain/service slices; remaining UI and accounting policy work is tracked in the report.

## Execution phases

1. Obtain and parse both mandatory specifications.
2. Complete source/schema gap analysis and traceability matrix.
3. Extend purchasing domain with requests, receipts, matching, returns, approvals, and audit history while reusing existing services.
4. Integrate inventory and accounting posting with idempotency and document references.
5. Add BO screens, permissions, reports, exports, and navigation.
6. Add isolated acceptance and regression tests.
7. Produce a rerunnable manual SQL Server deployment script and deployment guide.
8. Perform final document-by-document review.

## Safety constraints

The generated SQL will be manual-only. It will not be executed against any database. Existing POS, stock, invoice, voucher, and journal behavior must be regression-tested before delivery.
