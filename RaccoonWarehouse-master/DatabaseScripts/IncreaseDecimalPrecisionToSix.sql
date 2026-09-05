BEGIN TRANSACTION;
DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Voucher]') AND [c].[name] = N'ExchangeRate');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Voucher] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Voucher] ALTER COLUMN [ExchangeRate] decimal(18,6) NOT NULL;

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Voucher]') AND [c].[name] = N'Amount');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Voucher] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Voucher] ALTER COLUMN [Amount] decimal(18,6) NOT NULL;

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[User]') AND [c].[name] = N'OpeningBalance');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [User] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [User] ALTER COLUMN [OpeningBalance] decimal(18,6) NOT NULL;

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[User]') AND [c].[name] = N'CurrentBalance');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [User] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [User] ALTER COLUMN [CurrentBalance] decimal(18,6) NOT NULL;

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[User]') AND [c].[name] = N'CreditLimit');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [User] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [User] ALTER COLUMN [CreditLimit] decimal(18,6) NOT NULL;

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TaxRates]') AND [c].[name] = N'Rate');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [TaxRates] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [TaxRates] ALTER COLUMN [Rate] decimal(18,6) NOT NULL;

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockTransaction]') AND [c].[name] = N'UnitPrice');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [StockTransaction] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [StockTransaction] ALTER COLUMN [UnitPrice] decimal(18,6) NOT NULL;

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockTransaction]') AND [c].[name] = N'QuantityPerUnitSnapshot');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [StockTransaction] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [StockTransaction] ALTER COLUMN [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL;

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockTransaction]') AND [c].[name] = N'Quantity');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [StockTransaction] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [StockTransaction] ALTER COLUMN [Quantity] decimal(18,6) NOT NULL;

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockTransaction]') AND [c].[name] = N'BaseQuantity');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [StockTransaction] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [StockTransaction] ALTER COLUMN [BaseQuantity] decimal(18,6) NOT NULL;

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'SalePrice');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [StockLot] ALTER COLUMN [SalePrice] decimal(18,6) NOT NULL;

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'RemainingQuantity');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [StockLot] ALTER COLUMN [RemainingQuantity] decimal(18,6) NOT NULL;

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'RemainingBaseQuantity');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [StockLot] ALTER COLUMN [RemainingBaseQuantity] decimal(18,6) NOT NULL;

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'QuantityPerUnitSnapshot');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [StockLot] ALTER COLUMN [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL;

DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'Quantity');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var15 + '];');
ALTER TABLE [StockLot] ALTER COLUMN [Quantity] decimal(18,6) NOT NULL;

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'PurchasePrice');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [StockLot] ALTER COLUMN [PurchasePrice] decimal(18,6) NOT NULL;

DECLARE @var17 sysname;
SELECT @var17 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockLot]') AND [c].[name] = N'BaseQuantity');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [StockLot] DROP CONSTRAINT [' + @var17 + '];');
ALTER TABLE [StockLot] ALTER COLUMN [BaseQuantity] decimal(18,6) NOT NULL;

DECLARE @var18 sysname;
SELECT @var18 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockItem]') AND [c].[name] = N'SalePrice');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [StockItem] DROP CONSTRAINT [' + @var18 + '];');
ALTER TABLE [StockItem] ALTER COLUMN [SalePrice] decimal(18,6) NOT NULL;

DECLARE @var19 sysname;
SELECT @var19 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockItem]') AND [c].[name] = N'QuantityPerUnitSnapshot');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [StockItem] DROP CONSTRAINT [' + @var19 + '];');
ALTER TABLE [StockItem] ALTER COLUMN [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL;

DECLARE @var20 sysname;
SELECT @var20 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockItem]') AND [c].[name] = N'Quantity');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [StockItem] DROP CONSTRAINT [' + @var20 + '];');
ALTER TABLE [StockItem] ALTER COLUMN [Quantity] decimal(18,6) NOT NULL;

DECLARE @var21 sysname;
SELECT @var21 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockItem]') AND [c].[name] = N'PurchasePrice');
IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [StockItem] DROP CONSTRAINT [' + @var21 + '];');
ALTER TABLE [StockItem] ALTER COLUMN [PurchasePrice] decimal(18,6) NOT NULL;

DECLARE @var22 sysname;
SELECT @var22 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockItem]') AND [c].[name] = N'BaseQuantity');
IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [StockItem] DROP CONSTRAINT [' + @var22 + '];');
ALTER TABLE [StockItem] ALTER COLUMN [BaseQuantity] decimal(18,6) NOT NULL;

