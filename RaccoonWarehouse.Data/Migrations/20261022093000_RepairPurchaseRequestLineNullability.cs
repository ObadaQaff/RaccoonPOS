using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations;

/// <summary>
/// Repairs databases created from an older/manual PurchaseRequestLines schema.
/// Free-text purchase-request lines do not have catalog product references.
/// </summary>
[Migration("20261022093000_RepairPurchaseRequestLineNullability")]
public partial class RepairPurchaseRequestLineNullability : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.PurchaseRequestLines', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PurchaseRequestLines')
          AND name = N'IX_PurchaseRequestLines_PurchaseRequestId_ProductId_ProductUnitId'
    )
    BEGIN
        DROP INDEX [IX_PurchaseRequestLines_PurchaseRequestId_ProductId_ProductUnitId]
            ON [dbo].[PurchaseRequestLines];
    END;

    IF COL_LENGTH(N'dbo.PurchaseRequestLines', N'ProductId') IS NOT NULL
        ALTER TABLE [dbo].[PurchaseRequestLines]
        ALTER COLUMN [ProductId] int NULL;

    IF COL_LENGTH(N'dbo.PurchaseRequestLines', N'ProductUnitId') IS NOT NULL
        ALTER TABLE [dbo].[PurchaseRequestLines]
        ALTER COLUMN [ProductUnitId] int NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PurchaseRequestLines')
          AND name = N'IX_PurchaseRequestLines_PurchaseRequestId_ProductId_ProductUnitId'
    )
    BEGIN
        CREATE UNIQUE INDEX [IX_PurchaseRequestLines_PurchaseRequestId_ProductId_ProductUnitId]
            ON [dbo].[PurchaseRequestLines] ([PurchaseRequestId], [ProductId], [ProductUnitId])
            WHERE [ProductId] IS NOT NULL AND [ProductUnitId] IS NOT NULL;
    END;
END");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive: free-text lines may already contain NULL catalog references.
    }
}
