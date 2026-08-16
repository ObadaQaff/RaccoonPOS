SET XACT_ABORT ON;

DECLARE @CountsBefore TABLE (TableName sysname NOT NULL, RowCountBefore bigint NOT NULL);
DECLARE @CountsAfter TABLE (TableName sysname NOT NULL, RowCountAfter bigint NOT NULL);

INSERT INTO @CountsBefore (TableName, RowCountBefore)
SELECT t.name, SUM(p.rows)
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE t.name IN (
    'AccountOpeningBalances',
    'AccountingPeriods',
    'Branches',
    'Brand',
    'CashierSession',
    'Category',
    'Check',
    'CostCenters',
    'Currencies',
    'Delegate',
    'Employee',
    'FinancialTransaction',
    'FiscalYears',
    'Invoice',
    'InvoiceLine',
    'JournalEntries',
    'JournalEntryLines',
    'Product',
    'ProductUnit',
    'Stock',
    'StockAdjustment',
    'StockDocument',
    'StockItem',
    'StockLot',
    'StockTransaction',
    'SubCategory',
    'SubCategoryBrand',
    'Tax',
    'Unit',
    'Voucher',
    'Warehouse'
)
GROUP BY t.name;

DECLARE @DeleteOrder TABLE (SortOrder int NOT NULL, TableName sysname NOT NULL);

INSERT INTO @DeleteOrder (SortOrder, TableName)
VALUES
    (10, 'StockAdjustment'),
    (20, 'StockTransaction'),
    (30, 'StockItem'),
    (40, 'StockLot'),
    (50, 'Stock'),
    (60, 'Check'),
    (70, 'FinancialTransaction'),
    (80, 'InvoiceLine'),
    (90, 'Voucher'),
    (100, 'Invoice'),
    (110, 'StockDocument'),
    (120, 'CashierSession'),
    (130, 'JournalEntryLines'),
    (140, 'JournalEntries'),
    (150, 'AccountOpeningBalances'),
    (160, 'AccountingPeriods'),
    (170, 'FiscalYears'),
    (180, 'Tax'),
    (190, 'ProductUnit'),
    (200, 'Product'),
    (210, 'SubCategoryBrand'),
    (220, 'Brand'),
    (230, 'SubCategory'),
    (240, 'Category'),
    (250, 'Unit'),
    (260, 'Warehouse'),
    (270, 'Delegate'),
    (280, 'Employee'),
    (290, 'Currencies'),
    (300, 'Branches'),
    (310, 'CostCenters');

BEGIN TRANSACTION;

DECLARE @TableName sysname;
DECLARE @Sql nvarchar(max);

DECLARE delete_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT d.TableName
FROM @DeleteOrder d
JOIN sys.tables t ON t.name = d.TableName
ORDER BY d.SortOrder;

OPEN delete_cursor;
FETCH NEXT FROM delete_cursor INTO @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = N'DELETE FROM ' + QUOTENAME(@TableName) + N';';
    EXEC sys.sp_executesql @Sql;
    FETCH NEXT FROM delete_cursor INTO @TableName;
END;

CLOSE delete_cursor;
DEALLOCATE delete_cursor;

COMMIT TRANSACTION;

INSERT INTO @CountsAfter (TableName, RowCountAfter)
SELECT t.name, SUM(p.rows)
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE t.name IN (
    'AccountOpeningBalances',
    'AccountingPeriods',
    'Branches',
    'Brand',
    'CashierSession',
    'Category',
    'Check',
    'CostCenters',
    'Currencies',
    'Delegate',
    'Employee',
    'FinancialTransaction',
    'FiscalYears',
    'Invoice',
    'InvoiceLine',
    'JournalEntries',
    'JournalEntryLines',
    'Product',
    'ProductUnit',
    'Stock',
    'StockAdjustment',
    'StockDocument',
    'StockItem',
    'StockLot',
    'StockTransaction',
    'SubCategory',
    'SubCategoryBrand',
    'Tax',
    'Unit',
    'Voucher',
    'Warehouse'
)
GROUP BY t.name;

SELECT
    COALESCE(b.TableName, a.TableName) AS TableName,
    ISNULL(b.RowCountBefore, 0) AS RowCountBefore,
    ISNULL(a.RowCountAfter, 0) AS RowCountAfter
FROM @CountsBefore b
FULL JOIN @CountsAfter a ON a.TableName = b.TableName
ORDER BY TableName;

SELECT 'Accounts' AS PreservedTable, COUNT_BIG(*) AS [RowCount] FROM [Accounts]
UNION ALL
SELECT 'User', COUNT_BIG(*) FROM [User]
UNION ALL
SELECT 'PermissionDefinitions', COUNT_BIG(*) FROM [PermissionDefinitions]
UNION ALL
SELECT 'ReportPermissions', COUNT_BIG(*) FROM [ReportPermissions]
UNION ALL
SELECT 'RolePermissions', COUNT_BIG(*) FROM [RolePermissions]
UNION ALL
SELECT 'AppSettings', COUNT_BIG(*) FROM [AppSettings];