DECLARE @var23 sysname;
SELECT @var23 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockDocument]') AND [c].[name] = N'DiscountAmount');
IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [StockDocument] DROP CONSTRAINT [' + @var23 + '];');
ALTER TABLE [StockDocument] ALTER COLUMN [DiscountAmount] decimal(18,6) NULL;

DECLARE @var24 sysname;
SELECT @var24 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockAdjustment]') AND [c].[name] = N'SalePrice');
IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [StockAdjustment] DROP CONSTRAINT [' + @var24 + '];');
ALTER TABLE [StockAdjustment] ALTER COLUMN [SalePrice] decimal(18,6) NULL;

DECLARE @var25 sysname;
SELECT @var25 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockAdjustment]') AND [c].[name] = N'QuantityPerUnitSnapshot');
IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [StockAdjustment] DROP CONSTRAINT [' + @var25 + '];');
ALTER TABLE [StockAdjustment] ALTER COLUMN [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL;

DECLARE @var26 sysname;
SELECT @var26 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockAdjustment]') AND [c].[name] = N'QuantityDelta');
IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [StockAdjustment] DROP CONSTRAINT [' + @var26 + '];');
ALTER TABLE [StockAdjustment] ALTER COLUMN [QuantityDelta] decimal(18,6) NOT NULL;

DECLARE @var27 sysname;
SELECT @var27 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockAdjustment]') AND [c].[name] = N'PurchasePrice');
IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [StockAdjustment] DROP CONSTRAINT [' + @var27 + '];');
ALTER TABLE [StockAdjustment] ALTER COLUMN [PurchasePrice] decimal(18,6) NULL;

DECLARE @var28 sysname;
SELECT @var28 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StockAdjustment]') AND [c].[name] = N'BaseQuantityDelta');
IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [StockAdjustment] DROP CONSTRAINT [' + @var28 + '];');
ALTER TABLE [StockAdjustment] ALTER COLUMN [BaseQuantityDelta] decimal(18,6) NOT NULL;

DECLARE @var29 sysname;
SELECT @var29 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stock]') AND [c].[name] = N'SalePrice');
IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [Stock] DROP CONSTRAINT [' + @var29 + '];');
ALTER TABLE [Stock] ALTER COLUMN [SalePrice] decimal(18,6) NOT NULL;

DECLARE @var30 sysname;
SELECT @var30 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stock]') AND [c].[name] = N'Quantity');
IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Stock] DROP CONSTRAINT [' + @var30 + '];');
ALTER TABLE [Stock] ALTER COLUMN [Quantity] decimal(18,6) NOT NULL;

DECLARE @var31 sysname;
SELECT @var31 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stock]') AND [c].[name] = N'PurchasePrice');
IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [Stock] DROP CONSTRAINT [' + @var31 + '];');
ALTER TABLE [Stock] ALTER COLUMN [PurchasePrice] decimal(18,6) NOT NULL;

DECLARE @var32 sysname;
SELECT @var32 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RecurringJournalLines]') AND [c].[name] = N'DebitAmount');
IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [RecurringJournalLines] DROP CONSTRAINT [' + @var32 + '];');
ALTER TABLE [RecurringJournalLines] ALTER COLUMN [DebitAmount] decimal(18,6) NOT NULL;

DECLARE @var33 sysname;
SELECT @var33 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RecurringJournalLines]') AND [c].[name] = N'CreditAmount');
IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [RecurringJournalLines] DROP CONSTRAINT [' + @var33 + '];');
ALTER TABLE [RecurringJournalLines] ALTER COLUMN [CreditAmount] decimal(18,6) NOT NULL;

DECLARE @var34 sysname;
SELECT @var34 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductUnit]') AND [c].[name] = N'UnTaxedPrice');
IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [ProductUnit] DROP CONSTRAINT [' + @var34 + '];');
ALTER TABLE [ProductUnit] ALTER COLUMN [UnTaxedPrice] decimal(18,6) NOT NULL;

DECLARE @var35 sysname;
SELECT @var35 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductUnit]') AND [c].[name] = N'SalePrice');
IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [ProductUnit] DROP CONSTRAINT [' + @var35 + '];');
ALTER TABLE [ProductUnit] ALTER COLUMN [SalePrice] decimal(18,6) NOT NULL;

