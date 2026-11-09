/*
    MANUAL-ONLY DATABASE CLEANUP
    =============================
    Review, back up, and archive legacy HR data before executing this script.
    This script intentionally does not run automatically and does not touch:
      - dbo.[User] or user accounts/types
      - accounting, journal, invoice, POS, cashier-session, inventory, or audit data
      - EF migration history

    Posted payroll-related accounting entries are preserved because this script
    removes only the legacy HR operational tables and the HR feature setting.
    Confirm that no external application still needs the HR tables before use.
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* Stop rather than remove relationships owned by unrelated tables. Resolve
   those references manually after reviewing/archive planning. */
IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE referenced_object_id IN
    (
        OBJECT_ID(N'dbo.Employee'),
        OBJECT_ID(N'dbo.EmployeeEmploymentHistory'),
        OBJECT_ID(N'dbo.EmployeeSalaryVersion'),
        OBJECT_ID(N'dbo.EmployeePayroll'),
        OBJECT_ID(N'dbo.PayrollAdjustment'),
        OBJECT_ID(N'dbo.PayrollCorrection'),
        OBJECT_ID(N'dbo.PayrollPolicy')
    )
      AND parent_object_id NOT IN
    (
        OBJECT_ID(N'dbo.Employee'),
        OBJECT_ID(N'dbo.EmployeeEmploymentHistory'),
        OBJECT_ID(N'dbo.EmployeeSalaryVersion'),
        OBJECT_ID(N'dbo.EmployeePayroll'),
        OBJECT_ID(N'dbo.PayrollAdjustment'),
        OBJECT_ID(N'dbo.PayrollCorrection'),
        OBJECT_ID(N'dbo.PayrollPolicy')
    )
)
BEGIN
    ROLLBACK TRANSACTION;
    THROW 51000, 'Legacy Employee is referenced by a non-HR table. Review that relationship before cleanup.', 1;
END;

/* Remove foreign keys owned by legacy HR tables, including self-references and
   the Employee -> User link. Do not remove foreign keys owned by other tables. */
DECLARE @sql nvarchar(max) = N'';

SELECT @sql = @sql +
    N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id)) +
    N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' + CHAR(13) + CHAR(10)
FROM sys.foreign_keys
WHERE parent_object_id IN
(
    OBJECT_ID(N'dbo.Employee'),
    OBJECT_ID(N'dbo.EmployeeEmploymentHistory'),
    OBJECT_ID(N'dbo.EmployeeSalaryVersion'),
    OBJECT_ID(N'dbo.EmployeePayroll'),
    OBJECT_ID(N'dbo.PayrollAdjustment'),
    OBJECT_ID(N'dbo.PayrollCorrection'),
    OBJECT_ID(N'dbo.PayrollPolicy')
)
;

IF @sql <> N'' EXEC sys.sp_executesql @sql;

/* Drop only tables that belong exclusively to the removed HR/payroll module. */
DROP TABLE IF EXISTS dbo.PayrollCorrection;
DROP TABLE IF EXISTS dbo.PayrollAdjustment;
DROP TABLE IF EXISTS dbo.EmployeePayroll;
DROP TABLE IF EXISTS dbo.EmployeeSalaryVersion;
DROP TABLE IF EXISTS dbo.EmployeeEmploymentHistory;
DROP TABLE IF EXISTS dbo.PayrollPolicy;
DROP TABLE IF EXISTS dbo.Employee;

/* Remove the old feature toggle without affecting other application settings. */
IF OBJECT_ID(N'dbo.AppSettings', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppSettings', N'Key') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppSettings', N'Value') IS NOT NULL
BEGIN
    DELETE FROM dbo.AppSettings
    WHERE [Key] = N'EnableEmployeeSystem';
END;

COMMIT TRANSACTION;
