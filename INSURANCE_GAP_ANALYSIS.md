# Insurance Gap Analysis

Date: 2026-10-13  
Scope: Existing ROCCOPOS pharmacy insurance foundation and its POS/accounting boundaries.

## Executive summary

The repository contains an initial Insurance foundation added under `RaccoonWarehouse.Domain.Insurance`, an EF migration, a company-management window, permission definitions, and an application service for company listing, claim creation from an existing coverage row, and payment allocation. It is not yet a complete insurance sales or claims workflow.

The enhancement strategy is additive: preserve the current entities, migration, Dashboard module, company screen, and generic POS/accounting behavior; add missing capabilities behind explicit service contracts and insurance-specific fields. No production data is deleted or reinterpreted.

## Existing and preserved functionality

| Capability | Classification | Current implementation |
|---|---|---|
| Insurance company records | EXISTING | `InsuranceCompany`, `InsuranceManagementService`, `InsuranceCompaniesWindow` |
| Company code uniqueness and basic validation | EXISTING | Service validation plus unique EF index |
| Insurance networks | PARTIAL | Entity and table exist; no management UI/service |
| Beneficiary categories | PARTIAL | Entity and table exist; no management UI/service |
| Contracts and contract versions | PARTIAL | Entities and tables exist; no management UI/service |
| Coverage rules and exclusions | PARTIAL | Entities exist; no evaluator or configuration UI |
| Member policies | PARTIAL | Entity exists; no customer-linked management workflow |
| Pre-authorizations | PARTIAL | Entity exists; no request/review workflow |
| Invoice coverage snapshot | PARTIAL | Entity exists; POS does not create or calculate it |
| Claims | PARTIAL | Entity, listing, and creation service exist; Dashboard action is a placeholder |
| Claim batches and adjustments | PARTIAL | Entities exist; no lifecycle or export workflow |
| Claim payments | PARTIAL | Entity and allocation service exist; no receipt/accounting integration or UI |
| Attachments | PARTIAL | Entity exists; no secure file workflow |
| Insurance permissions | EXISTING | Catalog entries and permission-gated Dashboard actions exist |
| Ordinary cash/credit POS | EXISTING | Existing invoice, stock, payment, and accounting paths are preserved |
| Inventory costing and expiry allocation | EXISTING | Existing stock service remains authoritative |
| Accounting operation queue | EXISTING | Existing invoice/financial operation queue remains the integration point |

## Requested capability gap matrix

| Capability | Classification | Gap / safe enhancement |
|---|---|---|
| Insurer, administrator, contractual payer separation | MISSING | Add additive party roles and contract references; do not duplicate companies unnecessarily |
| Multiple plans | MISSING | Add plan entity linked to contract/version and member policy |
| Eligibility verification history | MISSING | Add manual/external/file/API-neutral verification record with operator, timestamp, method, status, and reference |
| Plan/product/category coverage | PARTIAL | Reuse existing product/category IDs; add effective dates and plan scope to rules |
| Contracted price and quantity limits | MISSING | Add optional rule values and a deterministic evaluator |
| Medicine-level authorization | PARTIAL | Pre-authorization entity exists; add line-level authorization links and lifecycle history |
| Insurance POS mode | MISSING | Extend existing invoice request/payment flow rather than creating a second sales engine |
| Mixed covered/uncovered invoices | MISSING | Add invoice-line coverage snapshots and patient/payer totals |
| Claim lifecycle and review | PARTIAL | Extend existing statuses, line decisions, review fields, and explicit transitions |
| Monthly batches and export | PARTIAL | Batch entity exists; add controlled selection, totals, and export boundary |
| Payer receivables | MISSING | Reuse existing accounting posting/receipt services; map payer receivable account |
| Partial settlement and reconciliation | PARTIAL | Allocation entity exists; fix payment linkage and add accounting receipt boundary |
| Returns/cancellations after claim submission | MISSING | Add claim adjustment/reversal policy without deleting original invoices |
| Insurance reports | MISSING | Add queries over persisted coverage, claims, payments, and existing invoice data |
| External provider integrations | MISSING | Add provider abstraction only; no fabricated Jordanian API implementation |
| Attachments and sensitive-data controls | PARTIAL | Preserve path-based entity; add controlled service, validation, and audit events |
| Automated insurance tests | MISSING | Add service/domain tests and regression scenarios for ordinary POS |

## Integration risks

1. The current Insurance tables have scalar IDs but few database foreign keys; incorrect IDs can be stored unless service validation is added.
2. POS currently has no insurance responsibility fields. Injecting insurance logic directly into barcode or stock allocation would risk ordinary sales, so calculation must occur before the existing checkout service and produce an immutable invoice snapshot.
3. Accounting must remain owned by the existing accounting operation and financial transaction services. Direct journal inserts would create duplicate or unreconciled postings.
4. Claim creation currently assumes an invoice coverage row already exists and uses one claim per invoice. This must be extended carefully for line-level coverage and mixed claims.
5. Claim payment allocation must persist the payment ID after the payment is saved; the current service creates allocation rows before that relationship is assigned.
6. Returns and settled claims require explicit business rules. The original invoice must remain intact; adjustments or receivable reclassification must be auditable.
7. External eligibility and submission are provider-specific and cannot be marked successful without an authorized provider response.

## Database changes required

Additive migrations are expected for administrator/payer roles, plans, eligibility checks, line-level coverage snapshots, authorization line/history data, claim review/submission metadata, and reconciliation references. Existing insurance tables and records must remain. Before applying each migration, check existing columns/tables and never use destructive `DROP` or data-reinterpretation operations.

## Accounting implications

Insurance sales need an invoice-time split between patient payment and payer receivable while preserving the existing invoice total, tax, discount, COGS, and inventory postings. Payer receipts must use the existing financial transaction/accounting workflow and allocate to claims without double posting. Rejections and deductions require explicit adjustment/write-off rules; they must not silently delete or reverse the original sale.

## Implementation priorities

1. Correct the domain/service safety issues and add missing plan, payer, eligibility, and line-level snapshot concepts.
2. Add management services and localized screens for configuration and membership workflows.
3. Integrate insurance calculation into POS through an additive checkout request/response extension.
4. Integrate claim lifecycle, batches, payment reconciliation, and accounting operations.
5. Add returns/cancellation guards, reports, provider abstraction, audit events, and automated tests.

## Explicit non-goals

- No hardcoded insurer rules, formularies, contracted prices, or external API responses.
- No replacement of the existing invoice, stock, accounting, or customer entities.
- No deletion or rewriting of existing production transactions.
- No live external provider adapter without official documentation and credentials.