DECLARE @var36 sysname;
SELECT @var36 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductUnit]') AND [c].[name] = N'QuantityPerUnit');
IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [ProductUnit] DROP CONSTRAINT [' + @var36 + '];');
ALTER TABLE [ProductUnit] ALTER COLUMN [QuantityPerUnit] decimal(18,6) NOT NULL;

DECLARE @var37 sysname;
SELECT @var37 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductUnit]') AND [c].[name] = N'PurchasePrice');
IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [ProductUnit] DROP CONSTRAINT [' + @var37 + '];');
ALTER TABLE [ProductUnit] ALTER COLUMN [PurchasePrice] decimal(18,6) NOT NULL;

DECLARE @var38 sysname;
SELECT @var38 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Product]') AND [c].[name] = N'TaxRate');
IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [Product] DROP CONSTRAINT [' + @var38 + '];');
ALTER TABLE [Product] ALTER COLUMN [TaxRate] decimal(18,6) NULL;

DECLARE @var39 sysname;
SELECT @var39 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Product]') AND [c].[name] = N'MiniQuantity');
IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [Product] DROP CONSTRAINT [' + @var39 + '];');
ALTER TABLE [Product] ALTER COLUMN [MiniQuantity] decimal(18,6) NULL;

DECLARE @var40 sysname;
SELECT @var40 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[JournalEntryLines]') AND [c].[name] = N'TaxAmount');
IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [JournalEntryLines] DROP CONSTRAINT [' + @var40 + '];');
ALTER TABLE [JournalEntryLines] ALTER COLUMN [TaxAmount] decimal(18,6) NULL;

DECLARE @var41 sysname;
SELECT @var41 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[JournalEntryLines]') AND [c].[name] = N'ForeignAmount');
IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [JournalEntryLines] DROP CONSTRAINT [' + @var41 + '];');
ALTER TABLE [JournalEntryLines] ALTER COLUMN [ForeignAmount] decimal(18,6) NULL;

DECLARE @var42 sysname;
SELECT @var42 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[JournalEntryLines]') AND [c].[name] = N'ExchangeRate');
IF @var42 IS NOT NULL EXEC(N'ALTER TABLE [JournalEntryLines] DROP CONSTRAINT [' + @var42 + '];');
ALTER TABLE [JournalEntryLines] ALTER COLUMN [ExchangeRate] decimal(18,6) NULL;

DECLARE @var43 sysname;
SELECT @var43 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[JournalEntryLines]') AND [c].[name] = N'Debit');
IF @var43 IS NOT NULL EXEC(N'ALTER TABLE [JournalEntryLines] DROP CONSTRAINT [' + @var43 + '];');
ALTER TABLE [JournalEntryLines] ALTER COLUMN [Debit] decimal(18,6) NOT NULL;

DECLARE @var44 sysname;
SELECT @var44 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[JournalEntryLines]') AND [c].[name] = N'Credit');
IF @var44 IS NOT NULL EXEC(N'ALTER TABLE [JournalEntryLines] DROP CONSTRAINT [' + @var44 + '];');
ALTER TABLE [JournalEntryLines] ALTER COLUMN [Credit] decimal(18,6) NOT NULL;

DECLARE @var45 sysname;
SELECT @var45 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'UnitPrice');
IF @var45 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var45 + '];');
ALTER TABLE [InvoiceLine] ALTER COLUMN [UnitPrice] decimal(18,6) NOT NULL;

DECLARE @var46 sysname;
SELECT @var46 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'UnitCost');
IF @var46 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var46 + '];');
ALTER TABLE [InvoiceLine] ALTER COLUMN [UnitCost] decimal(18,6) NOT NULL;

DECLARE @var47 sysname;
SELECT @var47 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'TaxRate');
IF @var47 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var47 + '];');
ALTER TABLE [InvoiceLine] ALTER COLUMN [TaxRate] decimal(18,6) NOT NULL;

DECLARE @var48 sysname;
SELECT @var48 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'TaxAmount');
IF @var48 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var48 + '];');
ALTER TABLE [InvoiceLine] ALTER COLUMN [TaxAmount] decimal(18,6) NOT NULL;

DECLARE @var49 sysname;
SELECT @var49 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'QuantityPerUnitSnapshot');
IF @var49 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var49 + '];');
ALTER TABLE [InvoiceLine] ALTER COLUMN [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL;

DECLARE @var50 sysname;
SELECT @var50 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'Quantity');
IF @var50 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var50 + '];');
ALTER TABLE [InvoiceLine] ALTER COLUMN [Quantity] decimal(18,6) NOT NULL;

