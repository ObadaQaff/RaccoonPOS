SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @Now DATETIME2 = SYSDATETIME();

-- Temporarily disable constraints for clean re-seed
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE [' + s.name + N'].[' + t.name + N'] NOCHECK CONSTRAINT ALL;'
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory';
EXEC sp_executesql @sql;

-- Clean transactional and master tables (keep migration history)
DELETE FROM [Check];
DELETE FROM FinancialTransaction;
DELETE FROM StockTransaction;
DELETE FROM InvoiceLine;
DELETE FROM Invoice;
DELETE FROM Voucher;
DELETE FROM StockItem;
DELETE FROM StockDocument;
DELETE FROM Stock;
DELETE FROM ProductUnit;
DELETE FROM Product;
DELETE FROM SubCategoryBrand;
DELETE FROM SubCategory;
DELETE FROM Brand;
DELETE FROM Category;
DELETE FROM Unit;
DELETE FROM CashierSession;
DELETE FROM [User];

-- Reset identities
SET @sql = N'';
SELECT @sql += N'DBCC CHECKIDENT (''[' + s.name + N'].[' + t.name + N']'', RESEED, 0) WITH NO_INFOMSGS;'
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.identity_columns ic ON ic.object_id = t.object_id
WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory';
EXEC sp_executesql @sql;

-- Users (Admin, Cashier, Customer, Supplier)
INSERT INTO [User] (Name, PhoneNumber, [Password], [Role], CreatedDate, UpdatedDate)
VALUES
('Demo Admin', '0799000001', '1234', 0, @Now, @Now),
('Demo Cashier', '0799000002', '1234', 1, @Now, @Now),
('Demo Customer A', '0799000003', '1234', 2, @Now, @Now),
('Demo Supplier A', '0799000004', '1234', 3, @Now, @Now);

DECLARE @AdminId INT = (SELECT Id FROM [User] WHERE Name = 'Demo Admin');
DECLARE @CashierId INT = (SELECT Id FROM [User] WHERE Name = 'Demo Cashier');
DECLARE @CustomerId INT = (SELECT Id FROM [User] WHERE Name = 'Demo Customer A');
DECLARE @SupplierId INT = (SELECT Id FROM [User] WHERE Name = 'Demo Supplier A');

-- Open cashier session
INSERT INTO CashierSession (CashierId, OpenedAt, ClosedAt, StatrBalance, EndingBalance, [Status], CreatedDate, UpdatedDate)
VALUES (@CashierId, DATEADD(HOUR, -4, @Now), NULL, 200.000, 200.000, 0, @Now, @Now);

DECLARE @CashierSessionId INT = SCOPE_IDENTITY();

-- Master data: categories / subcategories / brands / units
INSERT INTO Category (Name, Description, ImageUrl, CreatedDate, UpdatedDate)
VALUES
('Beverages', 'Drinks and juices', NULL, @Now, @Now),
('Snacks', 'Chips and biscuits', NULL, @Now, @Now);

DECLARE @CatBeverages INT = (SELECT Id FROM Category WHERE Name = 'Beverages');
DECLARE @CatSnacks INT = (SELECT Id FROM Category WHERE Name = 'Snacks');

INSERT INTO SubCategory (Name, ImageUrl, Description, ParentCategoryId, CreatedDate, UpdatedDate)
VALUES
('Soft Drinks', NULL, 'Carbonated beverages', @CatBeverages, @Now, @Now),
('Fruit Juice', NULL, 'Natural and mixed juices', @CatBeverages, @Now, @Now),
('Potato Chips', NULL, 'Salted and flavored chips', @CatSnacks, @Now, @Now);

DECLARE @SubSoftDrinks INT = (SELECT Id FROM SubCategory WHERE Name = 'Soft Drinks');
DECLARE @SubFruitJuice INT = (SELECT Id FROM SubCategory WHERE Name = 'Fruit Juice');
DECLARE @SubChips INT = (SELECT Id FROM SubCategory WHERE Name = 'Potato Chips');

