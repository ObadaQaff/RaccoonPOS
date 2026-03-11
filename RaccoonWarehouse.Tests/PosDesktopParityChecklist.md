# POS Desktop Parity Checklist

## Date
- 2026-03-09

## Compared Screens
- `Invoices/POS`
- `Invoices/CreateSalesInvoice`

## Checklist
1. Cashier session required before invoice actions
- Status: `PASS`
- Evidence: `TryGetActiveCashierSession` is used before save/payment and POS load.

2. Add products with unit pricing and tax snapshot
- Status: `PASS`
- Evidence: POS applies default sale unit, captures tax fields, recalculates totals.

3. Save invoice + post stock movements
- Status: `PASS`
- Evidence: POS `ProcessPaymentAsync` saves invoice and posts stock movements.

4. Save invoice + post financial transaction
- Status: `PASS`
- Evidence: POS posts financial transactions for non-credit payments.

5. Credit invoice behavior (no immediate financial post)
- Status: `PASS`
- Evidence: POS skips financial posting when payment is `Credit`.

6. Payment method parity with desktop sales invoice
- Status: `PASS (Fixed)`
- Evidence: POS now supports `Cash`, `Visa`, `Master`, `Debit`, `Check`, `MobilePayment`, `Credit`.

7. Prevent crashes on invalid/empty UI input
- Status: `PASS`
- Evidence: null checks and validation guards for barcode, line fields, and stock checks.

8. Loading indicator around async save/cancel/hold flows
- Status: `PASS`
- Evidence: loading show/hide in hold, payment, cancel handlers.

9. Hold and resume invoice flow
- Status: `PASS`
- Evidence: hold updates existing held invoice when available; resume loads selected held invoice.

## Open Gap
- POS still uses dedicated payment buttons while desktop invoice uses a payment combobox.
- This is a UX difference only; payment logic parity is now aligned.
