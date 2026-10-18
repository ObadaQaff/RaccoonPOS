using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <summary>
    /// Repairs columns present in the EF model but missing from older databases.
    /// </summary>
    [Migration("20261017150000_RepairStockDocumentColumns")]
    public partial class RepairStockDocumentColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.StockDocument', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.StockDocument', N'CustomerId') IS NULL
        ALTER TABLE [dbo].[StockDocument] ADD [CustomerId] int NULL;

    IF COL_LENGTH(N'dbo.StockDocument', N'OperationType') IS NULL
        ALTER TABLE [dbo].[StockDocument]
        ADD [OperationType] int NOT NULL
            CONSTRAINT [DF_StockDocument_OperationType] DEFAULT (0) WITH VALUES;

    IF COL_LENGTH(N'dbo.StockDocument', N'SourceDocumentId') IS NULL
        ALTER TABLE [dbo].[StockDocument] ADD [SourceDocumentId] nvarchar(max) NULL;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally non-destructive: these columns may contain history.
        }
    }
}
