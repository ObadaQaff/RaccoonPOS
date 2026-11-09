SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [Brand] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [ImageUrl] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Brand] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [Category] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [ImageUrl] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Category] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [Unit] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Unit] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [User] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [Password] nvarchar(max) NOT NULL,
        [Role] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_User] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [SubCategory] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [ImageUrl] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [ParentCategoryId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_SubCategory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubCategory_Category_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Category] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [Voucher] (
        [Id] int NOT NULL IDENTITY,
        [VoucherType] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentType] int NOT NULL,
        [CasherId] int NOT NULL,
        [SupplierId] int NULL,
        [CustomerId] int NULL,
        [Notes] nvarchar(max) NULL,
        [UserId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Voucher] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Voucher_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [Product] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NULL,
        [ITEMCODE] bigint NULL,
        [ImageUrl] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [TaxExempt] bit NULL,
        [MiniQuantity] decimal(18,2) NULL,
        [SubCategoryId] int NOT NULL,
        [BrandId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Product] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Product_Brand_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brand] ([Id]),
        CONSTRAINT [FK_Product_SubCategory_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [SubCategory] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [SubCategoryBrand] (
        [Id] int NOT NULL IDENTITY,
        [SubCategoryId] int NOT NULL,
        [BrandId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_SubCategoryBrand] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubCategoryBrand_Brand_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brand] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SubCategoryBrand_SubCategory_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [SubCategory] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [Invoice] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceType] int NOT NULL,
        [CasherId] int NOT NULL,
        [SupplierId] int NULL,
        [CustomerId] int NULL,
        [UserId] int NULL,
        [VoucherId] int NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Invoice] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoice_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]),
        CONSTRAINT [FK_Invoice_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [ProductUnit] (
        [Id] int NOT NULL IDENTITY,
        [SalePrice] decimal(18,2) NOT NULL,
        [PurchasePrice] decimal(18,2) NOT NULL,
        [QuantityPerUnit] decimal(18,2) NOT NULL,
        [ProductId] int NOT NULL,
        [UnitId] int NOT NULL,
        [UnitId1] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductUnit] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductUnit_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]),
        CONSTRAINT [FK_ProductUnit_Unit_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Unit] ([Id]),
        CONSTRAINT [FK_ProductUnit_Unit_UnitId1] FOREIGN KEY ([UnitId1]) REFERENCES [Unit] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE TABLE [InvoiceLine] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ProductUnitId] int NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InvoiceLine] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoiceLine_Invoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoice] ([Id]),
        CONSTRAINT [FK_InvoiceLine_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]),
        CONSTRAINT [FK_InvoiceLine_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_Invoice_UserId] ON [Invoice] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_Invoice_VoucherId] ON [Invoice] ([VoucherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_InvoiceLine_InvoiceId] ON [InvoiceLine] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_InvoiceLine_ProductId] ON [InvoiceLine] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_InvoiceLine_ProductUnitId] ON [InvoiceLine] ([ProductUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_Product_BrandId] ON [Product] ([BrandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_Product_SubCategoryId] ON [Product] ([SubCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_ProductUnit_ProductId] ON [ProductUnit] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_ProductUnit_UnitId] ON [ProductUnit] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_ProductUnit_UnitId1] ON [ProductUnit] ([UnitId1]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_SubCategory_ParentCategoryId] ON [SubCategory] ([ParentCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_SubCategoryBrand_BrandId] ON [SubCategoryBrand] ([BrandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_SubCategoryBrand_SubCategoryId] ON [SubCategoryBrand] ([SubCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    CREATE INDEX [IX_Voucher_UserId] ON [Voucher] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014134038_FixCascadePaths'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251014134038_FixCascadePaths', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016070154_StockHandle'
)
BEGIN
    CREATE TABLE [Stock] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [ProductUnitId] int NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Stock] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Stock_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Stock_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016070154_StockHandle'
)
BEGIN
    CREATE TABLE [StockDocument] (
        [Id] int NOT NULL IDENTITY,
        [DocumentNumber] nvarchar(max) NOT NULL,
        [Type] int NOT NULL,
        [Supplier] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_StockDocument] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016070154_StockHandle'
)
BEGIN
    CREATE TABLE [Warehouse] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [PhoneNumber] int NULL,
        [Status] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Warehouse] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016070154_StockHandle'
)
BEGIN
    CREATE TABLE [StockItem] (
        [Id] int NOT NULL IDENTITY,
        [StockId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ProductUnitId] int NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [PurchasePrice] decimal(18,2) NOT NULL,
        [SalePrice] decimal(18,2) NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_StockItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockItem_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StockItem_StockDocument_StockId] FOREIGN KEY ([StockId]) REFERENCES [StockDocument] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016070154_StockHandle'
)
BEGIN
    CREATE INDEX [IX_Stock_ProductId] ON [Stock] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016070154_StockHandle'
)
BEGIN
    CREATE INDEX [IX_Stock_ProductUnitId] ON [Stock] ([ProductUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016070154_StockHandle'
)
BEGIN
    CREATE INDEX [IX_StockItem_ProductId] ON [StockItem] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016070154_StockHandle'
)
BEGIN
    CREATE INDEX [IX_StockItem_StockId] ON [StockItem] ([StockId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016070154_StockHandle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251016070154_StockHandle', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017101922_updateSupplier'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockDocument]') AND [c].[name] = N'Supplier');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [StockDocument] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [StockDocument] DROP COLUMN [Supplier];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017101922_updateSupplier'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251017101922_updateSupplier', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017102029_updateSupplier2'
)
BEGIN
    ALTER TABLE [StockDocument] ADD [SupplierId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017102029_updateSupplier2'
)
BEGIN
    CREATE INDEX [IX_StockDocument_SupplierId] ON [StockDocument] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017102029_updateSupplier2'
)
BEGIN
    ALTER TABLE [StockDocument] ADD CONSTRAINT [FK_StockDocument_User_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017102029_updateSupplier2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251017102029_updateSupplier2', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017141331_hello'
)
BEGIN
    CREATE INDEX [IX_StockItem_ProductUnitId] ON [StockItem] ([ProductUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017141331_hello'
)
BEGIN
    ALTER TABLE [StockItem] ADD CONSTRAINT [FK_StockItem_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251017141331_hello'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251017141331_hello', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023020504_tempenddate'
)
BEGIN
    ALTER TABLE [Product] ADD [EndDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023020504_tempenddate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251023020504_tempenddate', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023021110_tempenddate2'
)
BEGIN
    ALTER TABLE [Product] ADD [IsSoldOut] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023021110_tempenddate2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251023021110_tempenddate2', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023021226_tempenddate22'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Product]') AND [c].[name] = N'Status');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Product] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Product] ALTER COLUMN [Status] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251023021226_tempenddate22'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251023021226_tempenddate22', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106022539_FixCascadeRelations'
)
BEGIN
    ALTER TABLE [Invoice] DROP CONSTRAINT [FK_Invoice_Voucher_VoucherId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106022539_FixCascadeRelations'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'VoucherId');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Invoice] ALTER COLUMN [VoucherId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106022539_FixCascadeRelations'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'CasherId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Invoice] ALTER COLUMN [CasherId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106022539_FixCascadeRelations'
)
BEGIN
    ALTER TABLE [Invoice] ADD CONSTRAINT [FK_Invoice_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106022539_FixCascadeRelations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251106022539_FixCascadeRelations', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106022828_FixCascadeRelations1'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251106022828_FixCascadeRelations1', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111044311_AddIsDeletedToEntities'
)
BEGIN
    ALTER TABLE [Product] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111044311_AddIsDeletedToEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251111044311_AddIsDeletedToEntities', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118010348_AddCheckAndUpdateInvoiceAndVoucher'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Voucher]') AND [c].[name] = N'CasherId');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Voucher] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Voucher] ALTER COLUMN [CasherId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118010348_AddCheckAndUpdateInvoiceAndVoucher'
)
BEGIN
    ALTER TABLE [Invoice] ADD [PaymentType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118010348_AddCheckAndUpdateInvoiceAndVoucher'
)
BEGIN
    CREATE TABLE [Check] (
        [Id] int NOT NULL IDENTITY,
        [CheckNumber] nvarchar(max) NOT NULL,
        [BankName] nvarchar(max) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [VoucherId] int NULL,
        [InvoiceId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Check] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Check_Invoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoice] ([Id]),
        CONSTRAINT [FK_Check_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118010348_AddCheckAndUpdateInvoiceAndVoucher'
)
BEGIN
    CREATE INDEX [IX_Check_InvoiceId] ON [Check] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118010348_AddCheckAndUpdateInvoiceAndVoucher'
)
BEGIN
    CREATE INDEX [IX_Check_VoucherId] ON [Check] ([VoucherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118010348_AddCheckAndUpdateInvoiceAndVoucher'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118010348_AddCheckAndUpdateInvoiceAndVoucher', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120050656_aaassad'
)
BEGIN
    ALTER TABLE [Voucher] ADD [VoucherNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120050656_aaassad'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251120050656_aaassad', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122135854_asas'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251122135854_asas', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251128102337_addExpierDate'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [ExpiryDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251128102337_addExpierDate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251128102337_addExpierDate', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251128112214_addExpierDate2'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InvoiceNumber] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251128112214_addExpierDate2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251128112214_addExpierDate2', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260212051339_initnewmigration'
)
BEGIN

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

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260212051339_initnewmigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260212051339_initnewmigration', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    DROP INDEX [IX_StockItem_StockId] ON [StockItem];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Warehouse] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Warehouse] ADD [Code] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Warehouse] ADD [CreatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Warehouse] ADD [UpdatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Voucher] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Voucher] ADD [CreatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Voucher] ADD [CurrencyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Voucher] ADD [ExchangeRate] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Voucher] ADD [PostingStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Voucher] ADD [ReferenceNumber] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Voucher] ADD [UpdatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Voucher] ADD [VoucherDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Voucher] ADD [WarehouseId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [BaseQuantity] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [CreatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [ExpiryDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [QuantityPerUnitSnapshot] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [ReferenceNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [SourceId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [SourceType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [StockAdjustmentId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [StockDocumentId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [StockLotId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD [WarehouseId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockItem] ADD [BaseQuantity] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockItem] ADD [LineNumber] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockItem] ADD [QuantityPerUnitSnapshot] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockItem] ADD [StockLotId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockDocument]') AND [c].[name] = N'DocumentNumber');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [StockDocument] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [StockDocument] ALTER COLUMN [DocumentNumber] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockDocument] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockDocument] ADD [CreatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockDocument] ADD [DocumentDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockDocument] ADD [PostingStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockDocument] ADD [ReferenceNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockDocument] ADD [UpdatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockDocument] ADD [WarehouseId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Stock] ADD [LastMovementDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Stock] ADD [PurchasePrice] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Stock] ADD [SalePrice] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Stock] ADD [WarehouseId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [ProductUnit] ADD [IsBaseUnit] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [ProductUnit] ADD [IsDefaultPurchaseUnit] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [ProductUnit] ADD [IsDefaultSaleUnit] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [BaseQuantity] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [LineSubTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [Profit] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [ProfitBeforeTax] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [QuantityPerUnitSnapshot] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [TaxAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [TaxExempt] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [TaxRate] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [UnitCost] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [CreatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [CurrencyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [DelegateId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [DocumentDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [ExchangeRate] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [GrossProfit] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [NetSales] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [PostingStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [ReferenceNumber] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [SubTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [TotalCOGS] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [TotalTax] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [UpdatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD [WarehouseId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [CreatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [CurrencyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [Direction] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [ExchangeRate] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [PostingStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [ReferenceNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [SourceId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [SourceType] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [Status] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [UpdatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD [WarehouseId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [CreatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [DifferenceAmount] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [ExpectedClosingBalance] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [SessionNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [UpdatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [Accounts] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [ArabicName] nvarchar(max) NULL,
        [EnglishName] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [AccountType] int NOT NULL,
        [NormalBalanceType] int NOT NULL,
        [IsPosting] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [IsSystemGenerated] bit NOT NULL,
        [AllowManualEntry] bit NOT NULL,
        [Level] int NOT NULL,
        [CurrencyId] int NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [ParentAccountId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Accounts_Accounts_ParentAccountId] FOREIGN KEY ([ParentAccountId]) REFERENCES [Accounts] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    IF OBJECT_ID(N'[AppSettings]', N'U') IS NULL
    CREATE TABLE [AppSettings] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_AppSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [Branches] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [ArabicName] nvarchar(max) NULL,
        [EnglishName] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [CostCenters] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [ArabicName] nvarchar(max) NULL,
        [EnglishName] nvarchar(max) NULL,
        [ParentCostCenterId] int NULL,
        [Level] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_CostCenters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CostCenters_CostCenters_ParentCostCenterId] FOREIGN KEY ([ParentCostCenterId]) REFERENCES [CostCenters] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [Currencies] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [ArabicName] nvarchar(max) NULL,
        [EnglishName] nvarchar(max) NULL,
        [Symbol] nvarchar(max) NULL,
        [ExchangeRate] decimal(18,2) NOT NULL,
        [IsBaseCurrency] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Currencies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [Delegate] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [Code] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [AlternatePhoneNumber] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [DelegateType] int NOT NULL,
        [RegionId] int NULL,
        [AreaName] nvarchar(max) NULL,
        [HireDate] datetime2 NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedBy] int NULL,
        [ModifiedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Delegate] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Delegate_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [Employee] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [Code] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [AlternatePhoneNumber] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [NationalId] nvarchar(max) NULL,
        [HireDate] datetime2 NULL,
        [TerminationDate] datetime2 NULL,
        [Status] int NOT NULL,
        [Gender] int NULL,
        [JobTitle] nvarchar(max) NULL,
        [DepartmentId] int NULL,
        [BranchId] int NULL,
        [ManagerId] int NULL,
        [BasicSalary] decimal(18,2) NULL,
        [Notes] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [DateOfBirth] datetime2 NULL,
        [CreatedBy] int NULL,
        [ModifiedBy] int NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Employee] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employee_Employee_ManagerId] FOREIGN KEY ([ManagerId]) REFERENCES [Employee] ([Id]),
        CONSTRAINT [FK_Employee_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [FiscalYears] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [IsClosed] bit NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_FiscalYears] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [PermissionDefinitions] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(450) NOT NULL,
        [Module] nvarchar(max) NOT NULL,
        [Resource] nvarchar(max) NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [LegacyReportKey] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PermissionDefinitions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [ReportPermissions] (
        [Id] int NOT NULL IDENTITY,
        [ReportKey] nvarchar(450) NOT NULL,
        [Role] int NOT NULL,
        [CanView] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ReportPermissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [Id] int NOT NULL IDENTITY,
        [Role] int NOT NULL,
        [PermissionKey] nvarchar(450) NOT NULL,
        [IsAllowed] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [StockLot] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [ProductUnitId] int NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [RemainingQuantity] decimal(18,2) NOT NULL,
        [QuantityPerUnitSnapshot] decimal(18,2) NOT NULL,
        [BaseQuantity] decimal(18,2) NOT NULL,
        [RemainingBaseQuantity] decimal(18,2) NOT NULL,
        [PurchasePrice] decimal(18,2) NOT NULL,
        [SalePrice] decimal(18,2) NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [Notes] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [ClosedDate] datetime2 NULL,
        [ClosedReason] nvarchar(max) NULL,
        [ClosedByUserId] int NULL,
        [ReplacesStockLotId] int NULL,
        [ReplacedByStockLotId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_StockLot] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockLot_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StockLot_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StockLot_StockLot_ReplacesStockLotId] FOREIGN KEY ([ReplacesStockLotId]) REFERENCES [StockLot] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [AccountingPeriods] (
        [Id] int NOT NULL IDENTITY,
        [FiscalYearId] int NOT NULL,
        [PeriodNumber] int NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [IsClosed] bit NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_AccountingPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountingPeriods_FiscalYears_FiscalYearId] FOREIGN KEY ([FiscalYearId]) REFERENCES [FiscalYears] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [AccountOpeningBalances] (
        [Id] int NOT NULL IDENTITY,
        [FiscalYearId] int NOT NULL,
        [AccountId] int NOT NULL,
        [BranchId] int NULL,
        [CostCenterId] int NULL,
        [WarehouseId] int NULL,
        [PartyUserId] int NULL,
        [Debit] decimal(18,2) NOT NULL,
        [Credit] decimal(18,2) NOT NULL,
        [ReferenceNumber] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_AccountOpeningBalances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountOpeningBalances_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]),
        CONSTRAINT [FK_AccountOpeningBalances_FiscalYears_FiscalYearId] FOREIGN KEY ([FiscalYearId]) REFERENCES [FiscalYears] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [StockAdjustment] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [ProductUnitId] int NOT NULL,
        [StockLotId] int NOT NULL,
        [NewStockLotId] int NULL,
        [AdjustmentType] int NOT NULL,
        [QuantityDelta] decimal(18,2) NOT NULL,
        [QuantityPerUnitSnapshot] decimal(18,2) NOT NULL,
        [BaseQuantityDelta] decimal(18,2) NOT NULL,
        [PurchasePrice] decimal(18,2) NULL,
        [SalePrice] decimal(18,2) NULL,
        [ExpiryDate] datetime2 NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Reference] nvarchar(max) NULL,
        [AdjustmentDate] datetime2 NOT NULL,
        [UserId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_StockAdjustment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockAdjustment_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StockAdjustment_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StockAdjustment_StockLot_NewStockLotId] FOREIGN KEY ([NewStockLotId]) REFERENCES [StockLot] ([Id]),
        CONSTRAINT [FK_StockAdjustment_StockLot_StockLotId] FOREIGN KEY ([StockLotId]) REFERENCES [StockLot] ([Id]),
        CONSTRAINT [FK_StockAdjustment_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [JournalEntries] (
        [Id] int NOT NULL IDENTITY,
        [EntryNumber] nvarchar(450) NOT NULL,
        [EntryDate] datetime2 NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [IsDraft] bit NOT NULL,
        [SourceType] int NULL,
        [SourceId] int NULL,
        [ReferenceType] nvarchar(max) NULL,
        [ReferenceId] int NULL,
        [ReferenceNumber] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [FiscalYearId] int NULL,
        [AccountingPeriodId] int NULL,
        [BranchId] int NULL,
        [WarehouseId] int NULL,
        [CashierSessionId] int NULL,
        [CurrencyId] int NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [ApprovedBy] int NULL,
        [ApprovedAt] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_JournalEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JournalEntries_AccountingPeriods_AccountingPeriodId] FOREIGN KEY ([AccountingPeriodId]) REFERENCES [AccountingPeriods] ([Id]),
        CONSTRAINT [FK_JournalEntries_FiscalYears_FiscalYearId] FOREIGN KEY ([FiscalYearId]) REFERENCES [FiscalYears] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE TABLE [JournalEntryLines] (
        [Id] int NOT NULL IDENTITY,
        [JournalEntryId] int NOT NULL,
        [AccountId] int NOT NULL,
        [LineNumber] int NOT NULL,
        [Debit] decimal(18,2) NOT NULL,
        [Credit] decimal(18,2) NOT NULL,
        [Description] nvarchar(max) NULL,
        [PartyUserId] int NULL,
        [CustomerId] int NULL,
        [SupplierId] int NULL,
        [CashierId] int NULL,
        [WarehouseId] int NULL,
        [CostCenterId] int NULL,
        [BranchId] int NULL,
        [InvoiceId] int NULL,
        [VoucherId] int NULL,
        [StockDocumentId] int NULL,
        [FinancialTransactionId] int NULL,
        [ReferenceType] nvarchar(max) NULL,
        [ReferenceId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_JournalEntryLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JournalEntryLines_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]),
        CONSTRAINT [FK_JournalEntryLines_JournalEntries_JournalEntryId] FOREIGN KEY ([JournalEntryId]) REFERENCES [JournalEntries] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_Voucher_ReferenceNumber] ON [Voucher] ([ReferenceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockTransaction_SourceType_SourceId] ON [StockTransaction] ([SourceType], [SourceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockTransaction_StockAdjustmentId] ON [StockTransaction] ([StockAdjustmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockTransaction_StockDocumentId] ON [StockTransaction] ([StockDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockTransaction_StockLotId] ON [StockTransaction] ([StockLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockItem_StockId_LineNumber] ON [StockItem] ([StockId], [LineNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockDocument_DocumentNumber] ON [StockDocument] ([DocumentNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_Stock_WarehouseId_ProductId_ProductUnitId] ON [Stock] ([WarehouseId], [ProductId], [ProductUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_Invoice_DelegateId] ON [Invoice] ([DelegateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_Invoice_ReferenceNumber] ON [Invoice] ([ReferenceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_FinancialTransaction_CasherId] ON [FinancialTransaction] ([CasherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_FinancialTransaction_SourceType_SourceId] ON [FinancialTransaction] ([SourceType], [SourceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountingPeriods_FiscalYearId_PeriodNumber] ON [AccountingPeriods] ([FiscalYearId], [PeriodNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountOpeningBalances_AccountId] ON [AccountOpeningBalances] ([AccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountOpeningBalances_FiscalYearId_AccountId_BranchId_CostCenterId_WarehouseId_PartyUserId] ON [AccountOpeningBalances] ([FiscalYearId], [AccountId], [BranchId], [CostCenterId], [WarehouseId], [PartyUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Accounts_Code] ON [Accounts] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_Accounts_ParentAccountId] ON [Accounts] ([ParentAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    IF NOT EXISTS (
        SELECT * FROM sys.indexes
        WHERE [name] = N'IX_AppSettings_Key'
          AND [object_id] = OBJECT_ID(N'[AppSettings]')
    )
    CREATE UNIQUE INDEX [IX_AppSettings_Key] ON [AppSettings] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Branches_Code] ON [Branches] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CostCenters_Code] ON [CostCenters] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_CostCenters_ParentCostCenterId] ON [CostCenters] ([ParentCostCenterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Currencies_Code] ON [Currencies] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Delegate_Code] ON [Delegate] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Delegate_UserId] ON [Delegate] ([UserId]) WHERE [UserId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_Employee_BranchId] ON [Employee] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employee_Code] ON [Employee] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_Employee_DepartmentId] ON [Employee] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_Employee_ManagerId] ON [Employee] ([ManagerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_Employee_Status] ON [Employee] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Employee_UserId] ON [Employee] ([UserId]) WHERE [UserId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FiscalYears_Code] ON [FiscalYears] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_JournalEntries_AccountingPeriodId] ON [JournalEntries] ([AccountingPeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JournalEntries_EntryNumber] ON [JournalEntries] ([EntryNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_JournalEntries_FiscalYearId] ON [JournalEntries] ([FiscalYearId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_JournalEntryLines_AccountId] ON [JournalEntryLines] ([AccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JournalEntryLines_JournalEntryId_LineNumber] ON [JournalEntryLines] ([JournalEntryId], [LineNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PermissionDefinitions_Key] ON [PermissionDefinitions] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReportPermissions_ReportKey_Role] ON [ReportPermissions] ([ReportKey], [Role]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolePermissions_Role_PermissionKey] ON [RolePermissions] ([Role], [PermissionKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockAdjustment_NewStockLotId] ON [StockAdjustment] ([NewStockLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockAdjustment_ProductId] ON [StockAdjustment] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockAdjustment_ProductUnitId] ON [StockAdjustment] ([ProductUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockAdjustment_StockLotId] ON [StockAdjustment] ([StockLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockAdjustment_UserId] ON [StockAdjustment] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockLot_ProductId] ON [StockLot] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    CREATE INDEX [IX_StockLot_ProductUnitId] ON [StockLot] ([ProductUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StockLot_ReplacesStockLotId] ON [StockLot] ([ReplacesStockLotId]) WHERE [ReplacesStockLotId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [FinancialTransaction] ADD CONSTRAINT [FK_FinancialTransaction_User_CasherId] FOREIGN KEY ([CasherId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [Invoice] ADD CONSTRAINT [FK_Invoice_Delegate_DelegateId] FOREIGN KEY ([DelegateId]) REFERENCES [Delegate] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD CONSTRAINT [FK_StockTransaction_StockAdjustment_StockAdjustmentId] FOREIGN KEY ([StockAdjustmentId]) REFERENCES [StockAdjustment] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    ALTER TABLE [StockTransaction] ADD CONSTRAINT [FK_StockTransaction_StockLot_StockLotId] FOREIGN KEY ([StockLotId]) REFERENCES [StockLot] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260411115659_Phase1AccountingFoundation', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528000100_AddCustomerBankDetails'
)
BEGIN
    ALTER TABLE [User] ADD [BankAccountNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528000100_AddCustomerBankDetails'
)
BEGIN
    ALTER TABLE [User] ADD [BankIban] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528000100_AddCustomerBankDetails'
)
BEGIN
    ALTER TABLE [User] ADD [BankName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528000100_AddCustomerBankDetails'
)
BEGIN
    ALTER TABLE [User] ADD [BankSwiftCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528000100_AddCustomerBankDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260528000100_AddCustomerBankDetails', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730083917_AddIntegrationInbox'
)
BEGIN
    CREATE TABLE [IntegrationInbox] (
        [Id] int NOT NULL IDENTITY,
        [EventId] uniqueidentifier NOT NULL,
        [EventType] nvarchar(100) NOT NULL,
        [EventVersion] int NOT NULL,
        [SourceSystem] nvarchar(50) NOT NULL,
        [ExternalOrderId] nvarchar(100) NOT NULL,
        [PayloadHash] nvarchar(64) NOT NULL,
        [Status] int NOT NULL,
        [ReceivedAtUtc] datetime2 NOT NULL,
        [ProcessedAtUtc] datetime2 NULL,
        [RaccoonInvoiceId] int NULL,
        [LastErrorCode] nvarchar(100) NULL,
        [LastErrorSummary] nvarchar(1000) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_IntegrationInbox] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730083917_AddIntegrationInbox'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IntegrationInbox_EventId] ON [IntegrationInbox] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730083917_AddIntegrationInbox'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IntegrationInbox_SourceSystem_ExternalOrderId] ON [IntegrationInbox] ([SourceSystem], [ExternalOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730083917_AddIntegrationInbox'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730083917_AddIntegrationInbox', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804000820_AddVoucherPartyLinks'
)
BEGIN
    IF COL_LENGTH(N'[Voucher]', N'CustomerId') IS NULL
        ALTER TABLE [Voucher] ADD [CustomerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804000820_AddVoucherPartyLinks'
)
BEGIN
    IF COL_LENGTH(N'[Voucher]', N'SupplierId') IS NULL
        ALTER TABLE [Voucher] ADD [SupplierId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804000820_AddVoucherPartyLinks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260804000820_AddVoucherPartyLinks', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804010000_RepairCreditPurchaseSupplierLinks'
)
BEGIN
    UPDATE [Invoice]
    SET [SupplierId] = [CustomerId],
        [CustomerId] = NULL
    WHERE [InvoiceType] = 2
      AND [PaymentType] = 2
      AND [SupplierId] IS NULL
      AND [CustomerId] IS NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804010000_RepairCreditPurchaseSupplierLinks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260804010000_RepairCreditPurchaseSupplierLinks', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819073117_AddInvoiceNotes'
)
BEGIN
    ALTER TABLE [Invoice] ADD [Notes] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819073117_AddInvoiceNotes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260819073117_AddInvoiceNotes', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819215403_AddStockDocumentDiscount'
)
BEGIN
    ALTER TABLE [StockDocument] ADD [DiscountAmount] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819215403_AddStockDocumentDiscount'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260819215403_AddStockDocumentDiscount', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819225929_AddStockDocumentPaymentTypeAndChecks'
)
BEGIN
    ALTER TABLE [StockDocument] ADD [PaymentType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819225929_AddStockDocumentPaymentTypeAndChecks'
)
BEGIN
    ALTER TABLE [Check] ADD [StockDocumentId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819225929_AddStockDocumentPaymentTypeAndChecks'
)
BEGIN
    CREATE INDEX [IX_Check_StockDocumentId] ON [Check] ([StockDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819225929_AddStockDocumentPaymentTypeAndChecks'
)
BEGIN
    ALTER TABLE [Check] ADD CONSTRAINT [FK_Check_StockDocument_StockDocumentId] FOREIGN KEY ([StockDocumentId]) REFERENCES [StockDocument] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819225929_AddStockDocumentPaymentTypeAndChecks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260819225929_AddStockDocumentPaymentTypeAndChecks', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820130922_AddProductUnitAlternateBarcode'
)
BEGIN
    ALTER TABLE [ProductUnit] ADD [AlternateBarcode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820130922_AddProductUnitAlternateBarcode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260820130922_AddProductUnitAlternateBarcode', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Voucher]') AND [c].[name] = N'ExchangeRate');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Voucher] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Voucher] ALTER COLUMN [ExchangeRate] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Voucher]') AND [c].[name] = N'Amount');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Voucher] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [Voucher] ALTER COLUMN [Amount] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[User]') AND [c].[name] = N'OpeningBalance');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [User] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [User] ALTER COLUMN [OpeningBalance] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[User]') AND [c].[name] = N'CurrentBalance');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [User] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [User] ALTER COLUMN [CurrentBalance] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[User]') AND [c].[name] = N'CreditLimit');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [User] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [User] ALTER COLUMN [CreditLimit] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TaxRates]') AND [c].[name] = N'Rate');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [TaxRates] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [TaxRates] ALTER COLUMN [Rate] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockTransaction]') AND [c].[name] = N'UnitPrice');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [StockTransaction] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [StockTransaction] ALTER COLUMN [UnitPrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockTransaction]') AND [c].[name] = N'QuantityPerUnitSnapshot');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [StockTransaction] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [StockTransaction] ALTER COLUMN [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockTransaction]') AND [c].[name] = N'Quantity');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [StockTransaction] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [StockTransaction] ALTER COLUMN [Quantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockTransaction]') AND [c].[name] = N'BaseQuantity');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [StockTransaction] DROP CONSTRAINT [' + @var15 + '];');
    ALTER TABLE [StockTransaction] ALTER COLUMN [BaseQuantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'SalePrice');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [StockLot] ALTER COLUMN [SalePrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'RemainingQuantity');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var17 + '];');
    ALTER TABLE [StockLot] ALTER COLUMN [RemainingQuantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var18 sysname;
    SELECT @var18 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'RemainingBaseQuantity');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var18 + '];');
    ALTER TABLE [StockLot] ALTER COLUMN [RemainingBaseQuantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var19 sysname;
    SELECT @var19 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'QuantityPerUnitSnapshot');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var19 + '];');
    ALTER TABLE [StockLot] ALTER COLUMN [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var20 sysname;
    SELECT @var20 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'Quantity');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var20 + '];');
    ALTER TABLE [StockLot] ALTER COLUMN [Quantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var21 sysname;
    SELECT @var21 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'PurchasePrice');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var21 + '];');
    ALTER TABLE [StockLot] ALTER COLUMN [PurchasePrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var22 sysname;
    SELECT @var22 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'BaseQuantity');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var22 + '];');
    ALTER TABLE [StockLot] ALTER COLUMN [BaseQuantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var23 sysname;
    SELECT @var23 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockItem]') AND [c].[name] = N'SalePrice');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [StockItem] DROP CONSTRAINT [' + @var23 + '];');
    ALTER TABLE [StockItem] ALTER COLUMN [SalePrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var24 sysname;
    SELECT @var24 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockItem]') AND [c].[name] = N'QuantityPerUnitSnapshot');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [StockItem] DROP CONSTRAINT [' + @var24 + '];');
    ALTER TABLE [StockItem] ALTER COLUMN [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var25 sysname;
    SELECT @var25 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockItem]') AND [c].[name] = N'Quantity');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [StockItem] DROP CONSTRAINT [' + @var25 + '];');
    ALTER TABLE [StockItem] ALTER COLUMN [Quantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var26 sysname;
    SELECT @var26 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockItem]') AND [c].[name] = N'PurchasePrice');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [StockItem] DROP CONSTRAINT [' + @var26 + '];');
    ALTER TABLE [StockItem] ALTER COLUMN [PurchasePrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var27 sysname;
    SELECT @var27 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockItem]') AND [c].[name] = N'BaseQuantity');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [StockItem] DROP CONSTRAINT [' + @var27 + '];');
    ALTER TABLE [StockItem] ALTER COLUMN [BaseQuantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var28 sysname;
    SELECT @var28 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockDocument]') AND [c].[name] = N'DiscountAmount');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [StockDocument] DROP CONSTRAINT [' + @var28 + '];');
    ALTER TABLE [StockDocument] ALTER COLUMN [DiscountAmount] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var29 sysname;
    SELECT @var29 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockAdjustment]') AND [c].[name] = N'SalePrice');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [StockAdjustment] DROP CONSTRAINT [' + @var29 + '];');
    ALTER TABLE [StockAdjustment] ALTER COLUMN [SalePrice] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var30 sysname;
    SELECT @var30 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockAdjustment]') AND [c].[name] = N'QuantityPerUnitSnapshot');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [StockAdjustment] DROP CONSTRAINT [' + @var30 + '];');
    ALTER TABLE [StockAdjustment] ALTER COLUMN [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var31 sysname;
    SELECT @var31 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockAdjustment]') AND [c].[name] = N'QuantityDelta');
    IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [StockAdjustment] DROP CONSTRAINT [' + @var31 + '];');
    ALTER TABLE [StockAdjustment] ALTER COLUMN [QuantityDelta] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var32 sysname;
    SELECT @var32 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockAdjustment]') AND [c].[name] = N'PurchasePrice');
    IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [StockAdjustment] DROP CONSTRAINT [' + @var32 + '];');
    ALTER TABLE [StockAdjustment] ALTER COLUMN [PurchasePrice] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var33 sysname;
    SELECT @var33 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockAdjustment]') AND [c].[name] = N'BaseQuantityDelta');
    IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [StockAdjustment] DROP CONSTRAINT [' + @var33 + '];');
    ALTER TABLE [StockAdjustment] ALTER COLUMN [BaseQuantityDelta] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var34 sysname;
    SELECT @var34 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stock]') AND [c].[name] = N'SalePrice');
    IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [Stock] DROP CONSTRAINT [' + @var34 + '];');
    ALTER TABLE [Stock] ALTER COLUMN [SalePrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var35 sysname;
    SELECT @var35 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stock]') AND [c].[name] = N'Quantity');
    IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [Stock] DROP CONSTRAINT [' + @var35 + '];');
    ALTER TABLE [Stock] ALTER COLUMN [Quantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var36 sysname;
    SELECT @var36 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stock]') AND [c].[name] = N'PurchasePrice');
    IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [Stock] DROP CONSTRAINT [' + @var36 + '];');
    ALTER TABLE [Stock] ALTER COLUMN [PurchasePrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var37 sysname;
    SELECT @var37 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RecurringJournalLines]') AND [c].[name] = N'DebitAmount');
    IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [RecurringJournalLines] DROP CONSTRAINT [' + @var37 + '];');
    ALTER TABLE [RecurringJournalLines] ALTER COLUMN [DebitAmount] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var38 sysname;
    SELECT @var38 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RecurringJournalLines]') AND [c].[name] = N'CreditAmount');
    IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [RecurringJournalLines] DROP CONSTRAINT [' + @var38 + '];');
    ALTER TABLE [RecurringJournalLines] ALTER COLUMN [CreditAmount] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var39 sysname;
    SELECT @var39 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductUnit]') AND [c].[name] = N'UnTaxedPrice');
    IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [ProductUnit] DROP CONSTRAINT [' + @var39 + '];');
    ALTER TABLE [ProductUnit] ALTER COLUMN [UnTaxedPrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var40 sysname;
    SELECT @var40 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductUnit]') AND [c].[name] = N'SalePrice');
    IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [ProductUnit] DROP CONSTRAINT [' + @var40 + '];');
    ALTER TABLE [ProductUnit] ALTER COLUMN [SalePrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var41 sysname;
    SELECT @var41 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductUnit]') AND [c].[name] = N'QuantityPerUnit');
    IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [ProductUnit] DROP CONSTRAINT [' + @var41 + '];');
    ALTER TABLE [ProductUnit] ALTER COLUMN [QuantityPerUnit] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var42 sysname;
    SELECT @var42 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductUnit]') AND [c].[name] = N'PurchasePrice');
    IF @var42 IS NOT NULL EXEC(N'ALTER TABLE [ProductUnit] DROP CONSTRAINT [' + @var42 + '];');
    ALTER TABLE [ProductUnit] ALTER COLUMN [PurchasePrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var43 sysname;
    SELECT @var43 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Product]') AND [c].[name] = N'TaxRate');
    IF @var43 IS NOT NULL EXEC(N'ALTER TABLE [Product] DROP CONSTRAINT [' + @var43 + '];');
    ALTER TABLE [Product] ALTER COLUMN [TaxRate] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var44 sysname;
    SELECT @var44 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Product]') AND [c].[name] = N'MiniQuantity');
    IF @var44 IS NOT NULL EXEC(N'ALTER TABLE [Product] DROP CONSTRAINT [' + @var44 + '];');
    ALTER TABLE [Product] ALTER COLUMN [MiniQuantity] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var45 sysname;
    SELECT @var45 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[JournalEntryLines]') AND [c].[name] = N'TaxAmount');
    IF @var45 IS NOT NULL EXEC(N'ALTER TABLE [JournalEntryLines] DROP CONSTRAINT [' + @var45 + '];');
    ALTER TABLE [JournalEntryLines] ALTER COLUMN [TaxAmount] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var46 sysname;
    SELECT @var46 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[JournalEntryLines]') AND [c].[name] = N'ForeignAmount');
    IF @var46 IS NOT NULL EXEC(N'ALTER TABLE [JournalEntryLines] DROP CONSTRAINT [' + @var46 + '];');
    ALTER TABLE [JournalEntryLines] ALTER COLUMN [ForeignAmount] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var47 sysname;
    SELECT @var47 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[JournalEntryLines]') AND [c].[name] = N'ExchangeRate');
    IF @var47 IS NOT NULL EXEC(N'ALTER TABLE [JournalEntryLines] DROP CONSTRAINT [' + @var47 + '];');
    ALTER TABLE [JournalEntryLines] ALTER COLUMN [ExchangeRate] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var48 sysname;
    SELECT @var48 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[JournalEntryLines]') AND [c].[name] = N'Debit');
    IF @var48 IS NOT NULL EXEC(N'ALTER TABLE [JournalEntryLines] DROP CONSTRAINT [' + @var48 + '];');
    ALTER TABLE [JournalEntryLines] ALTER COLUMN [Debit] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var49 sysname;
    SELECT @var49 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[JournalEntryLines]') AND [c].[name] = N'Credit');
    IF @var49 IS NOT NULL EXEC(N'ALTER TABLE [JournalEntryLines] DROP CONSTRAINT [' + @var49 + '];');
    ALTER TABLE [JournalEntryLines] ALTER COLUMN [Credit] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var50 sysname;
    SELECT @var50 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'UnitPrice');
    IF @var50 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var50 + '];');
    ALTER TABLE [InvoiceLine] ALTER COLUMN [UnitPrice] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var51 sysname;
    SELECT @var51 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'UnitCost');
    IF @var51 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var51 + '];');
    ALTER TABLE [InvoiceLine] ALTER COLUMN [UnitCost] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var52 sysname;
    SELECT @var52 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'TaxRate');
    IF @var52 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var52 + '];');
    ALTER TABLE [InvoiceLine] ALTER COLUMN [TaxRate] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var53 sysname;
    SELECT @var53 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'TaxAmount');
    IF @var53 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var53 + '];');
    ALTER TABLE [InvoiceLine] ALTER COLUMN [TaxAmount] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var54 sysname;
    SELECT @var54 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'QuantityPerUnitSnapshot');
    IF @var54 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var54 + '];');
    ALTER TABLE [InvoiceLine] ALTER COLUMN [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var55 sysname;
    SELECT @var55 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'Quantity');
    IF @var55 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var55 + '];');
    ALTER TABLE [InvoiceLine] ALTER COLUMN [Quantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var56 sysname;
    SELECT @var56 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'ProfitBeforeTax');
    IF @var56 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var56 + '];');
    ALTER TABLE [InvoiceLine] ALTER COLUMN [ProfitBeforeTax] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var57 sysname;
    SELECT @var57 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'Profit');
    IF @var57 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var57 + '];');
    ALTER TABLE [InvoiceLine] ALTER COLUMN [Profit] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var58 sysname;
    SELECT @var58 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'LineSubTotal');
    IF @var58 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var58 + '];');
    ALTER TABLE [InvoiceLine] ALTER COLUMN [LineSubTotal] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var59 sysname;
    SELECT @var59 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'BaseQuantity');
    IF @var59 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var59 + '];');
    ALTER TABLE [InvoiceLine] ALTER COLUMN [BaseQuantity] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var60 sysname;
    SELECT @var60 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'TotalTax');
    IF @var60 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var60 + '];');
    ALTER TABLE [Invoice] ALTER COLUMN [TotalTax] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var61 sysname;
    SELECT @var61 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'TotalCOGS');
    IF @var61 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var61 + '];');
    ALTER TABLE [Invoice] ALTER COLUMN [TotalCOGS] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var62 sysname;
    SELECT @var62 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'TotalAmount');
    IF @var62 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var62 + '];');
    ALTER TABLE [Invoice] ALTER COLUMN [TotalAmount] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var63 sysname;
    SELECT @var63 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'SubTotal');
    IF @var63 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var63 + '];');
    ALTER TABLE [Invoice] ALTER COLUMN [SubTotal] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var64 sysname;
    SELECT @var64 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'NetSales');
    IF @var64 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var64 + '];');
    ALTER TABLE [Invoice] ALTER COLUMN [NetSales] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var65 sysname;
    SELECT @var65 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'GrossProfit');
    IF @var65 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var65 + '];');
    ALTER TABLE [Invoice] ALTER COLUMN [GrossProfit] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var66 sysname;
    SELECT @var66 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'ExchangeRate');
    IF @var66 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var66 + '];');
    ALTER TABLE [Invoice] ALTER COLUMN [ExchangeRate] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var67 sysname;
    SELECT @var67 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'DiscountAmount');
    IF @var67 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var67 + '];');
    ALTER TABLE [Invoice] ALTER COLUMN [DiscountAmount] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var68 sysname;
    SELECT @var68 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FinancialTransaction]') AND [c].[name] = N'ExchangeRate');
    IF @var68 IS NOT NULL EXEC(N'ALTER TABLE [FinancialTransaction] DROP CONSTRAINT [' + @var68 + '];');
    ALTER TABLE [FinancialTransaction] ALTER COLUMN [ExchangeRate] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var69 sysname;
    SELECT @var69 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FinancialTransaction]') AND [c].[name] = N'Amount');
    IF @var69 IS NOT NULL EXEC(N'ALTER TABLE [FinancialTransaction] DROP CONSTRAINT [' + @var69 + '];');
    ALTER TABLE [FinancialTransaction] ALTER COLUMN [Amount] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var70 sysname;
    SELECT @var70 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ExchangeRates]') AND [c].[name] = N'Rate');
    IF @var70 IS NOT NULL EXEC(N'ALTER TABLE [ExchangeRates] DROP CONSTRAINT [' + @var70 + '];');
    ALTER TABLE [ExchangeRates] ALTER COLUMN [Rate] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var71 sysname;
    SELECT @var71 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employee]') AND [c].[name] = N'BasicSalary');
    IF @var71 IS NOT NULL EXEC(N'ALTER TABLE [Employee] DROP CONSTRAINT [' + @var71 + '];');
    ALTER TABLE [Employee] ALTER COLUMN [BasicSalary] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var72 sysname;
    SELECT @var72 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Currencies]') AND [c].[name] = N'ExchangeRate');
    IF @var72 IS NOT NULL EXEC(N'ALTER TABLE [Currencies] DROP CONSTRAINT [' + @var72 + '];');
    ALTER TABLE [Currencies] ALTER COLUMN [ExchangeRate] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var73 sysname;
    SELECT @var73 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Check]') AND [c].[name] = N'Amount');
    IF @var73 IS NOT NULL EXEC(N'ALTER TABLE [Check] DROP CONSTRAINT [' + @var73 + '];');
    ALTER TABLE [Check] ALTER COLUMN [Amount] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var74 sysname;
    SELECT @var74 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CashierSession]') AND [c].[name] = N'StatrBalance');
    IF @var74 IS NOT NULL EXEC(N'ALTER TABLE [CashierSession] DROP CONSTRAINT [' + @var74 + '];');
    ALTER TABLE [CashierSession] ALTER COLUMN [StatrBalance] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var75 sysname;
    SELECT @var75 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CashierSession]') AND [c].[name] = N'ExpectedClosingBalance');
    IF @var75 IS NOT NULL EXEC(N'ALTER TABLE [CashierSession] DROP CONSTRAINT [' + @var75 + '];');
    ALTER TABLE [CashierSession] ALTER COLUMN [ExpectedClosingBalance] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var76 sysname;
    SELECT @var76 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CashierSession]') AND [c].[name] = N'EndingBalance');
    IF @var76 IS NOT NULL EXEC(N'ALTER TABLE [CashierSession] DROP CONSTRAINT [' + @var76 + '];');
    ALTER TABLE [CashierSession] ALTER COLUMN [EndingBalance] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var77 sysname;
    SELECT @var77 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CashierSession]') AND [c].[name] = N'DifferenceAmount');
    IF @var77 IS NOT NULL EXEC(N'ALTER TABLE [CashierSession] DROP CONSTRAINT [' + @var77 + '];');
    ALTER TABLE [CashierSession] ALTER COLUMN [DifferenceAmount] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var78 sysname;
    SELECT @var78 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BankStatements]') AND [c].[name] = N'OpeningBalance');
    IF @var78 IS NOT NULL EXEC(N'ALTER TABLE [BankStatements] DROP CONSTRAINT [' + @var78 + '];');
    ALTER TABLE [BankStatements] ALTER COLUMN [OpeningBalance] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var79 sysname;
    SELECT @var79 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BankStatements]') AND [c].[name] = N'ClosingBalance');
    IF @var79 IS NOT NULL EXEC(N'ALTER TABLE [BankStatements] DROP CONSTRAINT [' + @var79 + '];');
    ALTER TABLE [BankStatements] ALTER COLUMN [ClosingBalance] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var80 sysname;
    SELECT @var80 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BankStatementLines]') AND [c].[name] = N'Amount');
    IF @var80 IS NOT NULL EXEC(N'ALTER TABLE [BankStatementLines] DROP CONSTRAINT [' + @var80 + '];');
    ALTER TABLE [BankStatementLines] ALTER COLUMN [Amount] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var81 sysname;
    SELECT @var81 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AccountOpeningBalances]') AND [c].[name] = N'Debit');
    IF @var81 IS NOT NULL EXEC(N'ALTER TABLE [AccountOpeningBalances] DROP CONSTRAINT [' + @var81 + '];');
    ALTER TABLE [AccountOpeningBalances] ALTER COLUMN [Debit] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    DECLARE @var82 sysname;
    SELECT @var82 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AccountOpeningBalances]') AND [c].[name] = N'Credit');
    IF @var82 IS NOT NULL EXEC(N'ALTER TABLE [AccountOpeningBalances] DROP CONSTRAINT [' + @var82 + '];');
    ALTER TABLE [AccountOpeningBalances] ALTER COLUMN [Credit] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260821133518_IncreaseDecimalPrecisionToSix', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825123502_AddPurchaseInvoiceLineDiscountAndFreeQuantity'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [FreeQuantity] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825123502_AddPurchaseInvoiceLineDiscountAndFreeQuantity'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [LineDiscountAmount] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825123502_AddPurchaseInvoiceLineDiscountAndFreeQuantity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825123502_AddPurchaseInvoiceLineDiscountAndFreeQuantity', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825144746_AddStockItemLineDiscountAndFreeQuantity'
)
BEGIN
    ALTER TABLE [StockItem] ADD [FreeQuantity] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825144746_AddStockItemLineDiscountAndFreeQuantity'
)
BEGIN
    ALTER TABLE [StockItem] ADD [LineDiscountAmount] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825144746_AddStockItemLineDiscountAndFreeQuantity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825144746_AddStockItemLineDiscountAndFreeQuantity', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827032526_AddInvoicePaymentAllocations'
)
BEGIN
    CREATE TABLE [InvoicePayments] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [PaymentType] int NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InvoicePayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoicePayments_Invoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoice] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827032526_AddInvoicePaymentAllocations'
)
BEGIN
    CREATE INDEX [IX_InvoicePayments_InvoiceId] ON [InvoicePayments] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827032526_AddInvoicePaymentAllocations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827032526_AddInvoicePaymentAllocations', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913222727_AddAuditLogs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260913222727_AddAuditLogs', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [Role] int NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [Action] nvarchar(200) NOT NULL,
        [PermissionKey] nvarchar(200) NULL,
        [EntityType] nvarchar(200) NULL,
        [EntityId] int NULL,
        [Succeeded] bit NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions'
)
BEGIN
    CREATE TABLE [PurchaseOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNumber] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [SupplierId] int NULL,
        [WarehouseId] int NULL,
        [CurrencyId] int NULL,
        [PaymentType] int NULL,
        [DocumentDate] datetime2 NULL,
        [DueDate] datetime2 NULL,
        [Notes] nvarchar(max) NULL,
        [DiscountAmount] decimal(18,6) NULL,
        [SubTotal] decimal(18,6) NOT NULL,
        [TotalTax] decimal(18,6) NOT NULL,
        [TotalAmount] decimal(18,6) NOT NULL,
        [CostPriceIncludesTax] bit NOT NULL,
        [CreatedBy] int NULL,
        [SubmittedBy] int NULL,
        [ApprovedBy] int NULL,
        [ApprovedDate] datetime2 NULL,
        [RejectedBy] int NULL,
        [RejectedDate] datetime2 NULL,
        [RejectionReason] nvarchar(max) NULL,
        [ConvertedInvoiceId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseOrders_User_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [User] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions'
)
BEGIN
    CREATE TABLE [UserPermissions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [PermissionKey] nvarchar(450) NOT NULL,
        [IsAllowed] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_UserPermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPermissions_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions'
)
BEGIN
    CREATE TABLE [PurchaseOrderLines] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseOrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ProductUnitId] int NOT NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL,
        [BaseQuantity] decimal(18,6) NOT NULL,
        [UnitPrice] decimal(18,6) NOT NULL,
        [UnitCost] decimal(18,6) NOT NULL,
        [LineDiscountAmount] decimal(18,6) NOT NULL,
        [FreeQuantity] decimal(18,6) NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [TaxExempt] bit NOT NULL,
        [TaxRate] decimal(18,6) NOT NULL,
        [TaxAmount] decimal(18,6) NOT NULL,
        [LineSubTotal] decimal(18,6) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedDate_UserId] ON [AuditLogs] ([CreatedDate], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderLines_PurchaseOrderId] ON [PurchaseOrderLines] ([PurchaseOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseOrders_OrderNumber] ON [PurchaseOrders] ([OrderNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserPermissions_UserId_PermissionKey] ON [UserPermissions] ([UserId], [PermissionKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012171306_AddPurchaseOrdersAndUserPermissions', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceAttachments] (
        [Id] int NOT NULL IDENTITY,
        [OwnerType] int NOT NULL,
        [OwnerId] int NOT NULL,
        [FileName] nvarchar(max) NOT NULL,
        [StoragePath] nvarchar(max) NOT NULL,
        [ContentType] nvarchar(max) NULL,
        [FileSize] bigint NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceAttachments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceBeneficiaryCategories] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [NameEnglish] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceBeneficiaryCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaimAdjustments] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceClaimId] int NOT NULL,
        [Type] int NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [IsAccountingPosted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimAdjustments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaimBatches] (
        [Id] int NOT NULL IDENTITY,
        [BatchNumber] nvarchar(max) NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [PeriodFrom] datetime2 NOT NULL,
        [PeriodTo] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [SubmittedDate] datetime2 NULL,
        [ExportFileName] nvarchar(max) NULL,
        [SubmittedAmount] decimal(18,6) NOT NULL,
        [ApprovedAmount] decimal(18,6) NOT NULL,
        [DeductionAmount] decimal(18,6) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimBatches] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaimLines] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceClaimId] int NOT NULL,
        [InvoiceLineId] int NOT NULL,
        [SubmittedAmount] decimal(18,6) NOT NULL,
        [ApprovedAmount] decimal(18,6) NOT NULL,
        [RejectedAmount] decimal(18,6) NOT NULL,
        [DeductionAmount] decimal(18,6) NOT NULL,
        [DeductionReason] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimLines] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaimPaymentAllocations] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceClaimPaymentId] int NOT NULL,
        [InsuranceClaimId] int NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimPaymentAllocations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaimPayments] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceCompanyId] int NOT NULL,
        [ReceiptNumber] nvarchar(max) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [FinancialTransactionId] int NULL,
        [ReferenceNumber] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimPayments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaims] (
        [Id] int NOT NULL IDENTITY,
        [ClaimNumber] nvarchar(450) NOT NULL,
        [InvoiceId] int NOT NULL,
        [InsuranceInvoiceCoverageId] int NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [InsuranceMemberPolicyId] int NOT NULL,
        [InsuranceClaimBatchId] int NULL,
        [Status] int NOT NULL,
        [SubmissionDate] datetime2 NULL,
        [DecisionDate] datetime2 NULL,
        [SubmittedAmount] decimal(18,6) NOT NULL,
        [ApprovedAmount] decimal(18,6) NOT NULL,
        [RejectedAmount] decimal(18,6) NOT NULL,
        [DeductionAmount] decimal(18,6) NOT NULL,
        [PaidAmount] decimal(18,6) NOT NULL,
        [OutstandingAmount] decimal(18,6) NOT NULL,
        [RejectionReason] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaims] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceCompanies] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [NameEnglish] nvarchar(max) NULL,
        [TaxNumber] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [ReceivableAccountId] int NULL,
        [DeductionAccountId] int NULL,
        [PaymentAccountId] int NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceCompanies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceContracts] (
        [Id] int NOT NULL IDENTITY,
        [ContractNumber] nvarchar(450) NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [NetworkId] int NULL,
        [Name] nvarchar(max) NOT NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [SettlementTerms] nvarchar(max) NULL,
        [PaymentDueDays] int NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceContracts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceContractVersions] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceContractId] int NOT NULL,
        [VersionNumber] int NOT NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceContractVersions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceCoverageRules] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceContractVersionId] int NOT NULL,
        [ProductId] int NULL,
        [CategoryId] int NULL,
        [ServiceCode] nvarchar(max) NULL,
        [CoveragePercent] decimal(18,6) NOT NULL,
        [CopayPercent] decimal(18,6) NOT NULL,
        [DeductibleAmount] decimal(18,6) NOT NULL,
        [PerClaimLimit] decimal(18,6) NULL,
        [AnnualLimit] decimal(18,6) NULL,
        [RequiresAuthorization] bit NOT NULL,
        [IsExcluded] bit NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceCoverageRules] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceExclusions] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceContractVersionId] int NOT NULL,
        [ProductId] int NULL,
        [CategoryId] int NULL,
        [ServiceCode] nvarchar(max) NULL,
        [Reason] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceExclusions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceInvoiceCoverages] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [InsuranceContractId] int NOT NULL,
        [InsuranceContractVersionId] int NOT NULL,
        [InsuranceMemberPolicyId] int NOT NULL,
        [GrossAmount] decimal(18,6) NOT NULL,
        [DiscountAmount] decimal(18,6) NOT NULL,
        [DeductibleAmount] decimal(18,6) NOT NULL,
        [CopayAmount] decimal(18,6) NOT NULL,
        [InsurerAmount] decimal(18,6) NOT NULL,
        [PatientAmount] decimal(18,6) NOT NULL,
        [SnapshotJson] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceInvoiceCoverages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceMemberPolicies] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [InsuranceContractId] int NOT NULL,
        [BeneficiaryCategoryId] int NULL,
        [MemberNumber] nvarchar(450) NOT NULL,
        [PolicyNumber] nvarchar(max) NULL,
        [CardNumber] nvarchar(max) NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [IsPrimary] bit NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceMemberPolicies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceNetworks] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [NameEnglish] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceNetworks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsurancePreAuthorizations] (
        [Id] int NOT NULL IDENTITY,
        [AuthorizationNumber] nvarchar(450) NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [InsuranceMemberPolicyId] int NOT NULL,
        [InvoiceId] int NULL,
        [Status] int NOT NULL,
        [RequestedDate] datetime2 NOT NULL,
        [DecisionDate] datetime2 NULL,
        [ValidTo] datetime2 NULL,
        [RequestedAmount] decimal(18,6) NOT NULL,
        [ApprovedAmount] decimal(18,6) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsurancePreAuthorizations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE INDEX [IX_InsuranceAttachments_OwnerType_OwnerId] ON [InsuranceAttachments] ([OwnerType], [OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceBeneficiaryCategories_Code] ON [InsuranceBeneficiaryCategories] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceClaimLines_InsuranceClaimId_InvoiceLineId] ON [InsuranceClaimLines] ([InsuranceClaimId], [InvoiceLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceClaimPaymentAllocations_InsuranceClaimPaymentId_InsuranceClaimId] ON [InsuranceClaimPaymentAllocations] ([InsuranceClaimPaymentId], [InsuranceClaimId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceClaims_ClaimNumber] ON [InsuranceClaims] ([ClaimNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE INDEX [IX_InsuranceClaims_InsuranceCompanyId_Status_SubmissionDate] ON [InsuranceClaims] ([InsuranceCompanyId], [Status], [SubmissionDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceCompanies_Code] ON [InsuranceCompanies] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceContracts_ContractNumber] ON [InsuranceContracts] ([ContractNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceContractVersions_InsuranceContractId_VersionNumber] ON [InsuranceContractVersions] ([InsuranceContractId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceInvoiceCoverages_InvoiceId] ON [InsuranceInvoiceCoverages] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE INDEX [IX_InsuranceMemberPolicies_InsuranceCompanyId_MemberNumber] ON [InsuranceMemberPolicies] ([InsuranceCompanyId], [MemberNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceNetworks_Code] ON [InsuranceNetworks] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsurancePreAuthorizations_AuthorizationNumber] ON [InsurancePreAuthorizations] ([AuthorizationNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013123927_AddInsuranceManagement', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceMemberPolicies] ADD [EligibilityStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceMemberPolicies] ADD [ExternalVerificationReference] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceMemberPolicies] ADD [InsurancePlanId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceMemberPolicies] ADD [LastVerificationMethod] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceMemberPolicies] ADD [LastVerifiedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceContracts] ADD [AdministratorId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceContracts] ADD [ContractualPayerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceCompanies] ADD [OrganizationType] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE TABLE [InsuranceEligibilityChecks] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceMemberPolicyId] int NOT NULL,
        [Status] int NOT NULL,
        [Method] int NOT NULL,
        [PerformedByUserId] int NULL,
        [CheckedAt] datetime2 NOT NULL,
        [ValidTo] datetime2 NULL,
        [ExternalReference] nvarchar(max) NULL,
        [ResponseSummary] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceEligibilityChecks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE TABLE [InsuranceInvoiceCoverageLines] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceInvoiceCoverageId] int NOT NULL,
        [InvoiceLineId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [ContractedUnitPrice] decimal(18,6) NOT NULL,
        [GrossAmount] decimal(18,6) NOT NULL,
        [CoveredAmount] decimal(18,6) NOT NULL,
        [PatientAmount] decimal(18,6) NOT NULL,
        [ExcludedAmount] decimal(18,6) NOT NULL,
        [DeductibleAmount] decimal(18,6) NOT NULL,
        [CopayAmount] decimal(18,6) NOT NULL,
        [ApprovedQuantity] decimal(18,6) NOT NULL,
        [RequiresAuthorization] bit NOT NULL,
        [IsExcluded] bit NOT NULL,
        [DecisionReason] nvarchar(max) NULL,
        [InsurancePreAuthorizationId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceInvoiceCoverageLines] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE TABLE [InsurancePlans] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceContractId] int NOT NULL,
        [InsuranceContractVersionId] int NULL,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [NameEnglish] nvarchar(max) NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsurancePlans] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE TABLE [InsurancePreAuthorizationLines] (
        [Id] int NOT NULL IDENTITY,
        [InsurancePreAuthorizationId] int NOT NULL,
        [ProductId] int NOT NULL,
        [RequestedQuantity] decimal(18,6) NOT NULL,
        [ApprovedQuantity] decimal(18,6) NOT NULL,
        [RequestedAmount] decimal(18,6) NOT NULL,
        [ApprovedAmount] decimal(18,6) NOT NULL,
        [Status] int NOT NULL,
        [RejectionReason] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsurancePreAuthorizationLines] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE INDEX [IX_InsuranceEligibilityChecks_InsuranceMemberPolicyId_CheckedAt] ON [InsuranceEligibilityChecks] ([InsuranceMemberPolicyId], [CheckedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceInvoiceCoverageLines_InsuranceInvoiceCoverageId_InvoiceLineId] ON [InsuranceInvoiceCoverageLines] ([InsuranceInvoiceCoverageId], [InvoiceLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsurancePlans_InsuranceContractId_Code] ON [InsurancePlans] ([InsuranceContractId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsurancePreAuthorizationLines_InsurancePreAuthorizationId_ProductId] ON [InsurancePreAuthorizationLines] ([InsurancePreAuthorizationId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013132624_AddInsuranceEnhancements', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133107_AddInsurancePlanCoverageScope'
)
BEGIN
    ALTER TABLE [InsuranceExclusions] ADD [InsurancePlanId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133107_AddInsurancePlanCoverageScope'
)
BEGIN
    ALTER TABLE [InsuranceCoverageRules] ADD [InsurancePlanId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133107_AddInsurancePlanCoverageScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013133107_AddInsurancePlanCoverageScope', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InsuranceCompanyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InsuranceContractId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InsuranceContractVersionId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InsuranceMemberPolicyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InsuranceResponsibility] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [IsInsuranceSale] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [PatientResponsibility] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013133255_AddInsuranceInvoiceBillingFields', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021122000_AddPosProductCatalog'
)
BEGIN
    CREATE TABLE [PosProductCatalogEntry] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [ProductUnitId] int NOT NULL,
        [SubCategoryId] int NOT NULL,
        [ProductName] nvarchar(450) NULL,
        [ItemCode] bigint NULL,
        [TaxExempt] bit NULL,
        [TaxRate] decimal(18,6) NULL,
        [AlternateBarcode] nvarchar(450) NULL,
        [UnitName] nvarchar(max) NULL,
        [UnitSalePrice] decimal(18,6) NOT NULL,
        [UnitPurchasePrice] decimal(18,6) NOT NULL,
        [QuantityPerUnit] decimal(18,6) NOT NULL,
        [AllowFractionalSale] bit NOT NULL,
        [FractionalPartsPerUnit] decimal(18,6) NOT NULL,
        [FractionalPartName] nvarchar(max) NULL,
        [IsBaseUnit] bit NOT NULL,
        [IsDefaultSaleUnit] bit NOT NULL,
        [IsDefaultPurchaseUnit] bit NOT NULL,
        [SupplierId] int NULL,
        [SupplierName] nvarchar(max) NULL,
        [StockQuantity] decimal(18,6) NOT NULL,
        [StockPurchasePrice] decimal(18,6) NOT NULL,
        [StockSalePrice] decimal(18,6) NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [AvailableToSale] bit NOT NULL,
        [StockStatus] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PosProductCatalogEntry] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021122000_AddPosProductCatalog'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PosProductCatalogEntry_ProductId_ProductUnitId] ON [PosProductCatalogEntry] ([ProductId], [ProductUnitId]) WHERE [ProductId] IS NOT NULL AND [ProductUnitId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021122000_AddPosProductCatalog'
)
BEGIN
    CREATE INDEX [IX_PosProductCatalogEntry_AlternateBarcode] ON [PosProductCatalogEntry] ([AlternateBarcode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021122000_AddPosProductCatalog'
)
BEGIN
    CREATE INDEX [IX_PosProductCatalogEntry_ProductName] ON [PosProductCatalogEntry] ([ProductName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021122000_AddPosProductCatalog'
)
BEGIN
    CREATE INDEX [IX_PosProductCatalogEntry_AvailableToSale] ON [PosProductCatalogEntry] ([AvailableToSale]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021122000_AddPosProductCatalog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021122000_AddPosProductCatalog', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    DROP INDEX [IX_StockLot_ProductId] ON [StockLot];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    DROP INDEX [IX_ProductUnit_ProductId] ON [ProductUnit];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [PurchaseOrderLines] ADD [PurchaseRequestLineId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    DECLARE @var83 sysname;
    SELECT @var83 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductUnit]') AND [c].[name] = N'AlternateBarcode');
    IF @var83 IS NOT NULL EXEC(N'ALTER TABLE [ProductUnit] DROP CONSTRAINT [' + @var83 + '];');
    ALTER TABLE [ProductUnit] ALTER COLUMN [AlternateBarcode] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [ProductUnit] ADD [AllowFractionalSale] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [ProductUnit] ADD [FractionalPartName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [ProductUnit] ADD [FractionalPartsPerUnit] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    DECLARE @var84 sysname;
    SELECT @var84 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Product]') AND [c].[name] = N'Name');
    IF @var84 IS NOT NULL EXEC(N'ALTER TABLE [Product] DROP CONSTRAINT [' + @var84 + '];');
    ALTER TABLE [Product] ALTER COLUMN [Name] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [Product] ADD [AgentName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [Product] ADD [SupplierId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [InsuranceInvoiceCoverageLines] ADD [InsuranceDiscountAmount] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [InsuranceInvoiceCoverageLines] ADD [InsuranceDiscountPercent] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [InsuranceCoverageRules] ADD [DiscountPercent] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [InsuranceCoverageRules] ADD [FixedDiscountAmount] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [InsuranceCoverageRules] ADD [ValidFrom] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [InsuranceCoverageRules] ADD [ValidTo] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [ClosedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [ClosingNotes] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [CountedVisa] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [ExpectedVisa] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [SettlementCalculatedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [SettlementCashDifference] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [SettlementCountedCash] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [SettlementCountedVisa] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [SettlementExpectedCash] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [SettlementExpectedVisa] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [SettlementLocked] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [SettlementVisaDifference] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [CashierSession] ADD [VisaDifference] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [GoodsReceipts] (
        [Id] int NOT NULL IDENTITY,
        [ReceiptNumber] nvarchar(450) NOT NULL,
        [PurchaseOrderId] int NOT NULL,
        [SupplierId] int NULL,
        [WarehouseId] int NULL,
        [BranchId] int NULL,
        [ReceivedBy] int NULL,
        [ReceiptDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [ConfirmedDate] datetime2 NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_GoodsReceipts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [PurchaseControlEntries] (
        [Id] int NOT NULL IDENTITY,
        [PaymentMethod] nvarchar(max) NULL,
        [PurchaseOrderNumber] nvarchar(max) NULL,
        [PurchaseOrderDate] datetime2 NULL,
        [PurchaseRequestNumber] nvarchar(max) NULL,
        [SupplierName] nvarchar(max) NULL,
        [PurchaseOrderAmount] decimal(18,6) NULL,
        [InvoiceNumber] nvarchar(max) NULL,
        [InvoiceDate] datetime2 NULL,
        [DeliveryDate] datetime2 NULL,
        [Description] nvarchar(max) NULL,
        [Category] nvarchar(max) NULL,
        [PaidAmount] decimal(18,6) NULL,
        [DueAmount] decimal(18,6) NULL,
        [ReceiptAmount] decimal(18,6) NULL,
        [PaymentStatus] nvarchar(max) NULL,
        [DueDate] datetime2 NULL,
        [LinkCheck] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [OverdueDays] int NULL,
        [ReceivedValue] decimal(18,6) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseControlEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [PurchaseInvoiceMatches] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [PurchaseOrderId] int NULL,
        [SupplierId] int NULL,
        [SupplierInvoiceNumber] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [PriceTolerance] decimal(18,6) NOT NULL,
        [QuantityTolerance] decimal(18,6) NOT NULL,
        [DiscrepancyReason] nvarchar(max) NULL,
        [MatchedDate] datetime2 NULL,
        [MatchedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseInvoiceMatches] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [PurchaseRequests] (
        [Id] int NOT NULL IDENTITY,
        [RequestNumber] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [Category] int NOT NULL,
        [RequestDate] datetime2 NOT NULL,
        [RequesterId] int NULL,
        [RequesterName] nvarchar(200) NULL,
        [BranchId] int NULL,
        [Department] nvarchar(max) NULL,
        [Priority] nvarchar(max) NULL,
        [Justification] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [SubmittedBy] int NULL,
        [SubmittedDate] datetime2 NULL,
        [ApprovedBy] int NULL,
        [ApprovedDate] datetime2 NULL,
        [RejectedBy] int NULL,
        [RejectedDate] datetime2 NULL,
        [RejectionReason] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [PurchaseReturns] (
        [Id] int NOT NULL IDENTITY,
        [ReturnNumber] nvarchar(450) NOT NULL,
        [SupplierId] int NULL,
        [OriginalInvoiceId] int NULL,
        [GoodsReceiptId] int NULL,
        [CashierSessionId] int NULL,
        [Status] int NOT NULL,
        [Reason] nvarchar(max) NULL,
        [ReturnDate] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [ConfirmedBy] int NULL,
        [ConfirmedDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseReturns] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [SupplierAdvances] (
        [Id] int NOT NULL IDENTITY,
        [VoucherId] int NOT NULL,
        [SupplierId] int NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [AllocatedAmount] decimal(18,6) NOT NULL,
        [AdvanceDate] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_SupplierAdvances] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [SupplierPaymentAllocations] (
        [Id] int NOT NULL IDENTITY,
        [VoucherId] int NOT NULL,
        [InvoiceId] int NOT NULL,
        [SupplierId] int NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [AllocationDate] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_SupplierPaymentAllocations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [GoodsReceiptLines] (
        [Id] int NOT NULL IDENTITY,
        [GoodsReceiptId] int NOT NULL,
        [PurchaseOrderLineId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ProductUnitId] int NOT NULL,
        [QuantityShipped] decimal(18,6) NOT NULL,
        [QuantityReceived] decimal(18,6) NOT NULL,
        [QuantityAccepted] decimal(18,6) NOT NULL,
        [QuantityRejected] decimal(18,6) NOT NULL,
        [QuantityDamaged] decimal(18,6) NOT NULL,
        [FreeQuantity] decimal(18,6) NOT NULL,
        [BatchNumber] nvarchar(max) NULL,
        [ExpiryDate] datetime2 NULL,
        [StorageLocation] nvarchar(max) NULL,
        [RejectionReason] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_GoodsReceiptLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId] FOREIGN KEY ([GoodsReceiptId]) REFERENCES [GoodsReceipts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [PurchaseInvoiceMatchLines] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseInvoiceMatchId] int NOT NULL,
        [InvoiceLineId] int NOT NULL,
        [PurchaseOrderLineId] int NOT NULL,
        [InvoicedQuantity] decimal(18,6) NOT NULL,
        [AcceptedQuantity] decimal(18,6) NOT NULL,
        [InvoicedUnitPrice] decimal(18,6) NOT NULL,
        [OrderedUnitPrice] decimal(18,6) NOT NULL,
        [PriceVariance] decimal(18,6) NOT NULL,
        [QuantityVariance] decimal(18,6) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseInvoiceMatchLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseInvoiceMatchLines_PurchaseInvoiceMatches_PurchaseInvoiceMatchId] FOREIGN KEY ([PurchaseInvoiceMatchId]) REFERENCES [PurchaseInvoiceMatches] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [PurchaseRequestLines] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseRequestId] int NOT NULL,
        [ProductId] int NULL,
        [ProductUnitId] int NULL,
        [Barcode] nvarchar(100) NULL,
        [ItemName] nvarchar(500) NOT NULL,
        [UnitName] nvarchar(100) NULL,
        [UnitPrice] decimal(18,6) NOT NULL,
        [SupplierId] int NULL,
        [SupplierName] nvarchar(200) NULL,
        [Description] nvarchar(max) NULL,
        [RequestedQuantity] decimal(18,6) NOT NULL,
        [ApprovedQuantity] decimal(18,6) NOT NULL,
        [AvailableStockSnapshot] decimal(18,6) NOT NULL,
        [ReservedStockSnapshot] decimal(18,6) NOT NULL,
        [AlreadyOrderedQuantitySnapshot] decimal(18,6) NOT NULL,
        [MinimumStockSnapshot] decimal(18,6) NOT NULL,
        [AverageSalesSnapshot] decimal(18,6) NOT NULL,
        [LastPurchaseDateSnapshot] datetime2 NULL,
        [ExpectedStockoutDate] datetime2 NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseRequestLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseRequestLines_PurchaseRequests_PurchaseRequestId] FOREIGN KEY ([PurchaseRequestId]) REFERENCES [PurchaseRequests] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE TABLE [PurchaseReturnLines] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseReturnId] int NOT NULL,
        [GoodsReceiptLineId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ProductUnitId] int NOT NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [BatchNumber] nvarchar(max) NULL,
        [ExpiryDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseReturnLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseReturnLines_PurchaseReturns_PurchaseReturnId] FOREIGN KEY ([PurchaseReturnId]) REFERENCES [PurchaseReturns] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE INDEX [IX_StockLot_ProductId_Status_RemainingQuantity] ON [StockLot] ([ProductId], [Status], [RemainingQuantity]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderLines_PurchaseRequestLineId] ON [PurchaseOrderLines] ([PurchaseRequestLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE INDEX [IX_ProductUnit_AlternateBarcode] ON [ProductUnit] ([AlternateBarcode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE INDEX [IX_ProductUnit_ProductId_AlternateBarcode] ON [ProductUnit] ([ProductId], [AlternateBarcode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE INDEX [IX_Product_ITEMCODE] ON [Product] ([ITEMCODE]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE INDEX [IX_Product_Name] ON [Product] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE INDEX [IX_Product_SupplierId] ON [Product] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE INDEX [IX_GoodsReceiptLines_GoodsReceiptId_PurchaseOrderLineId] ON [GoodsReceiptLines] ([GoodsReceiptId], [PurchaseOrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GoodsReceipts_ReceiptNumber] ON [GoodsReceipts] ([ReceiptNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseInvoiceMatches_InvoiceId] ON [PurchaseInvoiceMatches] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseInvoiceMatchLines_PurchaseInvoiceMatchId_InvoiceLineId] ON [PurchaseInvoiceMatchLines] ([PurchaseInvoiceMatchId], [InvoiceLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PurchaseRequestLines_PurchaseRequestId_ProductId_ProductUnitId] ON [PurchaseRequestLines] ([PurchaseRequestId], [ProductId], [ProductUnitId]) WHERE [ProductId] IS NOT NULL AND [ProductUnitId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseRequests_RequestNumber] ON [PurchaseRequests] ([RequestNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE INDEX [IX_PurchaseReturnLines_PurchaseReturnId] ON [PurchaseReturnLines] ([PurchaseReturnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseReturns_ReturnNumber] ON [PurchaseReturns] ([ReturnNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SupplierAdvances_VoucherId] ON [SupplierAdvances] ([VoucherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE INDEX [IX_SupplierPaymentAllocations_InvoiceId] ON [SupplierPaymentAllocations] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SupplierPaymentAllocations_VoucherId_InvoiceId] ON [SupplierPaymentAllocations] ([VoucherId], [InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    ALTER TABLE [Product] ADD CONSTRAINT [FK_Product_User_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021194648_AddPurchaseRequestDetails', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022120000_AddInsuranceCompanyContractualDiscount'
)
BEGIN
    ALTER TABLE [InsuranceCompanies] ADD [ContractualDiscountPercent] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022120000_AddInsuranceCompanyContractualDiscount'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022120000_AddInsuranceCompanyContractualDiscount', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022160000_AddPosInsuranceBilling'
)
BEGIN
    DECLARE @var85 sysname;
    SELECT @var85 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InsuranceInvoiceCoverages]') AND [c].[name] = N'InsuranceContractId');
    IF @var85 IS NOT NULL EXEC(N'ALTER TABLE [InsuranceInvoiceCoverages] DROP CONSTRAINT [' + @var85 + '];');
    ALTER TABLE [InsuranceInvoiceCoverages] ALTER COLUMN [InsuranceContractId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022160000_AddPosInsuranceBilling'
)
BEGIN
    DECLARE @var86 sysname;
    SELECT @var86 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InsuranceInvoiceCoverages]') AND [c].[name] = N'InsuranceContractVersionId');
    IF @var86 IS NOT NULL EXEC(N'ALTER TABLE [InsuranceInvoiceCoverages] DROP CONSTRAINT [' + @var86 + '];');
    ALTER TABLE [InsuranceInvoiceCoverages] ALTER COLUMN [InsuranceContractVersionId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022160000_AddPosInsuranceBilling'
)
BEGIN
    DECLARE @var87 sysname;
    SELECT @var87 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InsuranceInvoiceCoverages]') AND [c].[name] = N'InsuranceMemberPolicyId');
    IF @var87 IS NOT NULL EXEC(N'ALTER TABLE [InsuranceInvoiceCoverages] DROP CONSTRAINT [' + @var87 + '];');
    ALTER TABLE [InsuranceInvoiceCoverages] ALTER COLUMN [InsuranceMemberPolicyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022160000_AddPosInsuranceBilling'
)
BEGIN
    DECLARE @var88 sysname;
    SELECT @var88 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InsuranceClaims]') AND [c].[name] = N'InsuranceMemberPolicyId');
    IF @var88 IS NOT NULL EXEC(N'ALTER TABLE [InsuranceClaims] DROP CONSTRAINT [' + @var88 + '];');
    ALTER TABLE [InsuranceClaims] ALTER COLUMN [InsuranceMemberPolicyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022160000_AddPosInsuranceBilling'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022160000_AddPosInsuranceBilling', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023010000_AddPharmacyPurchaseBonus'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [BonusRatio] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023010000_AddPharmacyPurchaseBonus'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [PurchaseAcquisitionCost] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023010000_AddPharmacyPurchaseBonus'
)
BEGIN
    ALTER TABLE [InvoiceLine] ADD [SourcePurchaseLineId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023010000_AddPharmacyPurchaseBonus'
)
BEGIN
    CREATE INDEX [IX_InvoiceLine_SourcePurchaseLineId] ON [InvoiceLine] ([SourcePurchaseLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023010000_AddPharmacyPurchaseBonus'
)
BEGIN
    ALTER TABLE [StockLot] ADD [PurchaseAcquisitionCost] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023010000_AddPharmacyPurchaseBonus'
)
BEGIN
    ALTER TABLE [StockLot] ADD [SourcePurchaseLineId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023010000_AddPharmacyPurchaseBonus'
)
BEGIN
    CREATE INDEX [IX_StockLot_SourcePurchaseLineId] ON [StockLot] ([SourcePurchaseLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023010000_AddPharmacyPurchaseBonus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023010000_AddPharmacyPurchaseBonus', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Employee WHERE DATALENGTH(NationalId)>200)
        THROW 51000, 'Employee NationalId values exceed 100 characters; resolve manually before migration.', 1;
    IF EXISTS (SELECT NationalId FROM dbo.Employee WHERE NationalId IS NOT NULL AND NationalId<>N'' GROUP BY NationalId HAVING COUNT(*)>1)
        THROW 51001, 'Duplicate employee NationalId values must be resolved manually; no records were changed.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    DECLARE @var89 sysname;
    SELECT @var89 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employee]') AND [c].[name] = N'NationalId');
    IF @var89 IS NOT NULL EXEC(N'ALTER TABLE [Employee] DROP CONSTRAINT [' + @var89 + '];');
    ALTER TABLE [Employee] ALTER COLUMN [NationalId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    ALTER TABLE [Employee] ADD [EmploymentType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    ALTER TABLE [Employee] ADD [SocialSecurityNumberEncrypted] nvarchar(512) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [EmployeeEmploymentHistory] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [BeforeJson] nvarchar(max) NOT NULL,
        [AfterJson] nvarchar(max) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [ChangedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeeEmploymentHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeEmploymentHistory_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [EmployeeSalaryVersion] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [BasicMonthlySalary] decimal(18,6) NOT NULL,
        [FixedAllowances] decimal(18,6) NOT NULL,
        [StandardDailyHours] decimal(18,6) NOT NULL,
        [WorkingDaysPerWeek] int NOT NULL,
        [StandardMonthlyHours] decimal(18,6) NOT NULL,
        [HourlyRateMethod] int NOT NULL,
        [OvertimeMode] int NOT NULL,
        [NormalOvertimeMultiplier] decimal(18,6) NOT NULL,
        [HolidayOvertimeMultiplier] decimal(18,6) NOT NULL,
        [SocialSecurityCovered] bit NOT NULL,
        [SocialSecurityEligibleWage] decimal(18,6) NULL,
        [ContributionCategory] nvarchar(50) NOT NULL,
        [ChangeReason] nvarchar(1000) NOT NULL,
        [ChangedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeeSalaryVersion] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeSalaryVersion_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [PayrollPolicy] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [EffectiveTo] datetime2 NOT NULL,
        [SalaryDayDivisor] decimal(18,6) NOT NULL,
        [EmployeeSscPercent] decimal(18,6) NOT NULL,
        [EmployerSscPercent] decimal(18,6) NOT NULL,
        [InsurableWageCeiling] decimal(18,6) NOT NULL,
        [DefaultDailyHours] decimal(18,6) NOT NULL,
        [DefaultWorkingDaysPerWeek] int NOT NULL,
        [DefaultMonthlyHours] decimal(18,6) NOT NULL,
        [LegalDailyHours] decimal(18,6) NOT NULL,
        [LegalWeeklyHours] decimal(18,6) NOT NULL,
        [MinimumNormalMultiplier] decimal(18,6) NOT NULL,
        [MinimumHolidayMultiplier] decimal(18,6) NOT NULL,
        [AdvanceDeductionMaximumPercent] decimal(18,6) NOT NULL,
        [WeekStartsOn] int NOT NULL,
        [ProrationMethod] int NOT NULL,
        [IncludeFixedAllowancesInWageBasis] bit NOT NULL,
        [IsLegallyReviewed] bit NOT NULL,
        [SourceReference] nvarchar(2000) NOT NULL,
        [ReviewedBy] int NULL,
        [ReviewReason] nvarchar(1000) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PayrollPolicy] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [EmployeePayroll] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Year] int NOT NULL,
        [Month] int NOT NULL,
        [SalaryVersionId] int NOT NULL,
        [PolicyId] int NOT NULL,
        [State] int NOT NULL,
        [Revision] uniqueidentifier NOT NULL,
        [InputsJson] nvarchar(max) NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [ApprovedAt] datetime2 NULL,
        [ApprovedBy] int NULL,
        [ModifiedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeePayroll] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeePayroll_EmployeeSalaryVersion_SalaryVersionId] FOREIGN KEY ([SalaryVersionId]) REFERENCES [EmployeeSalaryVersion] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeePayroll_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeePayroll_PayrollPolicy_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [PayrollPolicy] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [PayrollAdjustment] (
        [Id] int NOT NULL IDENTITY,
        [EmployeePayrollId] int NOT NULL,
        [Kind] int NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [DeductionBasis] int NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [LegalReference] nvarchar(1000) NULL,
        [Notes] nvarchar(2000) NULL,
        [AuthorizedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PayrollAdjustment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollAdjustment_EmployeePayroll_EmployeePayrollId] FOREIGN KEY ([EmployeePayrollId]) REFERENCES [EmployeePayroll] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [PayrollCorrection] (
        [Id] int NOT NULL IDENTITY,
        [EmployeePayrollId] int NOT NULL,
        [OriginalRevision] uniqueidentifier NOT NULL,
        [OriginalInputsJson] nvarchar(max) NOT NULL,
        [OriginalSnapshotJson] nvarchar(max) NOT NULL,
        [OriginalAdjustmentsJson] nvarchar(max) NOT NULL,
        [OriginalApprovedAt] datetime2 NULL,
        [OriginalApprovedBy] int NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [CorrectedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PayrollCorrection] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollCorrection_EmployeePayroll_EmployeePayrollId] FOREIGN KEY ([EmployeePayrollId]) REFERENCES [EmployeePayroll] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Employee_NationalId] ON [Employee] ([NationalId]) WHERE [NationalId] IS NOT NULL AND [NationalId] <> N''''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE INDEX [IX_EmployeeEmploymentHistory_EmployeeId] ON [EmployeeEmploymentHistory] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeePayroll_EmployeeId_Year_Month] ON [EmployeePayroll] ([EmployeeId], [Year], [Month]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE INDEX [IX_EmployeePayroll_PolicyId] ON [EmployeePayroll] ([PolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE INDEX [IX_EmployeePayroll_SalaryVersionId] ON [EmployeePayroll] ([SalaryVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeeSalaryVersion_EmployeeId_EffectiveFrom] ON [EmployeeSalaryVersion] ([EmployeeId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE INDEX [IX_PayrollAdjustment_EmployeePayrollId] ON [PayrollAdjustment] ([EmployeePayrollId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE INDEX [IX_PayrollCorrection_EmployeePayrollId] ON [PayrollCorrection] ([EmployeePayrollId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PayrollPolicy_Category_EffectiveFrom] ON [PayrollPolicy] ([Category], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.PayrollPolicy WHERE Category=N'JO-PRIVATE-GENERAL' AND EffectiveFrom='20260101')
    INSERT INTO dbo.PayrollPolicy
    (Name,Category,EffectiveFrom,EffectiveTo,SalaryDayDivisor,EmployeeSscPercent,EmployerSscPercent,InsurableWageCeiling,
     DefaultDailyHours,DefaultWorkingDaysPerWeek,DefaultMonthlyHours,LegalDailyHours,LegalWeeklyHours,MinimumNormalMultiplier,MinimumHolidayMultiplier,
     AdvanceDeductionMaximumPercent,WeekStartsOn,ProrationMethod,IncludeFixedAllowancesInWageBasis,IsLegallyReviewed,SourceReference,ReviewedBy,ReviewReason,CreatedDate,UpdatedDate)
    VALUES
    (N'الأردن — القطاع الخاص العام — ٢٠٢٦',N'JO-PRIVATE-GENERAL','20260101','20261231',30,7.5,14.25,3733,
     8,6,208,8,48,1.25,1.50,10,6,1,0,0,N'https://www.ssc.gov.jo/wp-content/uploads/2021/10/قانون-الضمان-الاجتماعي-رقم-١-لسنة-٢٠١٤-1.pdf ; https://www.mol.gov.jo/AR/List/القوانين ; https://org.petra.gov.jo/Include/InnerPage.jsp?ID=348404&lang=ar&name=news ; قاسم اليوم وطريقة الإجازة يلزمان مراجعة العقد والسياسة، ولا يعتمدان تلقائياً.',NULL,NULL,SYSDATETIME(),SYSDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023072627_AddJordanEmployeePayroll', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [JordanEInvoiceAttemptCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [JordanEInvoiceLastAttemptAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [JordanEInvoiceQr] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [JordanEInvoiceReference] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [JordanEInvoiceResponse] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [JordanEInvoiceStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [JordanEInvoiceSubmittedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [SupplierJoFotaraNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [SupplierJoFotaraQr] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [SupplierJoFotaraUuid] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026060636_AddJordanEInvoiceFields', N'9.0.6');
END;

COMMIT;
GO

