/*
    Manual cleanup for the retired permissions system.
    Review the target database first. This script is intentionally not executed by the application.
    It removes only the legacy permission tables and the permission metadata column from AuditLogs.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @sql nvarchar(max) = N'';

/* Drop foreign keys owned by the legacy permission tables before dropping them. */
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(parent_object.schema_id)) + N'.' + QUOTENAME(parent_object.name)
    + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(13) + CHAR(10)
FROM sys.foreign_keys fk
JOIN sys.tables parent_object ON parent_object.object_id = fk.parent_object_id
JOIN sys.tables referenced_object ON referenced_object.object_id = fk.referenced_object_id
WHERE referenced_object.name IN (N'PermissionDefinitions', N'ReportPermissions', N'RolePermissions', N'UserPermissions')
   OR parent_object.name IN (N'PermissionDefinitions', N'ReportPermissions', N'RolePermissions', N'UserPermissions');

IF @sql <> N''
    EXEC sys.sp_executesql @sql;

IF OBJECT_ID(N'dbo.UserPermissions', N'U') IS NOT NULL DROP TABLE dbo.UserPermissions;
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NOT NULL DROP TABLE dbo.RolePermissions;
IF OBJECT_ID(N'dbo.ReportPermissions', N'U') IS NOT NULL DROP TABLE dbo.ReportPermissions;
IF OBJECT_ID(N'dbo.PermissionDefinitions', N'U') IS NOT NULL DROP TABLE dbo.PermissionDefinitions;

IF COL_LENGTH(N'dbo.AuditLogs', N'PermissionKey') IS NOT NULL
    ALTER TABLE dbo.AuditLogs DROP COLUMN PermissionKey;

COMMIT TRANSACTION;
