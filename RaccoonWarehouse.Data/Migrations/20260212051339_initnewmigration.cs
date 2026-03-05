using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaccoonWarehouse.Data.Migrations
{
    /// <inheritdoc />
    public partial class initnewmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
IF COL_LENGTH('Voucher', 'CashierSessionId') IS NULL
    ALTER TABLE [Voucher] ADD [CashierSessionId] int NULL;

IF COL_LENGTH('ProductUnit', 'UnTaxedPrice') IS NULL
    ALTER TABLE [ProductUnit] ADD [UnTaxedPrice] decimal(18,2) NOT NULL CONSTRAINT [DF_ProductUnit_UnTaxedPrice] DEFAULT (0);

IF COL_LENGTH('Product', 'TaxRate') IS NULL
    ALTER TABLE [Product] ADD [TaxRate] decimal(18,2) NULL;

IF COL_LENGTH('InvoiceLine', 'OriginalInvoiceId') IS NULL
    ALTER TABLE [InvoiceLine] ADD [OriginalInvoiceId] nvarchar(max) NULL;

IF COL_LENGTH('Invoice', 'CashierSessionId') IS NULL
    ALTER TABLE [Invoice] ADD [CashierSessionId] int NULL;

IF COL_LENGTH('Invoice', 'ClosedAt') IS NULL
    ALTER TABLE [Invoice] ADD [ClosedAt] datetime2 NULL;

IF COL_LENGTH('Invoice', 'DiscountAmount') IS NULL
    ALTER TABLE [Invoice] ADD [DiscountAmount] decimal(18,2) NULL;

IF COL_LENGTH('Invoice', 'IsPOS') IS NULL
    ALTER TABLE [Invoice] ADD [IsPOS] bit NULL;

IF COL_LENGTH('Invoice', 'OpenedAt') IS NULL
    ALTER TABLE [Invoice] ADD [OpenedAt] datetime2 NULL;

IF COL_LENGTH('Invoice', 'OriginalInvoiceId') IS NULL
    ALTER TABLE [Invoice] ADD [OriginalInvoiceId] nvarchar(max) NULL;

IF COL_LENGTH('Invoice', 'Status') IS NULL
    ALTER TABLE [Invoice] ADD [Status] int NULL;

IF OBJECT_ID(N'[CashierSession]', N'U') IS NULL
BEGIN
    CREATE TABLE [CashierSession]
    (
        [Id] int NOT NULL IDENTITY,
        [CashierId] int NOT NULL,
        [OpenedAt] datetime2 NOT NULL,
        [ClosedAt] datetime2 NULL,
        [StatrBalance] decimal(18,2) NOT NULL,
        [EndingBalance] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_CashierSession] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CashierSession_User_CashierId] FOREIGN KEY ([CashierId]) REFERENCES [User] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[Tax]', N'U') IS NULL
BEGIN
    CREATE TABLE [Tax]
    (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Rate] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Tax] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[FinancialTransaction]', N'U') IS NULL
BEGIN
    CREATE TABLE [FinancialTransaction]
    (
        [Id] int NOT NULL IDENTITY,
        [TransactionNumber] nvarchar(max) NOT NULL,
        [Type] int NOT NULL,
        [Method] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Date] datetime2 NOT NULL,
        [InvoiceId] int NULL,
        [VoucherId] int NULL,
        [CasherId] int NULL,
        [CashierSessionId] int NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_FinancialTransaction] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FinancialTransaction_CashierSession_CashierSessionId] FOREIGN KEY ([CashierSessionId]) REFERENCES [CashierSession] ([Id])
    );
END;

