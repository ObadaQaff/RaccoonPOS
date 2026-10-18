/*
    POS search Stage 1 indexes.
    Review and execute manually against the application database.
    This script is intentionally non-destructive and safe to rerun.
*/

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_ITEMCODE' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_ITEMCODE ON dbo.Products (ITEMCODE);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_Name' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_Name ON dbo.Products (Name);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_SupplierId' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_SupplierId ON dbo.Products (SupplierId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductUnits_AlternateBarcode' AND object_id = OBJECT_ID(N'dbo.ProductUnits'))
    CREATE INDEX IX_ProductUnits_AlternateBarcode ON dbo.ProductUnits (AlternateBarcode);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductUnits_ProductId_AlternateBarcode' AND object_id = OBJECT_ID(N'dbo.ProductUnits'))
    CREATE INDEX IX_ProductUnits_ProductId_AlternateBarcode ON dbo.ProductUnits (ProductId, AlternateBarcode);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockLots_ProductId_Status_RemainingQuantity' AND object_id = OBJECT_ID(N'dbo.StockLots'))
    CREATE INDEX IX_StockLots_ProductId_Status_RemainingQuantity
        ON dbo.StockLots (ProductId, Status, RemainingQuantity);

SELECT N'POS search Stage 1 indexes verified.' AS Result;