INSERT INTO Brand (Name, ImageUrl, CreatedDate, UpdatedDate)
VALUES
('Raccoon Cola Co', NULL, @Now, @Now),
('Mountain Sip', NULL, @Now, @Now),
('Crunchy Bite', NULL, @Now, @Now);

DECLARE @BrandCola INT = (SELECT Id FROM Brand WHERE Name = 'Raccoon Cola Co');
DECLARE @BrandMountain INT = (SELECT Id FROM Brand WHERE Name = 'Mountain Sip');
DECLARE @BrandCrunch INT = (SELECT Id FROM Brand WHERE Name = 'Crunchy Bite');

INSERT INTO Unit (Name, CreatedDate, UpdatedDate)
VALUES
('Piece', @Now, @Now),
('Box', @Now, @Now),
('Carton', @Now, @Now);

DECLARE @UnitPiece INT = (SELECT Id FROM Unit WHERE Name = 'Piece');
DECLARE @UnitBox INT = (SELECT Id FROM Unit WHERE Name = 'Box');
DECLARE @UnitCarton INT = (SELECT Id FROM Unit WHERE Name = 'Carton');

INSERT INTO SubCategoryBrand (SubCategoryId, BrandId, CreatedDate, UpdatedDate)
VALUES
(@SubSoftDrinks, @BrandCola, @Now, @Now),
(@SubFruitJuice, @BrandMountain, @Now, @Now),
(@SubChips, @BrandCrunch, @Now, @Now);

-- Products
INSERT INTO Product
(Name, ITEMCODE, ImageUrl, Description, [Status], TaxExempt, MiniQuantity, SubCategoryId, BrandId, CreatedDate, UpdatedDate, EndDate, IsSoldOut, IsDeleted, TaxRate)
VALUES
('Raccoon Cola 330ml', 1001001, NULL, 'Demo cola can', 1, 0, 10, @SubSoftDrinks, @BrandCola, @Now, @Now, NULL, 0, 0, 16.00),
('Mountain Orange Juice 1L', 1001002, NULL, 'Demo orange juice', 1, 0, 8, @SubFruitJuice, @BrandMountain, @Now, @Now, NULL, 0, 0, 8.00),
('Crunchy Chips Salt 45g', 1001003, NULL, 'Demo chips pack', 1, 0, 15, @SubChips, @BrandCrunch, @Now, @Now, NULL, 0, 0, 16.00);

DECLARE @ProdCola INT = (SELECT Id FROM Product WHERE Name = 'Raccoon Cola 330ml');
DECLARE @ProdJuice INT = (SELECT Id FROM Product WHERE Name = 'Mountain Orange Juice 1L');
DECLARE @ProdChips INT = (SELECT Id FROM Product WHERE Name = 'Crunchy Chips Salt 45g');

-- Product units (base + secondary)
INSERT INTO ProductUnit
(SalePrice, PurchasePrice, QuantityPerUnit, ProductId, UnitId, UnitId1, CreatedDate, UpdatedDate, UnTaxedPrice, IsBaseUnit, IsDefaultSaleUnit, IsDefaultPurchaseUnit)
VALUES
(0.500, 0.350, 1, @ProdCola, @UnitPiece, NULL, @Now, @Now, 0.431, 1, 1, 0),
(10.000, 7.000, 24, @ProdCola, @UnitBox, NULL, @Now, @Now, 8.621, 0, 0, 1),
(1.200, 0.850, 1, @ProdJuice, @UnitPiece, NULL, @Now, @Now, 1.111, 1, 1, 1),
(0.700, 0.450, 1, @ProdChips, @UnitPiece, NULL, @Now, @Now, 0.603, 1, 1, 1),
(16.000, 10.500, 30, @ProdChips, @UnitCarton, NULL, @Now, @Now, 13.793, 0, 0, 0);

