# Category UI QA Report

## Scope
- Screen: `CategoriesTable`
- Screen: `CreateCategory`
- Screen: `UpdateCategory`

## Test Cases

### CAT-UI-001 Create category with valid data
- Preconditions:
  - User can open `CategoriesTable`.
- Steps:
  - Click `CreateCategoryBtn`.
  - Enter valid `Name` and `Description`.
  - Click `CreateCategoryBtn` in create window.
- Expected:
  - Category is created.
  - Success message is shown.
  - No crash.
- Actual:
  - Success message exists in code.
  - No explicit error handling (`try/catch`) around create call.
- Status: `Partially Passed` (success path present, crash handling missing)

### CAT-UI-002 Create category with empty name
- Preconditions:
  - `CreateCategory` window is open.
- Steps:
  - Leave `Name` empty.
  - Enter description.
  - Click create.
- Expected:
  - Validation message shown.
  - Create is blocked.
  - No crash.
- Actual:
  - No input validation in `CreateCategory.xaml.cs` before service call.
- Status: `Failed`

### CAT-UI-003 Update category with empty required fields
- Preconditions:
  - A category row is selected in table.
  - `UpdateCategory` is opened.
- Steps:
  - Clear `CategoryName` or `CategoryDes`.
  - Click `UpdateCategoryBtn`.
- Expected:
  - Validation message shown.
  - Update blocked.
  - No crash.
- Actual:
  - Validation exists: "Please fill in all required fields."
- Status: `Passed`

### CAT-UI-004 Delete selected category
- Preconditions:
  - A category row is selected.
- Steps:
  - Open context menu -> delete.
  - Click `Yes` in confirmation.
- Expected:
  - Category is deleted.
  - Grid reloads.
  - No crash.
- Actual:
  - Delete call is not awaited in `Delete_Category`.
  - Success message appears even if delete fails asynchronously.
- Status: `Failed`

### CAT-UI-005 Open update with no selected row
- Preconditions:
  - `CategoriesTable` is open.
- Steps:
  - Ensure no row selected.
  - Click context menu update action.
- Expected:
  - User warning shown.
  - No crash.
- Actual:
  - Message exists: "No category selected."
- Status: `Passed`

### CAT-UI-006 Loading behavior on category list
- Preconditions:
  - `CategoriesTable` opens on slow DB/network.
- Steps:
  - Open `CategoriesTable`.
  - Observe UI while `Load_Categories` runs.
- Expected:
  - Loading window/indicator appears.
  - UI remains responsive.
- Actual:
  - No loading window usage in category screens.
- Status: `Failed`

### CAT-UI-007 Error-path crash resistance
- Preconditions:
  - Simulate service exception (DB unavailable).
- Steps:
  - Trigger create/update/delete operations.
- Expected:
  - Exception handled.
  - Friendly error shown.
  - No app crash.
- Actual:
  - No `try/catch` in category create/update/delete handlers.
- Status: `Failed`

## Findings
1. Missing input validation in `CreateCategory` can allow invalid request submission.
2. Missing `try/catch` in category UI operations can crash UI on service exceptions.
3. Missing loading indicator in category flows for async operations.
4. `Delete_Category` does not await `_categoryService.DeleteAsync`, creating false-success and race-condition risk.

## File References
- `RaccoonWarehouse-master/Categories/CreateCategory.xaml.cs`
- `RaccoonWarehouse-master/Categories/UpdateCategory.xaml.cs`
- `RaccoonWarehouse-master/Categories/CategoriesTable.xaml.cs`
