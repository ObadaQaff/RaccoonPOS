-- Run manually on the intended ROCCOPOS database before starting the updated app.
-- Adds nullable fields only. No historical costs, quantities, or prices are updated.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.InvoiceLine', N'U') IS NULL OR OBJECT_ID(N'dbo.StockLot', N'U') IS NULL
        THROW 51000, 'Expected InvoiceLine and StockLot tables were not found.', 1;

    IF COL_LENGTH(N'dbo.InvoiceLine', N'BonusRatio') IS NULL
        ALTER TABLE dbo.InvoiceLine ADD BonusRatio nvarchar(40) NULL;
    IF COL_LENGTH(N'dbo.InvoiceLine', N'PurchaseAcquisitionCost') IS NULL
        ALTER TABLE dbo.InvoiceLine ADD PurchaseAcquisitionCost decimal(18,6) NULL;
    IF COL_LENGTH(N'dbo.InvoiceLine', N'SourcePurchaseLineId') IS NULL
        ALTER TABLE dbo.InvoiceLine ADD SourcePurchaseLineId int NULL;
    IF COL_LENGTH(N'dbo.StockLot', N'PurchaseAcquisitionCost') IS NULL
        ALTER TABLE dbo.StockLot ADD PurchaseAcquisitionCost decimal(18,6) NULL;
    IF COL_LENGTH(N'dbo.StockLot', N'SourcePurchaseLineId') IS NULL
        ALTER TABLE dbo.StockLot ADD SourcePurchaseLineId int NULL;

    IF EXISTS (
        SELECT 1 FROM sys.columns c
        WHERE c.object_id IN (OBJECT_ID(N'dbo.InvoiceLine'), OBJECT_ID(N'dbo.StockLot'))
          AND ((c.name = N'PurchaseAcquisitionCost' AND
                (TYPE_NAME(c.system_type_id) <> N'decimal' OR c.precision <> 18 OR c.scale <> 6 OR c.is_nullable = 0))
            OR (c.name = N'SourcePurchaseLineId' AND
                (TYPE_NAME(c.system_type_id) <> N'int' OR c.is_nullable = 0))
            OR (c.object_id = OBJECT_ID(N'dbo.InvoiceLine') AND c.name = N'BonusRatio' AND
                (TYPE_NAME(c.system_type_id) <> N'nvarchar' OR c.max_length <> 80 OR c.is_nullable = 0))))
        THROW 51001, 'Existing bonus columns have incompatible definitions; no changes committed.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InvoiceLine') AND name = N'IX_InvoiceLine_SourcePurchaseLineId')
        EXEC(N'CREATE INDEX IX_InvoiceLine_SourcePurchaseLineId ON dbo.InvoiceLine(SourcePurchaseLineId);');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.StockLot') AND name = N'IX_StockLot_SourcePurchaseLineId')
        EXEC(N'CREATE INDEX IX_StockLot_SourcePurchaseLineId ON dbo.StockLot(SourcePurchaseLineId);');

    IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
        EXEC(N'IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N''20261023010000_AddPharmacyPurchaseBonus'')
            INSERT INTO dbo.__EFMigrationsHistory(MigrationId, ProductVersion)
            VALUES (N''20261023010000_AddPharmacyPurchaseBonus'', N''9.0.6'');');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