DECLARE @var51 sysname;
SELECT @var51 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'ProfitBeforeTax');
IF @var51 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var51 + '];');
ALTER TABLE [InvoiceLine] ALTER COLUMN [ProfitBeforeTax] decimal(18,6) NOT NULL;

DECLARE @var52 sysname;
SELECT @var52 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'Profit');
IF @var52 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var52 + '];');
ALTER TABLE [InvoiceLine] ALTER COLUMN [Profit] decimal(18,6) NOT NULL;

DECLARE @var53 sysname;
SELECT @var53 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'LineSubTotal');
IF @var53 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var53 + '];');
ALTER TABLE [InvoiceLine] ALTER COLUMN [LineSubTotal] decimal(18,6) NOT NULL;

DECLARE @var54 sysname;
SELECT @var54 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLine]') AND [c].[name] = N'BaseQuantity');
IF @var54 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLine] DROP CONSTRAINT [' + @var54 + '];');
ALTER TABLE [InvoiceLine] ALTER COLUMN [BaseQuantity] decimal(18,6) NOT NULL;

DECLARE @var55 sysname;
SELECT @var55 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'TotalTax');
IF @var55 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var55 + '];');
ALTER TABLE [Invoice] ALTER COLUMN [TotalTax] decimal(18,6) NOT NULL;

DECLARE @var56 sysname;
SELECT @var56 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'TotalCOGS');
IF @var56 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var56 + '];');
ALTER TABLE [Invoice] ALTER COLUMN [TotalCOGS] decimal(18,6) NOT NULL;

DECLARE @var57 sysname;
SELECT @var57 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'TotalAmount');
IF @var57 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var57 + '];');
ALTER TABLE [Invoice] ALTER COLUMN [TotalAmount] decimal(18,6) NOT NULL;

DECLARE @var58 sysname;
SELECT @var58 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'SubTotal');
IF @var58 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var58 + '];');
ALTER TABLE [Invoice] ALTER COLUMN [SubTotal] decimal(18,6) NOT NULL;

DECLARE @var59 sysname;
SELECT @var59 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'NetSales');
IF @var59 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var59 + '];');
ALTER TABLE [Invoice] ALTER COLUMN [NetSales] decimal(18,6) NOT NULL;

DECLARE @var60 sysname;
SELECT @var60 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'GrossProfit');
IF @var60 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var60 + '];');
ALTER TABLE [Invoice] ALTER COLUMN [GrossProfit] decimal(18,6) NOT NULL;

DECLARE @var61 sysname;
SELECT @var61 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'ExchangeRate');
IF @var61 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var61 + '];');
ALTER TABLE [Invoice] ALTER COLUMN [ExchangeRate] decimal(18,6) NOT NULL;

DECLARE @var62 sysname;
SELECT @var62 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoice]') AND [c].[name] = N'DiscountAmount');
IF @var62 IS NOT NULL EXEC(N'ALTER TABLE [Invoice] DROP CONSTRAINT [' + @var62 + '];');
ALTER TABLE [Invoice] ALTER COLUMN [DiscountAmount] decimal(18,6) NULL;

DECLARE @var63 sysname;
SELECT @var63 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FinancialTransaction]') AND [c].[name] = N'ExchangeRate');
IF @var63 IS NOT NULL EXEC(N'ALTER TABLE [FinancialTransaction] DROP CONSTRAINT [' + @var63 + '];');
ALTER TABLE [FinancialTransaction] ALTER COLUMN [ExchangeRate] decimal(18,6) NOT NULL;

DECLARE @var64 sysname;
SELECT @var64 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FinancialTransaction]') AND [c].[name] = N'Amount');
IF @var64 IS NOT NULL EXEC(N'ALTER TABLE [FinancialTransaction] DROP CONSTRAINT [' + @var64 + '];');
ALTER TABLE [FinancialTransaction] ALTER COLUMN [Amount] decimal(18,6) NOT NULL;

DECLARE @var65 sysname;
SELECT @var65 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ExchangeRates]') AND [c].[name] = N'Rate');
IF @var65 IS NOT NULL EXEC(N'ALTER TABLE [ExchangeRates] DROP CONSTRAINT [' + @var65 + '];');
ALTER TABLE [ExchangeRates] ALTER COLUMN [Rate] decimal(18,6) NOT NULL;

DECLARE @var66 sysname;
SELECT @var66 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employee]') AND [c].[name] = N'BasicSalary');
IF @var66 IS NOT NULL EXEC(N'ALTER TABLE [Employee] DROP CONSTRAINT [' + @var66 + '];');
ALTER TABLE [Employee] ALTER COLUMN [BasicSalary] decimal(18,6) NULL;

