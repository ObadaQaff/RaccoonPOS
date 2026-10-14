# Insurance Implementation Report

Date: 2026-10-13

## Preserved existing functionality

The existing company-management service/window, Insurance Dashboard module and permissions, insurance tables, claim creation/listing, payment allocation service, ordinary POS invoices, stock allocation, accounting-operation queue, and existing migrations were preserved. No production records were deleted or rewritten.

## Implemented in this enhancement

- Added insurer/administrator/contractual-payer organization roles to the existing insurance-company record.
- Added insurance plans linked to contracts and optional contract versions.
- Added eligibility verification history with manual, portal, API, file-import, and card-reader method values. Non-manual eligible results require an external reference.
- Added plan-scoped coverage and exclusion support.
- Added invoice coverage line snapshots and authorization line records for future POS persistence.
- Added a rule-based coverage evaluator for product/category/service rules, exclusions, percentage coverage, copayment, deductibles, per-claim limits, approved quantity, and prior-authorization requirements.
- Added claim decision validation for approved, partially approved, and rejected claims.
- Prevented review of paid, closed, or cancelled claims.
- Fixed claim payment allocation persistence so allocations receive the saved payment ID.
- Added a localized Insurance Claims window for listing and recording claim decisions.
- Added a company role selector for insurer, administrator, payer, or combined role.
- Added contract and member-policy CRUD service operations with ownership/date/duplicate validation.
- Added controlled claim-batch creation and persisted receivable-aging queries.
- Added opt-in invoice insurance billing fields and `InsuranceReceivable` payment type mapped to the existing Accounts Receivable accounting path.
- Added an `IInsuranceProvider` abstraction and a safe manual provider that never fabricates external eligibility or claim-submission success.
- Added migrations `20261013132624_AddInsuranceEnhancements` and `20261013133107_AddInsurancePlanCoverageScope`.

## Not yet enabled

POS insurance checkout UI, contract/rule CRUD screens, member-policy CRUD screen, settlement receipts through the existing financial transaction workflow, returns/cancellation integration, claim export, full insurance reports, attachments workflow, and live external-provider integrations still require additional implementation and business-approved accounting rules. The application service and persistence seams for contracts, policies, batches, aging, coverage calculation, and insurer receivables are now present.

## Compatibility decisions

- Ordinary invoices do not enter insurance calculation unless an insurance workflow explicitly supplies a contract version and coverage inputs.
- External providers are not fabricated; only method/reference storage is provided.
- Existing invoice, stock, and accounting posting paths remain the source of truth.
