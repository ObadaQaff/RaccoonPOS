# Purchases Database Changes

No production database changes were executed.

The application models now include PurchaseRequests/PurchaseRequestLines, GoodsReceipts/GoodsReceiptLines, PurchaseInvoiceMatches/PurchaseInvoiceMatchLines, SupplierPaymentAllocations, SupplierAdvances, PurchaseReturns/PurchaseReturnLines, and optional PurchaseOrderLine.PurchaseRequestLineId traceability. EF model configuration adds unique document numbers and workflow indexes.

Existing purchase orders, invoices, invoice lines, stock movements/lots, vouchers, accounting journals, permissions, and audit logs are reused. No duplicate accounting engine or product/supplier table was introduced.

`SPIDER_PHARMACY_PURCHASES_DATABASE_UPDATE.sql` contains guarded SQL Server DDL for these changes, including indexes, foreign keys, preflight, and validation notes. It is manual-only and was not executed.

Still pending in the database/application: approval policy/history tables, attachment storage policy, credit-note settlement fields, supplier aging/report projections, and final accounting mappings after the business-policy decisions are confirmed.
