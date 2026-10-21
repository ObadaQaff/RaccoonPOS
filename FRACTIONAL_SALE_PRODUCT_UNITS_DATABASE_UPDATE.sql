/*
    Fractional sale configuration for product units
    SQL Server - safe to run more than once

    This script is intentionally not executed by the application agent.
    Back up the database before applying it manually.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.ProductUnit', N'U') IS NULL
        THROW 51000, 'Required table dbo.ProductUnit was not found. Verify the database/schema before continuing.', 1;

    IF COL_LENGTH(N'dbo.ProductUnit', N'AllowFractionalSale') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductUnit
            ADD AllowFractionalSale bit NOT NULL
                CONSTRAINT DF_ProductUnit_AllowFractionalSale DEFAULT (0);
    END;

    IF COL_LENGTH(N'dbo.ProductUnit', N'FractionalPartsPerUnit') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductUnit
            ADD FractionalPartsPerUnit decimal(18, 6) NOT NULL
                CONSTRAINT DF_ProductUnit_FractionalPartsPerUnit DEFAULT (0);
    END;

    IF COL_LENGTH(N'dbo.ProductUnit', N'FractionalPartName') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductUnit
            ADD FractionalPartName nvarchar(50) NULL;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

/* Validation */
SELECT
    c.name AS ColumnName,
    t.name AS SqlType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    dc.name AS DefaultConstraint
FROM sys.columns AS c
INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints AS dc ON dc.object_id = c.default_object_id
WHERE c.object_id = OBJECT_ID(N'dbo.ProductUnit')
  AND c.name IN (N'AllowFractionalSale', N'FractionalPartsPerUnit', N'FractionalPartName')
ORDER BY c.column_id;

/* Expected result:
   AllowFractionalSale      bit           NOT NULL, default 0
   FractionalPartsPerUnit   decimal(18,6) NOT NULL, default 0
   FractionalPartName       nvarchar(50)  NULL
*/
