/*
    MANUAL-ONLY SQL SCRIPT: Permission category/subcategory migration
    Provider: SQL Server
    Additive and idempotent. The application never executes this script.

    Legacy permission keys are retained. Existing group access is copied to
    the new parent and child keys so current users do not lose access.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Map TABLE
(
    LegacyKey NVARCHAR(100) NOT NULL,
    ParentKey NVARCHAR(100) NOT NULL,
    ChildKey NVARCHAR(100) NOT NULL
);

INSERT INTO @Map(LegacyKey, ParentKey, ChildKey)
VALUES
    (N'POS', N'Sales', N'Sales.PointOfSale'),
    (N'Products', N'Inventory', N'Inventory.Products'),
    (N'Inventory', N'Inventory', N'Inventory.Products'),
    (N'Inventory', N'Inventory', N'Inventory.StockOperations'),
    (N'Inventory', N'Inventory', N'Inventory.StockReports'),
    (N'Inventory', N'Inventory', N'Inventory.Warehouses'),
    (N'Inventory', N'Inventory', N'Inventory.Categories'),
    (N'Inventory', N'Inventory', N'Inventory.StockAdjustments'),
    (N'Purchasing', N'Purchasing', N'Purchasing.PurchaseInvoices'),
    (N'Purchasing', N'Purchasing', N'Purchasing.PurchaseOrders'),
    (N'Purchasing', N'Purchasing', N'Purchasing.PurchaseRequests'),
    (N'Purchasing', N'Purchasing', N'Purchasing.Suppliers'),
    (N'Purchasing', N'Purchasing', N'Purchasing.PurchaseReports'),
    (N'Suppliers', N'Purchasing', N'Purchasing.Suppliers'),
    (N'Accounting', N'Accounting', N'Accounting.ChartOfAccounts'),
    (N'Accounting', N'Accounting', N'Accounting.FinancialTransactions'),
    (N'Accounting', N'Accounting', N'Accounting.Receipts'),
    (N'Accounting', N'Accounting', N'Accounting.Payments'),
    (N'Accounting', N'Accounting', N'Accounting.FinancialReports'),
    (N'Customers', N'Customers', N'Customers.CustomerProfiles'),
    (N'Customers', N'Customers', N'Customers.CustomerRequests'),
    (N'Customers', N'Customers', N'Customers.LostCustomers'),
    (N'Employees', N'Employees', N'Employees.EmployeeProfiles'),
    (N'Employees', N'Employees', N'Employees.ShiftManagement'),
    (N'Shifts', N'Employees', N'Employees.ShiftManagement'),
    (N'HR', N'HR', N'HR.Attendance'),
    (N'HR', N'HR', N'HR.Leaves'),
    (N'HR', N'HR', N'HR.Payroll'),
    (N'HR', N'HR', N'HR.HRReports'),
    (N'Users', N'Users', N'Users.UserManagement'),
    (N'Settings', N'Settings', N'Settings.ModuleManagement'),
    (N'Settings', N'Settings', N'Settings.SystemConfiguration'),
    (N'Settings', N'Settings', N'Settings.AuditLogs'),
    (N'Permissions', N'Settings', N'Settings.PermissionManagement');

/* The old central Reports permission is copied to all report children. */
INSERT INTO @Map(LegacyKey, ParentKey, ChildKey)
VALUES
    (N'Reports', N'Sales', N'Sales.SalesReports'),
    (N'Reports', N'Inventory', N'Inventory.StockReports'),
    (N'Reports', N'Purchasing', N'Purchasing.PurchaseReports'),
    (N'Reports', N'Accounting', N'Accounting.FinancialReports'),
    (N'Reports', N'HR', N'HR.HRReports');

/* Create parent category rows from legacy permissions. */
INSERT INTO dbo.PermissionGroupPermissions
(
    PermissionGroupId, AreaKey, [Level], CreatedDate, UpdatedDate
)
SELECT
    p.PermissionGroupId,
    m.ParentKey,
    MAX(p.[Level]),
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM dbo.PermissionGroupPermissions p
INNER JOIN @Map m ON m.LegacyKey = p.AreaKey
GROUP BY p.PermissionGroupId, m.ParentKey
HAVING NOT EXISTS
(
    SELECT 1
    FROM dbo.PermissionGroupPermissions existing
    WHERE existing.PermissionGroupId = p.PermissionGroupId
      AND existing.AreaKey = m.ParentKey
);

/* Copy each legacy permission to its mapped child. */
INSERT INTO dbo.PermissionGroupPermissions
(
    PermissionGroupId, AreaKey, [Level], CreatedDate, UpdatedDate
)
SELECT
    p.PermissionGroupId,
    m.ChildKey,
    MAX(p.[Level]),
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM dbo.PermissionGroupPermissions p
INNER JOIN @Map m ON m.LegacyKey = p.AreaKey
GROUP BY p.PermissionGroupId, m.ChildKey
HAVING NOT EXISTS
(
    SELECT 1
    FROM dbo.PermissionGroupPermissions existing
    WHERE existing.PermissionGroupId = p.PermissionGroupId
      AND existing.AreaKey = m.ChildKey
);

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PermissionGroupPermissions_AreaLevel'
      AND object_id = OBJECT_ID(N'dbo.PermissionGroupPermissions')
)
BEGIN
    CREATE INDEX IX_PermissionGroupPermissions_AreaLevel
        ON dbo.PermissionGroupPermissions(AreaKey, [Level], PermissionGroupId);
END;

COMMIT TRANSACTION;

/* Review the resulting category/subcategory assignments. */
SELECT PermissionGroupId, AreaKey, [Level]
FROM dbo.PermissionGroupPermissions
WHERE AreaKey LIKE N'%.%'
ORDER BY PermissionGroupId, AreaKey;
