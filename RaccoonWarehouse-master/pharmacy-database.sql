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

    IF OBJECT_ID(N'dbo.[TaxRates]', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.[TaxRates]
        (
            [Id] int NOT NULL IDENTITY(1,1),
            [Name] nvarchar(max) NOT NULL,
            [Rate] decimal(18,6) NOT NULL,
            [TaxAccountId] int NOT NULL,
            [TaxType] int NOT NULL,
            [IsActive] bit NOT NULL,
            [CreatedDate] datetime2 NOT NULL,
            [UpdatedDate] datetime2 NOT NULL,
            CONSTRAINT [PK_TaxRates] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_TaxRates_Accounts_TaxAccountId]
                FOREIGN KEY ([TaxAccountId]) REFERENCES dbo.[Accounts] ([Id])
        );

        CREATE INDEX [IX_TaxRates_TaxAccountId]
            ON dbo.[TaxRates] ([TaxAccountId]);
    END;

    IF COL_LENGTH(N'dbo.[JournalEntryLines]', N'TaxRateId') IS NULL
        ALTER TABLE dbo.[JournalEntryLines] ADD [TaxRateId] int NULL;

    IF COL_LENGTH(N'dbo.[JournalEntryLines]', N'TaxAmount') IS NULL
        ALTER TABLE dbo.[JournalEntryLines] ADD [TaxAmount] decimal(18,2) NULL;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_JournalEntryLines_TaxRateId'
          AND [object_id] = OBJECT_ID(N'dbo.[JournalEntryLines]')
    )
        CREATE INDEX [IX_JournalEntryLines_TaxRateId]
            ON dbo.[JournalEntryLines] ([TaxRateId]);

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE [name] = N'FK_JournalEntryLines_TaxRates_TaxRateId'
          AND [parent_object_id] = OBJECT_ID(N'dbo.[JournalEntryLines]')
    )
        ALTER TABLE dbo.[JournalEntryLines]
            ADD CONSTRAINT [FK_JournalEntryLines_TaxRates_TaxRateId]
            FOREIGN KEY ([TaxRateId]) REFERENCES dbo.[TaxRates] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix'
)
BEGIN

    IF COL_LENGTH(N'dbo.[User]', N'OpeningBalance') IS NULL
        ALTER TABLE dbo.[User]
        ADD [OpeningBalance] decimal(18,2) NOT NULL
            CONSTRAINT [DF_User_OpeningBalance] DEFAULT (0);

    IF COL_LENGTH(N'dbo.[User]', N'CurrentBalance') IS NULL
        ALTER TABLE dbo.[User]
        ADD [CurrentBalance] decimal(18,2) NOT NULL
            CONSTRAINT [DF_User_CurrentBalance] DEFAULT (0);

    IF COL_LENGTH(N'dbo.[User]', N'CreditLimit') IS NULL
        ALTER TABLE dbo.[User]
        ADD [CreditLimit] decimal(18,2) NOT NULL
            CONSTRAINT [DF_User_CreditLimit] DEFAULT (0);
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

COMMIT;
GO

