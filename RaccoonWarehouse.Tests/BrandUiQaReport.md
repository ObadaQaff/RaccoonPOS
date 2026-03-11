# Brand UI QA Report

## Scope
- Screen: `BrandsTable`
- Screen: `CreateBrand`
- Screen: `UpdateBrand`

## Rules Applied
- Non-nullable required field:
  - `Name`
- Nullable optional field:
  - `ImageUrl`

## Covered Cases
1. Create brand with valid name and empty image URL.
2. Validation blocks create/update when name is empty.
3. Update brand while clearing image URL (allowed).
4. Delete selected brand with awaited service call.
5. No-selection update/delete protection.
6. `try/catch` crash safety in load/create/update/delete.
7. Loading indicator shown/hidden around async operations.

## Expected Outcome
- Required name validation enforced.
- Nullable image URL accepted as null/empty.
- Delete success shown only when operation succeeds.
- UI remains stable on service exceptions.