DECLARE @var67 sysname;
SELECT @var67 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Currencies]') AND [c].[name] = N'ExchangeRate');
IF @var67 IS NOT NULL EXEC(N'ALTER TABLE [Currencies] DROP CONSTRAINT [' + @var67 + '];');
ALTER TABLE [Currencies] ALTER COLUMN [ExchangeRate] decimal(18,6) NOT NULL;

DECLARE @var68 sysname;
SELECT @var68 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Check]') AND [c].[name] = N'Amount');
IF @var68 IS NOT NULL EXEC(N'ALTER TABLE [Check] DROP CONSTRAINT [' + @var68 + '];');
ALTER TABLE [Check] ALTER COLUMN [Amount] decimal(18,6) NOT NULL;

DECLARE @var69 sysname;
SELECT @var69 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CashierSession]') AND [c].[name] = N'StatrBalance');
IF @var69 IS NOT NULL EXEC(N'ALTER TABLE [CashierSession] DROP CONSTRAINT [' + @var69 + '];');
ALTER TABLE [CashierSession] ALTER COLUMN [StatrBalance] decimal(18,6) NOT NULL;

DECLARE @var70 sysname;
SELECT @var70 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CashierSession]') AND [c].[name] = N'ExpectedClosingBalance');
IF @var70 IS NOT NULL EXEC(N'ALTER TABLE [CashierSession] DROP CONSTRAINT [' + @var70 + '];');
ALTER TABLE [CashierSession] ALTER COLUMN [ExpectedClosingBalance] decimal(18,6) NULL;

DECLARE @var71 sysname;
SELECT @var71 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CashierSession]') AND [c].[name] = N'EndingBalance');
IF @var71 IS NOT NULL EXEC(N'ALTER TABLE [CashierSession] DROP CONSTRAINT [' + @var71 + '];');
ALTER TABLE [CashierSession] ALTER COLUMN [EndingBalance] decimal(18,6) NOT NULL;

DECLARE @var72 sysname;
SELECT @var72 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CashierSession]') AND [c].[name] = N'DifferenceAmount');
IF @var72 IS NOT NULL EXEC(N'ALTER TABLE [CashierSession] DROP CONSTRAINT [' + @var72 + '];');
ALTER TABLE [CashierSession] ALTER COLUMN [DifferenceAmount] decimal(18,6) NULL;

DECLARE @var73 sysname;
SELECT @var73 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BankStatements]') AND [c].[name] = N'OpeningBalance');
IF @var73 IS NOT NULL EXEC(N'ALTER TABLE [BankStatements] DROP CONSTRAINT [' + @var73 + '];');
ALTER TABLE [BankStatements] ALTER COLUMN [OpeningBalance] decimal(18,6) NOT NULL;

DECLARE @var74 sysname;
SELECT @var74 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BankStatements]') AND [c].[name] = N'ClosingBalance');
IF @var74 IS NOT NULL EXEC(N'ALTER TABLE [BankStatements] DROP CONSTRAINT [' + @var74 + '];');
ALTER TABLE [BankStatements] ALTER COLUMN [ClosingBalance] decimal(18,6) NOT NULL;

DECLARE @var75 sysname;
SELECT @var75 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BankStatementLines]') AND [c].[name] = N'Amount');
IF @var75 IS NOT NULL EXEC(N'ALTER TABLE [BankStatementLines] DROP CONSTRAINT [' + @var75 + '];');
ALTER TABLE [BankStatementLines] ALTER COLUMN [Amount] decimal(18,6) NOT NULL;

DECLARE @var76 sysname;
SELECT @var76 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AccountOpeningBalances]') AND [c].[name] = N'Debit');
IF @var76 IS NOT NULL EXEC(N'ALTER TABLE [AccountOpeningBalances] DROP CONSTRAINT [' + @var76 + '];');
ALTER TABLE [AccountOpeningBalances] ALTER COLUMN [Debit] decimal(18,6) NOT NULL;

DECLARE @var77 sysname;
SELECT @var77 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AccountOpeningBalances]') AND [c].[name] = N'Credit');
IF @var77 IS NOT NULL EXEC(N'ALTER TABLE [AccountOpeningBalances] DROP CONSTRAINT [' + @var77 + '];');
ALTER TABLE [AccountOpeningBalances] ALTER COLUMN [Credit] decimal(18,6) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260821133518_IncreaseDecimalPrecisionToSix', N'9.0.6');

COMMIT;
GO
