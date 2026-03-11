# Stock In/Out UI QA Report

## Scope
- Screen: `StockIn`
- Screen: `StockOut`

## Requested Rule
- In stock out, do not display products that have no available quantity.

## What Was Tested
1. Stock out product loading only includes products with positive stock quantity.
2. Stock out unit loading only includes units that have positive stock quantity.
3. Adding stock out item blocks when requested qty > available qty.
4. Adding stock out item blocks when selected product/unit has zero available qty.
5. Existing crash-safety via `try/catch` remains for save/load/add paths.

## Expected vs Actual
- Expected: products with zero quantity are hidden in stock out selection.
- Actual: fixed by filtering stock load with `Quantity > 0` and distinct product projection.
- Expected: qty should not exceed current stock.
- Actual: fixed with runtime availability check before add.

## Status
- `Pass` for implemented rules in code review and build/test verification.

## Notes
- `StockIn` keeps full product list behavior (intended for incoming stock).
- `StockOut` now enforces availability at both display and add validation levels.