DECLARE @ColaPieceUnit INT = (SELECT TOP 1 Id FROM ProductUnit WHERE ProductId = @ProdCola AND UnitId = @UnitPiece);
DECLARE @ColaBoxUnit INT = (SELECT TOP 1 Id FROM ProductUnit WHERE ProductId = @ProdCola AND UnitId = @UnitBox);
DECLARE @JuicePieceUnit INT = (SELECT TOP 1 Id FROM ProductUnit WHERE ProductId = @ProdJuice AND UnitId = @UnitPiece);
DECLARE @ChipsPieceUnit INT = (SELECT TOP 1 Id FROM ProductUnit WHERE ProductId = @ProdChips AND UnitId = @UnitPiece);
DECLARE @ChipsCartonUnit INT = (SELECT TOP 1 Id FROM ProductUnit WHERE ProductId = @ProdChips AND UnitId = @UnitCarton);

-- Stock balances
INSERT INTO Stock (ProductId, ProductUnitId, Quantity, CreatedDate, UpdatedDate)
VALUES
(@ProdCola, @ColaPieceUnit, 120, @Now, @Now),
(@ProdCola, @ColaBoxUnit, 12, @Now, @Now),
(@ProdJuice, @JuicePieceUnit, 70, @Now, @Now),
(@ProdChips, @ChipsPieceUnit, 150, @Now, @Now),
(@ProdChips, @ChipsCartonUnit, 5, @Now, @Now);

DECLARE @StockColaPiece INT = (SELECT TOP 1 Id FROM Stock WHERE ProductId = @ProdCola AND ProductUnitId = @ColaPieceUnit);
DECLARE @StockJuicePiece INT = (SELECT TOP 1 Id FROM Stock WHERE ProductId = @ProdJuice AND ProductUnitId = @JuicePieceUnit);
DECLARE @StockChipsPiece INT = (SELECT TOP 1 Id FROM Stock WHERE ProductId = @ProdChips AND ProductUnitId = @ChipsPieceUnit);

-- Stock in voucher-style document
INSERT INTO StockDocument (DocumentNumber, [Type], Notes, CreatedDate, UpdatedDate, SupplierId)
VALUES ('STKIN-DEM-0001', 1, 'Demo stock in import', @Now, @Now, @SupplierId);
DECLARE @StockDocInId INT = SCOPE_IDENTITY();

INSERT INTO StockItem
(StockId, ProductId, ProductUnitId, Quantity, PurchasePrice, SalePrice, ExpiryDate, CreatedDate, UpdatedDate, QuantityPerUnitSnapshot, BaseQuantity)
VALUES
(@StockDocInId, @ProdCola, @ColaPieceUnit, 120, 0.350, 0.500, DATEADD(MONTH, 8, @Now), @Now, @Now, 1, 120),
(@StockDocInId, @ProdJuice, @JuicePieceUnit, 70, 0.850, 1.200, DATEADD(MONTH, 6, @Now), @Now, @Now, 1, 70),
(@StockDocInId, @ProdChips, @ChipsPieceUnit, 150, 0.450, 0.700, DATEADD(MONTH, 5, @Now), @Now, @Now, 1, 150);

INSERT INTO StockTransaction
(StockId, ProductId, ProductUnitId, Quantity, TransactionType, TransactionDate, InvoiceId, UserId, Notes, CreatedDate, UpdatedDate, IsDeleted, UnitPrice, QuantityPerUnitSnapshot, BaseQuantity, CasherId, CustomerId, CashierSessionId, VoucherId)
VALUES
(@StockColaPiece, @ProdCola, @ColaPieceUnit, 120, 2, DATEADD(HOUR, -3, @Now), NULL, @AdminId, 'Seed purchase in', @Now, @Now, 0, 0.350, 1, 120, @CashierId, NULL, @CashierSessionId, NULL),
(@StockJuicePiece, @ProdJuice, @JuicePieceUnit, 70, 2, DATEADD(HOUR, -3, @Now), NULL, @AdminId, 'Seed purchase in', @Now, @Now, 0, 0.850, 1, 70, @CashierId, NULL, @CashierSessionId, NULL),
(@StockChipsPiece, @ProdChips, @ChipsPieceUnit, 150, 2, DATEADD(HOUR, -3, @Now), NULL, @AdminId, 'Seed purchase in', @Now, @Now, 0, 0.450, 1, 150, @CashierId, NULL, @CashierSessionId, NULL);

