# Product + Units UI QA Report

## Scope
- Screen: `ProductsTable`
- Screen: `CreateProduct`
- Screen: `UpdateProduct`

## Relation Focus
- Product creation and update with related `ProductUnit` rows.
- Unit sync behavior: add/update/remove units for a product.
- Unit flag normalization:
  - `IsBaseUnit`
  - `IsDefaultSaleUnit`
  - `IsDefaultPurchaseUnit`

## Covered Automated Logic
1. Product update with units:
  - updates product fields
  - updates existing unit
  - removes missing unit
  - adds new unit
2. Validation fails for duplicate unit IDs in same product.
3. Product-not-found update fails gracefully.
4. Tax application recalculates sale price from untaxed price.

## Expected UI Checks (manual follow-up)
1. Create product with one unit auto-sets all default flags.
2. Add same unit twice is blocked.
3. Removing a unit updates the units panel and saves correctly.
4. Empty/invalid numeric unit fields show validation.
5. Save/update errors should not crash window and should show message.
