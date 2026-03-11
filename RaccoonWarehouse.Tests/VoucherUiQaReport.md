# Voucher UI QA Report

## Scope
- Screen: `CreateVoucher` (Receipt Voucher)
- Screen: `PaymentVoucher` (Payment Voucher)
- Screen: `SearchVoucherWindow`

## Rules Applied
- `try/catch` added around risky UI flows.
- Program crash prevention added for invalid numeric parsing and null selections.
- Loading window shown/hidden around async load/save/search paths.
- Incorrect values and data types validated (especially payment by check).
- Nullable-field rule respected:
  - nullable DTO fields (`VoucherNumber`, `Notes`, `CustomerId`, `SupplierId`, `CasherId`) can remain empty.

## Test Cases

### TC-01 Create payment voucher by check (valid)
- Preconditions: cashier session opened, payment type = `Check`.
- Steps:
  1. Enter amount.
  2. Add one or more checks with valid number and positive amount.
  3. Save voucher.
- Expected result: voucher saved, financial posting succeeds, success message shown.
- Actual result: passed after fixes.
- Status: `Pass`

### TC-02 Prevent crash on invalid check amount input
- Preconditions: payment type = `Check`.
- Steps:
  1. Enter non-numeric text in check amount.
  2. Click add check.
- Expected result: validation warning; no crash.
- Actual result: passed after replacing unsafe parse with `decimal.TryParse` + `try/catch`.
- Status: `Pass`

### TC-03 Reject duplicate check numbers in same voucher
- Preconditions: payment type = `Check`.
- Steps:
  1. Add a check with number `CHK-1`.
  2. Try to add another check with same number.
- Expected result: duplicate warning and second row blocked.
- Actual result: passed.
- Status: `Pass`

### TC-04 Enforce check total equals voucher amount
- Preconditions: payment type = `Check`.
- Steps:
  1. Enter voucher amount.
  2. Add checks with total not equal to voucher amount.
  3. Save voucher.
- Expected result: save blocked with warning.
- Actual result: passed.
- Status: `Pass`

### TC-05 Search vouchers safely with date validation
- Preconditions: open search window.
- Steps:
  1. Enter date range with `From > To`.
  2. Run search.
- Expected result: warning shown and no crash.
- Actual result: passed.
- Status: `Pass`

## Key Findings Fixed
- Fixed unsafe parsing in check entry that could crash on incorrect data type.
- Added robust payment-by-check validation (required checks, positive amounts, duplicate check prevention, total match).
- Added loading indicator coverage for voucher load/save/search flows.
- Added defensive null checks for selected voucher and delete-check context.
- Removed recursive load-event pattern in `CreateVoucher` and switched to single load subscription.

## Automated Test Reference
- `VoucherServiceCrudTests.cs`
