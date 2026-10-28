SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.CustomerRequests', N'CustomerPhone') IS NULL
    ALTER TABLE [dbo].[CustomerRequests] ADD [CustomerPhone] nvarchar(50) NULL;

UPDATE cr
SET CustomerPhone = u.PhoneNumber
FROM dbo.CustomerRequests cr
INNER JOIN dbo.Users u ON u.Id = cr.CustomerId
WHERE cr.CustomerPhone IS NULL;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20261028160000_AddCustomerRequestPhone'
)
    INSERT INTO dbo.__EFMigrationsHistory(MigrationId, ProductVersion)
    VALUES (N'20261028160000_AddCustomerRequestPhone', N'9.0.6');

COMMIT TRANSACTION;
