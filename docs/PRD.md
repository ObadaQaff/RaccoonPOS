# Product Requirements Document

Last verified: 2026-06-13

## Product Summary

ROCCOPOS is a Windows desktop application for small and medium retail,
warehouse, and distribution operations. It combines product master data,
inventory, purchasing and sales documents, POS checkout, customer and supplier
accounts, vouchers, accounting, reporting, permissions, and selected external
integrations in one bilingual application.

This document records the product behavior evidenced by the current source.
It is not a roadmap. Unverified deployment behavior is marked explicitly.

## Users

- Administrators configure the system, permissions, reports, and optional modules.
- Cashiers operate POS and cashier sessions.
- Managers review operational and financial information.
- HR users may manage employee-related data where the feature is enabled.
- Customers and suppliers are business parties represented by user records.
- Delegates may be assigned to sales activity where the module is enabled.

Current roles include `Admin`, `Casher`, `Customer`, `Supplier`, `Manager`, and `HR`.

## Product Goals

- Maintain accurate product, stock-lot, stock-movement, and document records.
- Support fast cashier sales with barcode, keyboard, return, exchange, and hold flows.
- Keep sales, stock, money movements, and accounting entries traceable.
- Provide useful operational and accounting reports.
- Support Arabic and English user interfaces.
- Control access by role and configurable permission overrides.
- Integrate external orders without double-deducting inventory.

## Functional Scope

### Master Data

- Manage products, categories, subcategories, brands, units, warehouses, users,
  customers, suppliers, employees, and delegates.
- Support products with multiple units and conversion factors.
- Allow nullable domain fields to remain optional in UI validation.

### Inventory

- Receive and issue inventory through stock documents.
- Track lots, expiry, purchase cost, sale price, and movement history.
- Allocate outbound stock using FEFO and exclude expired lots.
- Support controlled batch adjustments without rewriting historical movements.
- Provide current-stock, movement, valuation, low-stock, and balance reports.

### POS And Sales

- Require authentication and an active cashier session.
- Add products by barcode, search, category, or product selection.
- Support cash, card, transfer, check, mobile, and credit payment types.
- Validate stock, sale price, customer credit eligibility, and return quantities.
- Support held invoices, returns, exchanges, receipt/payment entry, and PDF output.

### Accounting And Finance

- Maintain accounts and balanced double-entry journals.
- Post supported invoices, vouchers, financial transactions, stock documents,
  and stock adjustments.
- Support reversal rather than destructive journal editing.
- Enforce a configurable posting lock date.
- Provide trial balance, ledger, balance sheet, statements, and other reports.

### External Orders

- Poll Box for pending carts and show their count.
- Import carts only through an explicit order refresh/open action.
- Match products by normalized barcode and reject incomplete cart mappings.
- Reserve stock on import, post accounting on completion, and restore stock on cancellation.
- Synchronize supported status changes back to Box.

### Permissions And Optional Modules

- Authenticate before showing the main dashboard.
- Apply role permission overrides and report visibility rules.
- Allow only an active administrator to save permission changes.
- Support feature flags for accounting, employees, and delegates.

### Localization

- Support Arabic and English at runtime.
- Translate new labels, buttons, table headers, dialogs, reports, and messages.
- Use stable keys or identifiers for logic rather than translated display text.

## Cross-Cutting Requirements

- High-risk UI operations should use loading feedback, exception handling, and
  user-readable errors.
- Financial and inventory operations should be atomic or compensating.
- Repeated external requests should be idempotent.
- Sensitive credentials and passwords must not be stored in plaintext.
- Database schema changes must use reviewed EF Core migrations.
- Significant behavior changes must update the relevant feature document and QA evidence.

## Out Of Scope Or Not Confirmed

- Web or mobile clients are not present in this repository.
- Box server-side behavior and deployment configuration are external.
- Production migration state cannot be established from source alone.
- Multi-warehouse isolation is represented in the model but is not verified end to end.
- Full permission enforcement across every window/action is not currently confirmed.

## Acceptance Baseline

A release candidate should:

1. Build the full solution successfully.
2. Pass relevant automated tests with no unexplained failures.
3. Complete critical POS, stock, accounting, login, and external-order scenarios.
4. Preserve Arabic and English behavior.
5. Avoid partial stock, financial, or accounting commits on failure.
6. Contain no embedded production credentials or plaintext password handling.

The current source does not yet satisfy every baseline item; the feature
documents record known gaps.

