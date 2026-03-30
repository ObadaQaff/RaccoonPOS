# POS Keyboard Flow QA Report

## Date
- 2026-03-24

## Scope
- Keyboard-only cashier flow validation for `Invoices/POS`

## Preconditions
- User is logged in.
- Cashier session is open.
- At least 3 saleable products exist with stock.
- At least 1 customer exists.
- Printer/PDF save flow is available for print verification.

## Test Cases

### 1. Customer Selection By Keyboard
- Preconditions:
  - POS screen is open.
- Test steps:
  1. Move focus to customer field using keyboard.
  2. Type part of a customer name.
  3. Use arrow keys if needed.
  4. Press `Enter`.
- Expected result:
  - Customer list opens.
  - Customer is selected.
  - Focus returns to barcode box.
- Actual result:
  - 
- Pass/Fail:
  - 

### 2. Add Product By Barcode
- Preconditions:
  - POS screen is open.
  - Focus is on barcode input.
- Test steps:
  1. Type or scan a valid barcode.
  2. Press `Enter`.
- Expected result:
  - Product is added to invoice.
  - Invoice totals update.
  - Grid enters editable flow without crash.
- Actual result:
  - 
- Pass/Fail:
  - 

### 3. Product Search Popup By Keyboard
- Preconditions:
  - POS grid is editable.
- Test steps:
  1. Open product editing cell.
  2. Type part of product name.
  3. Confirm first suggestion or use arrows.
  4. Press `Enter`.
- Expected result:
  - Suggestions appear.
  - First valid suggestion is highlighted.
  - Product is selected.
  - Focus moves to quantity cell.
- Actual result:
  - 
- Pass/Fail:
  - 

### 4. Quantity Update By Keyboard
- Preconditions:
  - At least one line exists in invoice.
- Test steps:
  1. Move to quantity cell with keyboard.
  2. Enter a valid quantity.
  3. Press `Enter`.
- Expected result:
  - Quantity is saved.
  - Totals recalculate.
  - No invalid focus jump occurs.
- Actual result:
  - 
- Pass/Fail:
  - 

### 5. Payment Shortcuts
- Preconditions:
  - Invoice has at least one valid line.
- Test steps:
  1. Try `Ctrl+1`.
  2. Try `Ctrl+2`.
  3. Try `Ctrl+3`.
  4. Try `Ctrl+4`.
  5. Try `Ctrl+5`.
  6. Try `Ctrl+6`.
  7. Try `Ctrl+7`.
- Expected result:
  - Each shortcut maps to the expected payment type.
  - Save path completes or shows a clear validation message.
  - No crash occurs.
- Actual result:
  - 
- Pass/Fail:
  - 

### 6. POS Action Shortcuts
- Preconditions:
  - POS screen is open.
- Test steps:
  1. Press `F1`.
  2. Press `F2`.
  3. Press `F3`.
  4. Press `F4`.
  5. Press `F5`.
  6. Press `F6`.
  7. Press `F7`.
  8. Press `F8`.
  9. Press `F9`.
  10. Press `F10`.
  11. Press `F11`.
  12. Press `F12`.
  13. Press `Esc`.
- Expected result:
  - Each shortcut triggers the intended action or a clear safe message.
  - No dead shortcut exists.
  - No crash occurs.
- Actual result:
  - 
- Pass/Fail:
  - 

### 7. Hold And Resume With Keyboard
- Preconditions:
  - Invoice contains valid lines.
- Test steps:
  1. Press `F5` to hold.
  2. Reopen or remain in POS.
  3. Press `F6` to resume.
  4. Choose held invoice by keyboard if supported.
- Expected result:
  - Hold succeeds.
  - Resume loads invoice.
  - Focus returns to barcode after the flow.
- Actual result:
  - 
- Pass/Fail:
  - 

### 8. Receipt And Payment Windows
- Preconditions:
  - Cashier session is open.
- Test steps:
  1. Press `F7` and complete or cancel receipt window.
  2. Press `F8` and complete or cancel payment window.
- Expected result:
  - Both windows open.
  - Save/cancel works safely.
  - Focus returns to barcode after closing.
- Actual result:
  - 
- Pass/Fail:
  - 

### 9. Print And Daily Report By Keyboard
- Preconditions:
  - A real invoice was saved for print.
- Test steps:
  1. Press `F9`.
  2. Press `F10`.
- Expected result:
  - Print opens save/print flow safely.
  - Daily report opens safely.
  - Focus returns to barcode when appropriate.
- Actual result:
  - 
- Pass/Fail:
  - 

### 10. Focus Recovery Regression
- Preconditions:
  - Run multiple invoice actions in sequence.
- Test steps:
  1. Select customer.
  2. Add product.
  3. Edit quantity.
  4. Hold or cancel.
  5. Resume or start new invoice.
  6. Complete payment.
- Expected result:
  - Focus consistently returns to the correct next field.
  - Barcode remains the default cashier input after modal actions.
  - No mouse is required for the main flow.
- Actual result:
  - 
- Pass/Fail:
  - 

## Key Findings
- Pending manual execution.

## Remaining Risks
- Modal child windows may still require partial mouse interaction depending on their own internal keyboard support.
- Customer dropdown filtering behavior should be validated with Arabic and numeric input.
- Product suggestion behavior should be tested with large datasets and similar names/barcodes.
