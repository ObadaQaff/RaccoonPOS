# Authentication And Permissions

Last verified: 2026-06-13

## Purpose

This area authenticates users, stores the current user/cashier session, applies
role permission overrides, filters report access, and controls optional employee,
delegate, and accounting modules.

## Authentication

Startup requires login before the dashboard. Login accepts an exact phone
number or name and compares the supplied password to stored data.

Roles currently include:

- `Admin`
- `Casher`
- `Customer`
- `Supplier`
- `Manager`
- `HR`

Successful login appears to require or open a cashier session for every role.
This behavior is **Needs verification** against intended business policy.

## Permission Model

`PermissionCatalog` defines module/resource/action keys. `RolePermission`
records override access by role. Missing overrides currently default to allowed.
Only an active administrator can save permission changes.

Legacy report permissions are mapped into unified role permissions. Catalog-based
report navigation filters visible reports and rechecks access before opening.

## Optional Modules

Database settings include:

- `EnableEmployeeSystem`
- `EnableDelegateSystem`
- `EnableAccountingSystem`

Employee and delegate entities can optionally link to user records. Delegates
can be assigned to invoices. The current Employees table primarily manages
staff user accounts; separate employee entity CRUD exists but active navigation
to it was not verified.

## Services And Models

- `AuthService`
- `IUserSession` / `UserSession`
- `PermissionService`
- `ReportPermissionService`
- `EmployeeService`
- `DelegateService`
- Feature-setting services

Principal models include `User`, `PermissionDefinition`, `RolePermission`,
`ReportPermission`, `Employee`, and `Delegate`.

## Verification

- `RaccoonWarehouse.Tests/PermissionServiceTests.cs`
- `RaccoonWarehouse.Tests/EmployeeServiceCrudTests.cs`
- `RaccoonWarehouse.Tests/DelegateServiceCrudTests.cs`
- `RaccoonWarehouse.Tests/QA_Testing_Summary.md`

No dedicated authentication test suite was found.

## Known Risks

- Passwords are compared and stored as plaintext.
- Password data is returned in read DTOs and retained in session state.
- No verified password hashing, lockout, or rate limiting exists.
- Missing permission overrides default to allowed.
- Permission definitions exceed actual enforcement coverage.
- Delegate CRUD lacks verified permission checks.
- Employee UI uses user-oriented permission keys in some paths.
- Feature-setting dialogs may be opened without their own authorization check.
- Report export permissions are defined but export-time enforcement was not found.
- Some accounting report routes may bypass the report catalog.

## Related Paths

- `RaccoonWarehouse-master/Auth/`
- `RaccoonWarehouse-master/Settings/ReportPermissionsManager.*`
- `RaccoonWarehouse-master/Navigation/Modules/`
- `RaccoonWarehouse.Application/Service/AuthService/`
- `RaccoonWarehouse.Application/Service/Permissions/`
- `RaccoonWarehouse.Application/Service/Employees/`
- `RaccoonWarehouse.Application/Service/Delegates/`
- `RaccoonWarehouse.Application/Service/Settings/`
- `RaccoonWarehouse.Domain/Permissions/`
- `RaccoonWarehouse.Domain/Users/`
- `RaccoonWarehouse.Domain/Employees/`
- `RaccoonWarehouse.Domain/Delegates/`