IF OBJECT_ID(N'[StockTransaction]', N'U') IS NULL
BEGIN
    CREATE TABLE [StockTransaction]
    (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [ProductUnitId] int NOT NULL,
        [StockId] int NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TransactionType] int NOT NULL,
        [InvoiceId] int NULL,
        [VoucherId] int NULL,
        [CasherId] int NULL,
        [CashierSessionId] int NULL,
        [CustomerId] int NULL,
        [TransactionDate] datetime2 NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_StockTransaction] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockTransaction_CashierSession_CashierSessionId] FOREIGN KEY ([CashierSessionId]) REFERENCES [CashierSession] ([Id]),
        CONSTRAINT [FK_StockTransaction_Invoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoice] ([Id]),
        CONSTRAINT [FK_StockTransaction_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StockTransaction_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StockTransaction_Stock_StockId] FOREIGN KEY ([StockId]) REFERENCES [Stock] ([Id]),
        CONSTRAINT [FK_StockTransaction_User_CasherId] FOREIGN KEY ([CasherId]) REFERENCES [User] ([Id]),
        CONSTRAINT [FK_StockTransaction_User_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [User] ([Id]),
        CONSTRAINT [FK_StockTransaction_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Voucher_CashierSessionId' AND object_id = OBJECT_ID(N'[Voucher]'))
    CREATE INDEX [IX_Voucher_CashierSessionId] ON [Voucher] ([CashierSessionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Invoice_CashierSessionId' AND object_id = OBJECT_ID(N'[Invoice]'))
    CREATE INDEX [IX_Invoice_CashierSessionId] ON [Invoice] ([CashierSessionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CashierSession_CashierId' AND object_id = OBJECT_ID(N'[CashierSession]'))
    CREATE INDEX [IX_CashierSession_CashierId] ON [CashierSession] ([CashierId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FinancialTransaction_CashierSessionId' AND object_id = OBJECT_ID(N'[FinancialTransaction]'))
    CREATE INDEX [IX_FinancialTransaction_CashierSessionId] ON [FinancialTransaction] ([CashierSessionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockTransaction_CasherId' AND object_id = OBJECT_ID(N'[StockTransaction]'))
    CREATE INDEX [IX_StockTransaction_CasherId] ON [StockTransaction] ([CasherId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockTransaction_CashierSessionId' AND object_id = OBJECT_ID(N'[StockTransaction]'))
    CREATE INDEX [IX_StockTransaction_CashierSessionId] ON [StockTransaction] ([CashierSessionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockTransaction_CustomerId' AND object_id = OBJECT_ID(N'[StockTransaction]'))
    CREATE INDEX [IX_StockTransaction_CustomerId] ON [StockTransaction] ([CustomerId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockTransaction_InvoiceId' AND object_id = OBJECT_ID(N'[StockTransaction]'))
    CREATE INDEX [IX_StockTransaction_InvoiceId] ON [StockTransaction] ([InvoiceId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockTransaction_ProductId' AND object_id = OBJECT_ID(N'[StockTransaction]'))
    CREATE INDEX [IX_StockTransaction_ProductId] ON [StockTransaction] ([ProductId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockTransaction_ProductUnitId' AND object_id = OBJECT_ID(N'[StockTransaction]'))
    CREATE INDEX [IX_StockTransaction_ProductUnitId] ON [StockTransaction] ([ProductUnitId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockTransaction_StockId' AND object_id = OBJECT_ID(N'[StockTransaction]'))
    CREATE INDEX [IX_StockTransaction_StockId] ON [StockTransaction] ([StockId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockTransaction_VoucherId' AND object_id = OBJECT_ID(N'[StockTransaction]'))
    CREATE INDEX [IX_StockTransaction_VoucherId] ON [StockTransaction] ([VoucherId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Invoice_CashierSession_CashierSessionId')
    ALTER TABLE [Invoice] ADD CONSTRAINT [FK_Invoice_CashierSession_CashierSessionId] FOREIGN KEY ([CashierSessionId]) REFERENCES [CashierSession] ([Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Voucher_CashierSession_CashierSessionId')
    ALTER TABLE [Voucher] ADD CONSTRAINT [FK_Voucher_CashierSession_CashierSessionId] FOREIGN KEY ([CashierSessionId]) REFERENCES [CashierSession] ([Id]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_CashierSession_CashierSessionId",
                table: "Invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_Voucher_CashierSession_CashierSessionId",
                table: "Voucher");

            migrationBuilder.DropTable(
                name: "FinancialTransaction");

            migrationBuilder.DropTable(
                name: "StockTransaction");

            migrationBuilder.DropTable(
                name: "Tax");

            migrationBuilder.DropTable(
                name: "CashierSession");

            migrationBuilder.DropIndex(
                name: "IX_Voucher_CashierSessionId",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_CashierSessionId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "CashierSessionId",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "UnTaxedPrice",
                table: "ProductUnit");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceId",
                table: "InvoiceLine");

            migrationBuilder.DropColumn(
                name: "CashierSessionId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "IsPOS",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "OpenedAt",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Invoice");
        }
    }
}
