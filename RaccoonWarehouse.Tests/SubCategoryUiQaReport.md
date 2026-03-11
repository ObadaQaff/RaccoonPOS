# SubCategory UI QA Report

## Scope
- Screen: `SubCategoryTable`
- Screen: `CreateSubCategory`
- Screen: `UpdateSubCategory`

## Rules Applied
- Non-nullable fields are required in UI validation:
  - `Name`
  - `ParentCategoryId`
- Nullable fields are optional and allowed to be null/empty:
  - `Description`
  - `ImageUrl`

## Covered Cases
1. Create with required fields only and nullable fields empty.
2. Update with nullable fields cleared to empty/null.
3. Delete selected subcategory with confirmation and awaited operation.
4. No-selection update/delete message behavior.
5. Crash protection via `try/catch` around create/update/delete/load.
6. Loading indicator shown/hidden around async operations.

## Expected Outcome
- Required-field validation blocks invalid submits.
- Nullable fields can be left empty without UI rejection.
- UI should not crash on service exceptions.
- Delete should only show success if service returns success.