-- Invoices (normal sales invoice + POS invoice + purchase invoice)
INSERT INTO Invoice
(InvoiceType, CasherId, SupplierId, CustomerId, UserId, VoucherId, TotalAmount, CreatedDate, UpdatedDate, PaymentType, InvoiceNumber, [Status], IsPOS, OpenedAt, ClosedAt, DiscountAmount, OriginalInvoiceId, CashierSessionId, SubTotal, TotalTax, NetSales, TotalCOGS, GrossProfit)
VALUES
(0, @CashierId, NULL, @CustomerId, NULL, NULL, 5.900, DATEADD(HOUR, -2, @Now), DATEADD(HOUR, -2, @Now), 1, 'INV-DEM-0001', 2, 0, DATEADD(HOUR, -2, @Now), DATEADD(HOUR, -2, @Now), 0, NULL, @CashierSessionId, 5.310, 0.590, 5.900, 3.850, 1.460),
(0, @CashierId, NULL, @CustomerId, NULL, NULL, 3.100, DATEADD(HOUR, -1, @Now), DATEADD(HOUR, -1, @Now), 7, 'POS-DEM-0001', 2, 1, DATEADD(HOUR, -1, @Now), DATEADD(HOUR, -1, @Now), 0, NULL, @CashierSessionId, 2.793, 0.307, 3.100, 2.050, 0.743),
(2, @CashierId, @SupplierId, NULL, NULL, NULL, 28.000, DATEADD(HOUR, -3, @Now), DATEADD(HOUR, -3, @Now), 4, 'PINV-DEM-0001', 2, 0, DATEADD(HOUR, -3, @Now), DATEADD(HOUR, -3, @Now), 0, NULL, @CashierSessionId, 28.000, 0.000, 28.000, 28.000, 0.000);

DECLARE @InvSale INT = (SELECT Id FROM Invoice WHERE InvoiceNumber = 'INV-DEM-0001');
DECLARE @InvPos INT = (SELECT Id FROM Invoice WHERE InvoiceNumber = 'POS-DEM-0001');
DECLARE @InvPurchase INT = (SELECT Id FROM Invoice WHERE InvoiceNumber = 'PINV-DEM-0001');

INSERT INTO InvoiceLine
(InvoiceId, ProductId, ProductUnitId, Quantity, UnitPrice, CreatedDate, UpdatedDate, ExpiryDate, OriginalInvoiceId, UnitCost, TaxExempt, TaxRate, TaxAmount, LineSubTotal, ProfitBeforeTax, Profit, QuantityPerUnitSnapshot, BaseQuantity)
VALUES
(@InvSale, @ProdCola, @ColaPieceUnit, 5, 0.500, @Now, @Now, DATEADD(MONTH, 8, @Now), NULL, 0.350, 0, 16.00, 0.345, 2.155, 0.405, 0.405, 1, 5),
(@InvSale, @ProdChips, @ChipsPieceUnit, 2, 0.700, @Now, @Now, DATEADD(MONTH, 5, @Now), NULL, 0.450, 0, 16.00, 0.193, 1.207, 0.307, 0.307, 1, 2),
(@InvPos, @ProdJuice, @JuicePieceUnit, 2, 1.200, @Now, @Now, DATEADD(MONTH, 6, @Now), NULL, 0.850, 0, 8.00, 0.178, 2.222, 0.522, 0.522, 1, 2),
(@InvPos, @ProdCola, @ColaPieceUnit, 1, 0.500, @Now, @Now, DATEADD(MONTH, 8, @Now), NULL, 0.350, 0, 16.00, 0.069, 0.431, 0.081, 0.081, 1, 1),
(@InvPurchase, @ProdCola, @ColaBoxUnit, 2, 7.000, @Now, @Now, DATEADD(MONTH, 9, @Now), NULL, 7.000, 1, 0.00, 0.000, 14.000, 0.000, 0.000, 24, 48),
(@InvPurchase, @ProdChips, @ChipsCartonUnit, 1, 10.500, @Now, @Now, DATEADD(MONTH, 7, @Now), NULL, 10.500, 1, 0.00, 0.000, 10.500, 0.000, 0.000, 30, 30);

