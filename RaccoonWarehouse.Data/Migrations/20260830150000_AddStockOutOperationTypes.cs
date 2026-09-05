using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations;

public partial class AddStockOutOperationTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('StockDocument', 'OperationType') IS NULL
    ALTER TABLE [StockDocument] ADD [OperationType] int NOT NULL CONSTRAINT [DF_StockDocument_OperationType] DEFAULT (1);

IF COL_LENGTH('StockDocument', 'SourceDocumentId') IS NULL
    ALTER TABLE [StockDocument] ADD [SourceDocumentId] nvarchar(max) NULL;

IF COL_LENGTH('StockDocument', 'CustomerId') IS NULL
    ALTER TABLE [StockDocument] ADD [CustomerId] int NULL;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('StockDocument', 'CustomerId') IS NOT NULL
    ALTER TABLE [StockDocument] DROP COLUMN [CustomerId];

IF COL_LENGTH('StockDocument', 'SourceDocumentId') IS NOT NULL
    ALTER TABLE [StockDocument] DROP COLUMN [SourceDocumentId];

IF COL_LENGTH('StockDocument', 'OperationType') IS NOT NULL
    ALTER TABLE [StockDocument] DROP CONSTRAINT [DF_StockDocument_OperationType];

IF COL_LENGTH('StockDocument', 'OperationType') IS NOT NULL
    ALTER TABLE [StockDocument] DROP COLUMN [OperationType];
");
    }
}