-- Vouchers (receipt + payment by check)
INSERT INTO Voucher
(VoucherType, Amount, PaymentType, CasherId, SupplierId, CustomerId, Notes, UserId, CreatedDate, UpdatedDate, VoucherNumber, CashierSessionId)
VALUES
(7, 3.100, 1, @CashierId, NULL, @CustomerId, 'Demo receipt voucher (cash)', NULL, @Now, @Now, 'VCH-REC-0001', @CashierSessionId),
(8, 28.000, 4, @CashierId, @SupplierId, NULL, 'Demo payment voucher (check)', NULL, @Now, @Now, 'VCH-PAY-0001', @CashierSessionId);

DECLARE @VoucherReceipt INT = (SELECT Id FROM Voucher WHERE VoucherNumber = 'VCH-REC-0001');
DECLARE @VoucherPayment INT = (SELECT Id FROM Voucher WHERE VoucherNumber = 'VCH-PAY-0001');

INSERT INTO [Check]
(CheckNumber, BankName, DueDate, Amount, Notes, VoucherId, InvoiceId, CreatedDate, UpdatedDate)
VALUES
('CHK-DEM-0001', 'Demo National Bank', DATEADD(DAY, 15, @Now), 28.000, 'Supplier payment by check', @VoucherPayment, NULL, @Now, @Now);

-- Financial transactions for demo tracing
INSERT INTO FinancialTransaction
(TransactionNumber, Method, Amount, TransactionDate, Casher, Notes, CreatedDate, UpdatedDate, CashierId, CashierSessionId, Direction, SourceType, SourceId, [Status])
VALUES
('FT-POS-0001', 2, 3.100, DATEADD(HOUR, -1, @Now), @CashierId, 'POS sale payment (Visa)', @Now, @Now, @CashierId, @CashierSessionId, 0, 2, @InvPos, 0),
('FT-SALE-0001', 1, 5.900, DATEADD(HOUR, -2, @Now), @CashierId, 'Sale invoice cash', @Now, @Now, @CashierId, @CashierSessionId, 0, 3, @InvSale, 0),
('FT-REC-0001', 1, 3.100, @Now, @CashierId, 'Receipt voucher', @Now, @Now, @CashierId, @CashierSessionId, 0, 5, @VoucherReceipt, 0),
('FT-PAY-0001', 6, 28.000, @Now, @CashierId, 'Payment voucher by check', @Now, @Now, @CashierId, @CashierSessionId, 1, 6, @VoucherPayment, 0);

-- Re-enable constraints
SET @sql = N'';
SELECT @sql += N'ALTER TABLE [' + s.name + N'].[' + t.name + N'] WITH CHECK CHECK CONSTRAINT ALL;'
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory';
EXEC sp_executesql @sql;

COMMIT TRAN;

-- Quick summary
SELECT 
    (SELECT COUNT(*) FROM Category) AS Categories,
    (SELECT COUNT(*) FROM SubCategory) AS SubCategories,
    (SELECT COUNT(*) FROM Brand) AS Brands,
    (SELECT COUNT(*) FROM Unit) AS Units,
    (SELECT COUNT(*) FROM Product) AS Products,
    (SELECT COUNT(*) FROM ProductUnit) AS ProductUnits,
    (SELECT COUNT(*) FROM Stock) AS Stocks,
    (SELECT COUNT(*) FROM Invoice) AS Invoices,
    (SELECT COUNT(*) FROM InvoiceLine) AS InvoiceLines,
    (SELECT COUNT(*) FROM Voucher) AS Vouchers,
    (SELECT COUNT(*) FROM [Check]) AS Checks,
    (SELECT COUNT(*) FROM FinancialTransaction) AS FinancialTransactions;
