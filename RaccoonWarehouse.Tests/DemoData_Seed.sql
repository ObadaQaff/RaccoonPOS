SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @Today DATE = CAST(@Now AS DATE);
    DECLARE @sql NVARCHAR(MAX) = N'';

    /* Disable constraints for clean demo reseed */
    SELECT @sql += N'ALTER TABLE [' + s.name + N'].[' + t.name + N'] NOCHECK CONSTRAINT ALL;'
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory';
    EXEC sp_executesql @sql;

    /* Clean transactional and presentation data */
    IF OBJECT_ID(N'dbo.JournalEntryLines', N'U') IS NOT NULL DELETE FROM dbo.JournalEntryLines;
    IF OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL DELETE FROM dbo.JournalEntries;
    IF OBJECT_ID(N'dbo.AccountOpeningBalances', N'U') IS NOT NULL DELETE FROM dbo.AccountOpeningBalances;
    IF OBJECT_ID(N'dbo.AccountingPeriods', N'U') IS NOT NULL DELETE FROM dbo.AccountingPeriods;
    IF OBJECT_ID(N'dbo.FiscalYears', N'U') IS NOT NULL DELETE FROM dbo.FiscalYears;
    IF OBJECT_ID(N'dbo.Accounts', N'U') IS NOT NULL DELETE FROM dbo.Accounts;

    IF OBJECT_ID(N'dbo.[Check]', N'U') IS NOT NULL DELETE FROM dbo.[Check];
    IF OBJECT_ID(N'dbo.Checks', N'U') IS NOT NULL DELETE FROM dbo.Checks;
    IF OBJECT_ID(N'dbo.FinancialTransaction', N'U') IS NOT NULL DELETE FROM dbo.FinancialTransaction;
    IF OBJECT_ID(N'dbo.FinancialTransactions', N'U') IS NOT NULL DELETE FROM dbo.FinancialTransactions;
    IF OBJECT_ID(N'dbo.Voucher', N'U') IS NOT NULL DELETE FROM dbo.Voucher;
    IF OBJECT_ID(N'dbo.Vouchers', N'U') IS NOT NULL DELETE FROM dbo.Vouchers;
    IF OBJECT_ID(N'dbo.InvoiceLine', N'U') IS NOT NULL DELETE FROM dbo.InvoiceLine;
    IF OBJECT_ID(N'dbo.InvoiceLines', N'U') IS NOT NULL DELETE FROM dbo.InvoiceLines;
    IF OBJECT_ID(N'dbo.Invoice', N'U') IS NOT NULL DELETE FROM dbo.Invoice;
    IF OBJECT_ID(N'dbo.Invoices', N'U') IS NOT NULL DELETE FROM dbo.Invoices;
    IF OBJECT_ID(N'dbo.StockTransaction', N'U') IS NOT NULL DELETE FROM dbo.StockTransaction;
    IF OBJECT_ID(N'dbo.StockTransactions', N'U') IS NOT NULL DELETE FROM dbo.StockTransactions;
    IF OBJECT_ID(N'dbo.StockItem', N'U') IS NOT NULL DELETE FROM dbo.StockItem;
    IF OBJECT_ID(N'dbo.StockItems', N'U') IS NOT NULL DELETE FROM dbo.StockItems;
    IF OBJECT_ID(N'dbo.StockDocument', N'U') IS NOT NULL DELETE FROM dbo.StockDocument;
    IF OBJECT_ID(N'dbo.StockDocuments', N'U') IS NOT NULL DELETE FROM dbo.StockDocuments;
    IF OBJECT_ID(N'dbo.StockAdjustment', N'U') IS NOT NULL DELETE FROM dbo.StockAdjustment;
    IF OBJECT_ID(N'dbo.StockAdjustments', N'U') IS NOT NULL DELETE FROM dbo.StockAdjustments;
    IF OBJECT_ID(N'dbo.StockLot', N'U') IS NOT NULL DELETE FROM dbo.StockLot;
    IF OBJECT_ID(N'dbo.StockLots', N'U') IS NOT NULL DELETE FROM dbo.StockLots;
    IF OBJECT_ID(N'dbo.Stock', N'U') IS NOT NULL DELETE FROM dbo.Stock;
    IF OBJECT_ID(N'dbo.Stocks', N'U') IS NOT NULL DELETE FROM dbo.Stocks;
    IF OBJECT_ID(N'dbo.CashierSession', N'U') IS NOT NULL DELETE FROM dbo.CashierSession;
    IF OBJECT_ID(N'dbo.CashierSessions', N'U') IS NOT NULL DELETE FROM dbo.CashierSessions;
    IF OBJECT_ID(N'dbo.Warehouse', N'U') IS NOT NULL DELETE FROM dbo.Warehouse;
    IF OBJECT_ID(N'dbo.Warehouses', N'U') IS NOT NULL DELETE FROM dbo.Warehouses;
    IF OBJECT_ID(N'dbo.Currencies', N'U') IS NOT NULL DELETE FROM dbo.Currencies;
    IF OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL DELETE FROM dbo.Branches;
    IF OBJECT_ID(N'dbo.Tax', N'U') IS NOT NULL DELETE FROM dbo.Tax;
    IF OBJECT_ID(N'dbo.Taxs', N'U') IS NOT NULL DELETE FROM dbo.Taxs;
    IF OBJECT_ID(N'dbo.ProductUnit', N'U') IS NOT NULL DELETE FROM dbo.ProductUnit;
    IF OBJECT_ID(N'dbo.ProductUnits', N'U') IS NOT NULL DELETE FROM dbo.ProductUnits;
    IF OBJECT_ID(N'dbo.Product', N'U') IS NOT NULL DELETE FROM dbo.Product;
    IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DELETE FROM dbo.Products;
    IF OBJECT_ID(N'dbo.SubCategoryBrand', N'U') IS NOT NULL DELETE FROM dbo.SubCategoryBrand;
    IF OBJECT_ID(N'dbo.SubCategoryBrands', N'U') IS NOT NULL DELETE FROM dbo.SubCategoryBrands;
    IF OBJECT_ID(N'dbo.SubCategory', N'U') IS NOT NULL DELETE FROM dbo.SubCategory;
    IF OBJECT_ID(N'dbo.SubCategories', N'U') IS NOT NULL DELETE FROM dbo.SubCategories;
    IF OBJECT_ID(N'dbo.Brand', N'U') IS NOT NULL DELETE FROM dbo.Brand;
    IF OBJECT_ID(N'dbo.Brands', N'U') IS NOT NULL DELETE FROM dbo.Brands;
    IF OBJECT_ID(N'dbo.Category', N'U') IS NOT NULL DELETE FROM dbo.Category;
    IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DELETE FROM dbo.Categories;
    IF OBJECT_ID(N'dbo.Unit', N'U') IS NOT NULL DELETE FROM dbo.Unit;
    IF OBJECT_ID(N'dbo.Units', N'U') IS NOT NULL DELETE FROM dbo.Units;
    IF OBJECT_ID(N'dbo.[User]', N'U') IS NOT NULL DELETE FROM dbo.[User];

    /* Reset identities */
    SET @sql = N'';
    SELECT @sql += N'DBCC CHECKIDENT (''[' + s.name + N'].[' + t.name + N']'', RESEED, 0) WITH NO_INFOMSGS;'
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    JOIN sys.identity_columns ic ON ic.object_id = t.object_id
    WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory';
    EXEC sp_executesql @sql;

    /* Users */
    INSERT INTO dbo.[User] (Name, PhoneNumber, [Password], [Role], CreatedDate, UpdatedDate)
    VALUES
    (N'مدير النظام', N'0790000001', N'1234', 0, @Now, @Now),
    (N'كاشير المعرض', N'0790000002', N'1234', 1, @Now, @Now),
    (N'شركة الأفق التجارية', N'0790000003', N'1234', 2, @Now, @Now),
    (N'مؤسسة النور للتوريد', N'0790000004', N'1234', 3, @Now, @Now),
    (N'مؤسسة المدارس الحديثة', N'0790000005', N'1234', 2, @Now, @Now),
    (N'شركة الهلال للمواد الغذائية', N'0790000006', N'1234', 3, @Now, @Now);

    DECLARE @AdminId INT = (SELECT Id FROM dbo.[User] WHERE Name = N'مدير النظام');
    DECLARE @CashierId INT = (SELECT Id FROM dbo.[User] WHERE Name = N'كاشير المعرض');
    DECLARE @CustomerRetailId INT = (SELECT Id FROM dbo.[User] WHERE Name = N'شركة الأفق التجارية');
    DECLARE @SupplierMainId INT = (SELECT Id FROM dbo.[User] WHERE Name = N'مؤسسة النور للتوريد');
    DECLARE @CustomerWholesaleId INT = (SELECT Id FROM dbo.[User] WHERE Name = N'مؤسسة المدارس الحديثة');
    DECLARE @SupplierAltId INT = (SELECT Id FROM dbo.[User] WHERE Name = N'شركة الهلال للمواد الغذائية');

    /* Optional master data */
    IF OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.Branches (Code, Name, ArabicName, EnglishName, Address, PhoneNumber, IsActive, CreatedDate, UpdatedDate)
        VALUES (N'BR01', N'الفرع الرئيسي', N'الفرع الرئيسي', N'Main Branch', N'عمان - شارع المدينة', N'065500001', 1, @Now, @Now);
    END;

    IF OBJECT_ID(N'dbo.Currencies', N'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.Currencies (Code, Name, ArabicName, EnglishName, Symbol, ExchangeRate, IsBaseCurrency, IsActive, CreatedDate, UpdatedDate)
        VALUES (N'JOD', N'دينار أردني', N'دينار أردني', N'Jordanian Dinar', N'د.أ', 1, 1, 1, @Now, @Now);
    END;

    DECLARE @BranchId INT = CASE WHEN OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL THEN (SELECT TOP 1 Id FROM dbo.Branches) END;
    DECLARE @CurrencyId INT = CASE WHEN OBJECT_ID(N'dbo.Currencies', N'U') IS NOT NULL THEN (SELECT TOP 1 Id FROM dbo.Currencies WHERE Code = N'JOD') END;

    IF OBJECT_ID(N'dbo.Tax', N'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.Tax (Name, Rate, IsActive, CreatedDate, UpdatedDate)
        VALUES
        (N'ضريبة مبيعات 16%', 16.00, 1, @Now, @Now),
        (N'ضريبة مخفضة 8%', 8.00, 0, @Now, @Now);
    END;

    /* Product master */
    INSERT INTO dbo.Category (Name, Description, ImageUrl, CreatedDate, UpdatedDate)
    VALUES
    (N'المشروبات', N'عصائر ومياه ومشروبات غازية', NULL, @Now, @Now),
    (N'المواد الغذائية', N'أرز وسكر وزيوت', NULL, @Now, @Now),
    (N'المنظفات', N'مواد تنظيف منزلية', NULL, @Now, @Now);

    DECLARE @CatDrinks INT = (SELECT Id FROM dbo.Category WHERE Name = N'المشروبات');
    DECLARE @CatFood INT = (SELECT Id FROM dbo.Category WHERE Name = N'المواد الغذائية');
    DECLARE @CatCleaning INT = (SELECT Id FROM dbo.Category WHERE Name = N'المنظفات');

    INSERT INTO dbo.SubCategory (Name, ImageUrl, Description, ParentCategoryId, CreatedDate, UpdatedDate)
    VALUES
    (N'مشروبات غازية', NULL, N'عبوات غازية متنوعة', @CatDrinks, @Now, @Now),
    (N'عصائر', NULL, N'عصائر طبيعية ومعبأة', @CatDrinks, @Now, @Now),
    (N'مواد أساسية', NULL, N'أرز وسكر وطحين وزيوت', @CatFood, @Now, @Now),
    (N'منظفات أرضيات', NULL, N'منظفات وتعقيم', @CatCleaning, @Now, @Now);

    DECLARE @SubSoft INT = (SELECT Id FROM dbo.SubCategory WHERE Name = N'مشروبات غازية');
    DECLARE @SubJuices INT = (SELECT Id FROM dbo.SubCategory WHERE Name = N'عصائر');
    DECLARE @SubBasics INT = (SELECT Id FROM dbo.SubCategory WHERE Name = N'مواد أساسية');
    DECLARE @SubCleaners INT = (SELECT Id FROM dbo.SubCategory WHERE Name = N'منظفات أرضيات');

    INSERT INTO dbo.Brand (Name, ImageUrl, CreatedDate, UpdatedDate)
    VALUES
    (N'قمة', NULL, @Now, @Now),
    (N'ندى', NULL, @Now, @Now),
    (N'بيت الخير', NULL, @Now, @Now),
    (N'نقاء', NULL, @Now, @Now);

    DECLARE @BrandQimma INT = (SELECT Id FROM dbo.Brand WHERE Name = N'قمة');
    DECLARE @BrandNada INT = (SELECT Id FROM dbo.Brand WHERE Name = N'ندى');
    DECLARE @BrandBayt INT = (SELECT Id FROM dbo.Brand WHERE Name = N'بيت الخير');
    DECLARE @BrandNaqa INT = (SELECT Id FROM dbo.Brand WHERE Name = N'نقاء');

    INSERT INTO dbo.Unit (Name, CreatedDate, UpdatedDate)
    VALUES
    (N'حبة', @Now, @Now),
    (N'علبة', @Now, @Now),
    (N'كرتونة', @Now, @Now),
    (N'كيس', @Now, @Now),
    (N'جالون', @Now, @Now);

    DECLARE @UnitPiece INT = (SELECT Id FROM dbo.Unit WHERE Name = N'حبة');
    DECLARE @UnitBox INT = (SELECT Id FROM dbo.Unit WHERE Name = N'علبة');
    DECLARE @UnitCarton INT = (SELECT Id FROM dbo.Unit WHERE Name = N'كرتونة');
    DECLARE @UnitBag INT = (SELECT Id FROM dbo.Unit WHERE Name = N'كيس');
    DECLARE @UnitGallon INT = (SELECT Id FROM dbo.Unit WHERE Name = N'جالون');

    INSERT INTO dbo.SubCategoryBrand (SubCategoryId, BrandId, CreatedDate, UpdatedDate)
    VALUES
    (@SubSoft, @BrandQimma, @Now, @Now),
    (@SubJuices, @BrandNada, @Now, @Now),
    (@SubBasics, @BrandBayt, @Now, @Now),
    (@SubCleaners, @BrandNaqa, @Now, @Now);

    INSERT INTO dbo.Product
    (Name, ITEMCODE, ImageUrl, Description, [Status], TaxExempt, MiniQuantity, SubCategoryId, BrandId, CreatedDate, UpdatedDate, EndDate, IsSoldOut, IsDeleted, TaxRate)
    VALUES
    (N'مياه قمة 500 مل', 110001, NULL, N'عبوة مياه للشرب', 1, 0, 24, @SubSoft, @BrandQimma, @Now, @Now, NULL, 0, 0, 16.00),
    (N'عصير برتقال ندى 1 لتر', 110002, NULL, N'عصير برتقال طبيعي', 1, 0, 12, @SubJuices, @BrandNada, @Now, @Now, NULL, 0, 0, 8.00),
    (N'أرز بيت الخير 5 كغ', 110003, NULL, N'أرز بسمتي فاخر', 1, 0, 10, @SubBasics, @BrandBayt, @Now, @Now, NULL, 0, 0, 0.00),
    (N'سكر بيت الخير 1 كغ', 110004, NULL, N'سكر أبيض ناعم', 1, 0, 15, @SubBasics, @BrandBayt, @Now, @Now, NULL, 0, 0, 0.00),
    (N'منظف أرضيات نقاء 3 لتر', 110005, NULL, N'منظف ومعقم للأرضيات', 1, 0, 6, @SubCleaners, @BrandNaqa, @Now, @Now, NULL, 0, 0, 16.00),
    (N'مشروب غازي قمة 330 مل', 110006, NULL, N'مشروب غازي بارد', 1, 0, 30, @SubSoft, @BrandQimma, @Now, @Now, NULL, 0, 0, 16.00);

    DECLARE @ProdWater INT = (SELECT Id FROM dbo.Product WHERE ITEMCODE = 110001);
    DECLARE @ProdJuice INT = (SELECT Id FROM dbo.Product WHERE ITEMCODE = 110002);
    DECLARE @ProdRice INT = (SELECT Id FROM dbo.Product WHERE ITEMCODE = 110003);
    DECLARE @ProdSugar INT = (SELECT Id FROM dbo.Product WHERE ITEMCODE = 110004);
    DECLARE @ProdCleaner INT = (SELECT Id FROM dbo.Product WHERE ITEMCODE = 110005);
    DECLARE @ProdSoda INT = (SELECT Id FROM dbo.Product WHERE ITEMCODE = 110006);

    INSERT INTO dbo.ProductUnit
    (SalePrice, PurchasePrice, QuantityPerUnit, ProductId, UnitId, UnitId1, CreatedDate, UpdatedDate, UnTaxedPrice, IsBaseUnit, IsDefaultSaleUnit, IsDefaultPurchaseUnit)
    VALUES
    (0.250, 0.140, 1, @ProdWater, @UnitPiece, NULL, @Now, @Now, 0.216, 1, 1, 0),
    (5.400, 3.100, 24, @ProdWater, @UnitCarton, NULL, @Now, @Now, 4.655, 0, 0, 1),
    (1.100, 0.750, 1, @ProdJuice, @UnitPiece, NULL, @Now, @Now, 1.019, 1, 1, 1),
    (4.250, 3.200, 1, @ProdRice, @UnitBag, NULL, @Now, @Now, 4.250, 1, 1, 1),
    (0.950, 0.700, 1, @ProdSugar, @UnitBag, NULL, @Now, @Now, 0.950, 1, 1, 1),
    (3.750, 2.550, 1, @ProdCleaner, @UnitGallon, NULL, @Now, @Now, 3.233, 1, 1, 1),
    (0.350, 0.190, 1, @ProdSoda, @UnitPiece, NULL, @Now, @Now, 0.302, 1, 1, 0),
    (7.900, 4.200, 24, @ProdSoda, @UnitCarton, NULL, @Now, @Now, 6.810, 0, 0, 1);

    DECLARE @WaterPieceUnit INT = (SELECT TOP 1 Id FROM dbo.ProductUnit WHERE ProductId = @ProdWater AND UnitId = @UnitPiece);
    DECLARE @WaterCartonUnit INT = (SELECT TOP 1 Id FROM dbo.ProductUnit WHERE ProductId = @ProdWater AND UnitId = @UnitCarton);
    DECLARE @JuiceUnit INT = (SELECT TOP 1 Id FROM dbo.ProductUnit WHERE ProductId = @ProdJuice AND UnitId = @UnitPiece);
    DECLARE @RiceUnit INT = (SELECT TOP 1 Id FROM dbo.ProductUnit WHERE ProductId = @ProdRice AND UnitId = @UnitBag);
    DECLARE @SugarUnit INT = (SELECT TOP 1 Id FROM dbo.ProductUnit WHERE ProductId = @ProdSugar AND UnitId = @UnitBag);
    DECLARE @CleanerUnit INT = (SELECT TOP 1 Id FROM dbo.ProductUnit WHERE ProductId = @ProdCleaner AND UnitId = @UnitGallon);
    DECLARE @SodaPieceUnit INT = (SELECT TOP 1 Id FROM dbo.ProductUnit WHERE ProductId = @ProdSoda AND UnitId = @UnitPiece);
    DECLARE @SodaCartonUnit INT = (SELECT TOP 1 Id FROM dbo.ProductUnit WHERE ProductId = @ProdSoda AND UnitId = @UnitCarton);

    /* Warehouse and session */
    IF OBJECT_ID(N'dbo.Warehouse', N'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.Warehouse (Code, Name, Location, Description, PhoneNumber, BranchId, [Status], CreatedDate, UpdatedDate)
        VALUES
        (N'WH-MAIN', N'المستودع الرئيسي', N'عمان - سحاب', N'مستودع العرض الرئيسي', 65000002, @BranchId, 1, @Now, @Now),
        (N'WH-SHOW', N'مخزن الصالة', N'عمان - شارع الجامعة', N'مخزن قريب من نقطة البيع', 65000003, @BranchId, 1, @Now, @Now);
    END;

    DECLARE @WarehouseId INT = CASE WHEN OBJECT_ID(N'dbo.Warehouse', N'U') IS NOT NULL THEN (SELECT TOP 1 Id FROM dbo.Warehouse WHERE Code = N'WH-MAIN') END;

    INSERT INTO dbo.CashierSession
    (CashierId, SessionNumber, BranchId, OpenedAt, ClosedAt, StatrBalance, EndingBalance, ExpectedClosingBalance, DifferenceAmount, [Status], CreatedDate, UpdatedDate)
    VALUES
    (@CashierId, N'CS-20260411-01', @BranchId, DATEADD(HOUR, -6, @Now), NULL, 250.000, 250.000, NULL, NULL, 0, @Now, @Now);

    DECLARE @CashierSessionId INT = SCOPE_IDENTITY();

    /* Stock balances */
    INSERT INTO dbo.Stock (ProductId, ProductUnitId, WarehouseId, Quantity, PurchasePrice, SalePrice, LastMovementDate, CreatedDate, UpdatedDate)
    VALUES
    (@ProdWater, @WaterPieceUnit, @WarehouseId, 240, 0.140, 0.250, @Now, @Now, @Now),
    (@ProdWater, @WaterCartonUnit, @WarehouseId, 20, 3.100, 5.400, @Now, @Now, @Now),
    (@ProdJuice, @JuiceUnit, @WarehouseId, 80, 0.750, 1.100, @Now, @Now, @Now),
    (@ProdRice, @RiceUnit, @WarehouseId, 35, 3.200, 4.250, @Now, @Now, @Now),
    (@ProdSugar, @SugarUnit, @WarehouseId, 50, 0.700, 0.950, @Now, @Now, @Now),
    (@ProdCleaner, @CleanerUnit, @WarehouseId, 24, 2.550, 3.750, @Now, @Now, @Now),
    (@ProdSoda, @SodaPieceUnit, @WarehouseId, 180, 0.190, 0.350, @Now, @Now, @Now),
    (@ProdSoda, @SodaCartonUnit, @WarehouseId, 14, 4.200, 7.900, @Now, @Now, @Now);

    DECLARE @StockWaterPiece INT = (SELECT TOP 1 Id FROM dbo.Stock WHERE ProductId = @ProdWater AND ProductUnitId = @WaterPieceUnit);
    DECLARE @StockJuice INT = (SELECT TOP 1 Id FROM dbo.Stock WHERE ProductId = @ProdJuice AND ProductUnitId = @JuiceUnit);
    DECLARE @StockRice INT = (SELECT TOP 1 Id FROM dbo.Stock WHERE ProductId = @ProdRice AND ProductUnitId = @RiceUnit);
    DECLARE @StockSugar INT = (SELECT TOP 1 Id FROM dbo.Stock WHERE ProductId = @ProdSugar AND ProductUnitId = @SugarUnit);
    DECLARE @StockCleaner INT = (SELECT TOP 1 Id FROM dbo.Stock WHERE ProductId = @ProdCleaner AND ProductUnitId = @CleanerUnit);
    DECLARE @StockSodaPiece INT = (SELECT TOP 1 Id FROM dbo.Stock WHERE ProductId = @ProdSoda AND ProductUnitId = @SodaPieceUnit);

    /* Stock documents */
    INSERT INTO dbo.StockDocument
    (DocumentNumber, [Type], DocumentDate, ReferenceNumber, Notes, SupplierId, WarehouseId, BranchId, PostingStatus, CreatedBy, UpdatedBy, CreatedDate, UpdatedDate)
    VALUES
    (N'STIN-AR-0001', 1, @Now, N'PO-AR-0001', N'توريد افتتاحي من المورد الرئيسي', @SupplierMainId, @WarehouseId, @BranchId, 3, @AdminId, @AdminId, @Now, @Now),
    (N'STOUT-AR-0001', 2, @Now, N'USE-AR-0001', N'صرف داخلي لمواد الضيافة والتنظيف', NULL, @WarehouseId, @BranchId, 3, @AdminId, @AdminId, @Now, @Now);

    DECLARE @StockInDocId INT = (SELECT Id FROM dbo.StockDocument WHERE DocumentNumber = N'STIN-AR-0001');
    DECLARE @StockOutDocId INT = (SELECT Id FROM dbo.StockDocument WHERE DocumentNumber = N'STOUT-AR-0001');

    INSERT INTO dbo.StockItem
    (StockId, LineNumber, ProductId, ProductUnitId, Quantity, QuantityPerUnitSnapshot, BaseQuantity, PurchasePrice, SalePrice, ExpiryDate, CreatedDate, UpdatedDate)
    VALUES
    (@StockInDocId, 1, @ProdWater, @WaterCartonUnit, 10, 24, 240, 3.100, 5.400, DATEADD(MONTH, 10, @Now), @Now, @Now),
    (@StockInDocId, 2, @ProdRice, @RiceUnit, 15, 1, 15, 3.200, 4.250, DATEADD(YEAR, 1, @Now), @Now, @Now),
    (@StockInDocId, 3, @ProdCleaner, @CleanerUnit, 8, 1, 8, 2.550, 3.750, DATEADD(MONTH, 18, @Now), @Now, @Now),
    (@StockOutDocId, 1, @ProdSugar, @SugarUnit, 3, 1, 3, 0.700, 0.950, NULL, @Now, @Now),
    (@StockOutDocId, 2, @ProdCleaner, @CleanerUnit, 2, 1, 2, 2.550, 3.750, NULL, @Now, @Now);

    INSERT INTO dbo.StockTransaction
    (ProductId, ProductUnitId, StockId, WarehouseId, StockDocumentId, Quantity, QuantityPerUnitSnapshot, BaseQuantity, UnitPrice, TransactionType, InvoiceId, VoucherId, CasherId, CashierSessionId, CustomerId, BranchId, TransactionDate, Notes, ReferenceNumber, SourceType, SourceId, CreatedBy, CreatedDate, UpdatedDate)
    VALUES
    (@ProdWater, @WaterCartonUnit, NULL, @WarehouseId, @StockInDocId, 10, 24, 240, 3.100, 2, NULL, NULL, @CashierId, @CashierSessionId, NULL, @BranchId, DATEADD(HOUR, -5, @Now), N'توريد افتتاحي مياه', N'STIN-AR-0001', 4, @StockInDocId, @AdminId, @Now, @Now),
    (@ProdRice, @RiceUnit, @StockRice, @WarehouseId, @StockInDocId, 15, 1, 15, 3.200, 2, NULL, NULL, @CashierId, @CashierSessionId, NULL, @BranchId, DATEADD(HOUR, -5, @Now), N'توريد افتتاحي أرز', N'STIN-AR-0001', 4, @StockInDocId, @AdminId, @Now, @Now),
    (@ProdSugar, @SugarUnit, @StockSugar, @WarehouseId, @StockOutDocId, 3, 1, 3, 0.700, 1, NULL, NULL, @CashierId, @CashierSessionId, NULL, @BranchId, DATEADD(HOUR, -1, @Now), N'صرف داخلي سكر', N'STOUT-AR-0001', 4, @StockOutDocId, @AdminId, @Now, @Now),
    (@ProdCleaner, @CleanerUnit, @StockCleaner, @WarehouseId, @StockOutDocId, 2, 1, 2, 2.550, 1, NULL, NULL, @CashierId, @CashierSessionId, NULL, @BranchId, DATEADD(HOUR, -1, @Now), N'صرف داخلي منظفات', N'STOUT-AR-0001', 4, @StockOutDocId, @AdminId, @Now, @Now);

    /* Invoices */
    INSERT INTO dbo.Invoice
    (InvoiceNumber, OriginalInvoiceId, InvoiceType, PaymentType, CasherId, SupplierId, CustomerId, VoucherId, BranchId, WarehouseId, CurrencyId, ExchangeRate, DocumentDate, ReferenceNumber, PostingStatus, CreatedBy, UpdatedBy, TotalAmount, CashierSessionId, [Status], IsPOS, OpenedAt, ClosedAt, DiscountAmount, SubTotal, TotalTax, TotalCOGS, GrossProfit, NetSales, CreatedDate, UpdatedDate)
    VALUES
    (N'INV-AR-0001', NULL, 0, 1, @CashierId, NULL, @CustomerRetailId, NULL, @BranchId, @WarehouseId, @CurrencyId, 1, @Now, N'SALE-CASH-1', 3, @AdminId, @AdminId, 8.000, @CashierSessionId, 2, 0, DATEADD(HOUR, -3, @Now), DATEADD(HOUR, -3, @Now), 0.000, 6.900, 1.100, 4.850, 2.050, 6.900, DATEADD(HOUR, -3, @Now), DATEADD(HOUR, -3, @Now)),
    (N'INV-AR-0002', NULL, 0, 2, @CashierId, NULL, @CustomerWholesaleId, NULL, @BranchId, @WarehouseId, @CurrencyId, 1, @Now, N'SALE-CREDIT-1', 3, @AdminId, @AdminId, 13.500, @CashierSessionId, 2, 0, DATEADD(HOUR, -2, @Now), DATEADD(HOUR, -2, @Now), 0.500, 12.069, 1.931, 8.650, 3.419, 11.569, DATEADD(HOUR, -2, @Now), DATEADD(HOUR, -2, @Now)),
    (N'POS-AR-0001', NULL, 0, 7, @CashierId, NULL, @CustomerRetailId, NULL, @BranchId, @WarehouseId, @CurrencyId, 1, @Now, N'POS-SALE-1', 3, @AdminId, @AdminId, 4.200, @CashierSessionId, 2, 1, DATEADD(HOUR, -1, @Now), DATEADD(HOUR, -1, @Now), 0.000, 3.621, 0.579, 2.310, 1.311, 3.621, DATEADD(HOUR, -1, @Now), DATEADD(HOUR, -1, @Now)),
    (N'PINV-AR-0001', NULL, 2, 2, @CashierId, @SupplierMainId, NULL, NULL, @BranchId, @WarehouseId, @CurrencyId, 1, @Now, N'PUR-CREDIT-1', 3, @AdminId, @AdminId, 56.500, @CashierSessionId, 2, 0, DATEADD(HOUR, -4, @Now), DATEADD(HOUR, -4, @Now), 0.000, 56.500, 0.000, 56.500, 0.000, 56.500, DATEADD(HOUR, -4, @Now), DATEADD(HOUR, -4, @Now));

    DECLARE @InvoiceCashId INT = (SELECT Id FROM dbo.Invoice WHERE InvoiceNumber = N'INV-AR-0001');
    DECLARE @InvoiceCreditId INT = (SELECT Id FROM dbo.Invoice WHERE InvoiceNumber = N'INV-AR-0002');
    DECLARE @InvoicePosId INT = (SELECT Id FROM dbo.Invoice WHERE InvoiceNumber = N'POS-AR-0001');
    DECLARE @InvoicePurchaseId INT = (SELECT Id FROM dbo.Invoice WHERE InvoiceNumber = N'PINV-AR-0001');

    INSERT INTO dbo.InvoiceLine
    (InvoiceId, ProductId, ProductUnitId, Quantity, UnitPrice, ExpiryDate, OriginalInvoiceId, UnitCost, TaxExempt, TaxRate, TaxAmount, LineSubTotal, ProfitBeforeTax, Profit, QuantityPerUnitSnapshot, BaseQuantity, CreatedDate, UpdatedDate)
    VALUES
    (@InvoiceCashId, @ProdWater, @WaterPieceUnit, 8, 0.250, DATEADD(MONTH, 10, @Now), NULL, 0.140, 0, 16.00, 0.276, 1.724, 0.604, 0.604, 1, 8, @Now, @Now),
    (@InvoiceCashId, @ProdCleaner, @CleanerUnit, 1, 3.750, DATEADD(MONTH, 18, @Now), NULL, 2.550, 0, 16.00, 0.517, 3.233, 0.683, 0.683, 1, 1, @Now, @Now),
    (@InvoiceCreditId, @ProdRice, @RiceUnit, 2, 4.250, DATEADD(YEAR, 1, @Now), NULL, 3.200, 1, 0.00, 0.000, 8.500, 2.100, 2.100, 1, 2, @Now, @Now),
    (@InvoiceCreditId, @ProdJuice, @JuiceUnit, 4, 1.100, DATEADD(MONTH, 8, @Now), NULL, 0.750, 0, 8.00, 0.326, 4.074, 1.074, 1.074, 1, 4, @Now, @Now),
    (@InvoicePosId, @ProdSoda, @SodaPieceUnit, 6, 0.350, DATEADD(MONTH, 9, @Now), NULL, 0.190, 0, 16.00, 0.290, 1.810, 0.670, 0.670, 1, 6, @Now, @Now),
    (@InvoicePosId, @ProdWater, @WaterPieceUnit, 4, 0.250, DATEADD(MONTH, 10, @Now), NULL, 0.140, 0, 16.00, 0.138, 0.862, 0.302, 0.302, 1, 4, @Now, @Now),
    (@InvoicePurchaseId, @ProdWater, @WaterCartonUnit, 10, 3.100, DATEADD(MONTH, 10, @Now), NULL, 3.100, 1, 0.00, 0.000, 31.000, 0.000, 0.000, 24, 240, @Now, @Now),
    (@InvoicePurchaseId, @ProdCleaner, @CleanerUnit, 10, 2.550, DATEADD(MONTH, 18, @Now), NULL, 2.550, 1, 0.00, 0.000, 25.500, 0.000, 0.000, 1, 10, @Now, @Now);

    /* Vouchers */
    INSERT INTO dbo.Voucher
    (VoucherNumber, VoucherDate, VoucherType, Amount, PaymentType, CasherId, SupplierId, CustomerId, BranchId, WarehouseId, CurrencyId, ExchangeRate, ReferenceNumber, Notes, CashierSessionId, PostingStatus, CreatedBy, UpdatedBy, CreatedDate, UpdatedDate)
    VALUES
    (N'VCH-REC-AR-0001', @Now, 7, 13.500, 1, @CashierId, NULL, @CustomerWholesaleId, @BranchId, @WarehouseId, @CurrencyId, 1, N'RV-001', N'سند قبض من العميل على فاتورة آجلة', @CashierSessionId, 3, @AdminId, @AdminId, @Now, @Now),
    (N'VCH-PAY-AR-0001', @Now, 8, 25.000, 4, @CashierId, @SupplierMainId, NULL, @BranchId, @WarehouseId, @CurrencyId, 1, N'PV-001', N'سند صرف للمورد الرئيسي', @CashierSessionId, 3, @AdminId, @AdminId, @Now, @Now);

    DECLARE @VoucherReceiptId INT = (SELECT Id FROM dbo.Voucher WHERE VoucherNumber = N'VCH-REC-AR-0001');
    DECLARE @VoucherPaymentId INT = (SELECT Id FROM dbo.Voucher WHERE VoucherNumber = N'VCH-PAY-AR-0001');

    IF OBJECT_ID(N'dbo.[Check]', N'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.[Check]
        (CheckNumber, BankName, DueDate, Amount, Notes, VoucherId, InvoiceId, CreatedDate, UpdatedDate)
        VALUES
        (N'CHK-AR-0001', N'البنك العربي', DATEADD(DAY, 14, @Now), 25.000, N'شيك سداد للمورد', @VoucherPaymentId, NULL, @Now, @Now);
    END;

    /* Financial transactions */
    INSERT INTO dbo.FinancialTransaction
    (TransactionNumber, Method, Amount, TransactionDate, Casher, Notes, CreatedDate, UpdatedDate, CashierId, CashierSessionId, Direction, SourceType, SourceId, [Status])
    VALUES
    (N'FT-SALE-AR-0001', 1, 8.000, DATEADD(HOUR, -3, @Now), @CashierId, N'تحصيل نقدي لفاتورة بيع', @Now, @Now, @CashierId, @CashierSessionId, 0, 3, @InvoiceCashId, 0),
    (N'FT-POS-AR-0001', 2, 4.200, DATEADD(HOUR, -1, @Now), @CashierId, N'تحصيل نقاط بيع بواسطة فيزا', @Now, @Now, @CashierId, @CashierSessionId, 0, 2, @InvoicePosId, 0),
    (N'FT-REC-AR-0001', 1, 13.500, @Now, @CashierId, N'تحصيل سند قبض', @Now, @Now, @CashierId, @CashierSessionId, 0, 5, @VoucherReceiptId, 0),
    (N'FT-PAY-AR-0001', 6, 25.000, @Now, @CashierId, N'صرف سند مورد بشيك', @Now, @Now, @CashierId, @CashierSessionId, 1, 6, @VoucherPaymentId, 0);

    /* Accounts and settings */
    IF OBJECT_ID(N'dbo.Accounts', N'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.Accounts
        (Code, Name, ArabicName, EnglishName, Description, AccountType, NormalBalanceType, IsPosting, IsActive, IsSystemGenerated, AllowManualEntry, [Level], CurrencyId, ParentAccountId, CreatedBy, UpdatedBy, CreatedDate, UpdatedDate)
        VALUES
        (N'1000', N'الصندوق', N'الصندوق', N'Cash', N'حساب الصندوق الرئيسي', 1, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'1100', N'البنك', N'البنك', N'Bank', N'حساب البنك الرئيسي', 1, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'1200', N'العملاء', N'العملاء', N'Accounts Receivable', N'ذمم العملاء', 1, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'1210', N'ضريبة المدخلات', N'ضريبة المدخلات', N'Input Tax', N'ضريبة شراء قابلة للخصم', 1, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'1300', N'المخزون', N'المخزون', N'Inventory', N'حساب مخزون البضاعة', 1, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'1400', N'ذمم مدينة أخرى', N'ذمم مدينة أخرى', N'Other Receivables', N'ذمم مدينة متنوعة', 1, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'2000', N'الموردون', N'الموردون', N'Accounts Payable', N'ذمم الموردين', 2, 2, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'2100', N'ضريبة المخرجات', N'ضريبة المخرجات', N'Output Tax', N'ضريبة المبيعات', 2, 2, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'2200', N'ذمم دائنة أخرى', N'ذمم دائنة أخرى', N'Other Payables', N'ذمم دائنة متنوعة', 2, 2, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'3000', N'رأس المال', N'رأس المال', N'Capital', N'حقوق الملكية', 3, 2, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'4000', N'إيرادات المبيعات', N'إيرادات المبيعات', N'Sales Revenue', N'إيرادات البيع', 4, 2, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'4100', N'مردودات المبيعات', N'مردودات المبيعات', N'Sales Returns', N'حساب مردودات البيع', 4, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'4200', N'خصومات المبيعات', N'خصومات المبيعات', N'Sales Discounts', N'خصومات بيع', 4, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'4300', N'أرباح تسويات المخزون', N'أرباح تسويات المخزون', N'Stock Gain', N'أرباح المخزون', 4, 2, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'5000', N'تكلفة البضاعة المباعة', N'تكلفة البضاعة المباعة', N'COGS', N'تكلفة البيع', 5, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'6000', N'مصروفات تشغيلية', N'مصروفات تشغيلية', N'Operating Expense', N'مصروفات تشغيلية عامة', 5, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now),
        (N'6100', N'خسائر المخزون', N'خسائر المخزون', N'Stock Loss', N'خسائر وتلف مخزون', 5, 1, 1, 1, 1, 1, 1, @CurrencyId, NULL, @AdminId, @AdminId, @Now, @Now);

        IF OBJECT_ID(N'dbo.AppSettings', N'U') IS NOT NULL
        BEGIN
            DELETE FROM dbo.AppSettings WHERE [Key] LIKE N'Accounting.AccountCode.%';

            INSERT INTO dbo.AppSettings ([Key], [Value], Description, CreatedDate, UpdatedDate)
            VALUES
            (N'Accounting.AccountCode.CashMain', N'1000', N'الحساب الافتراضي للصندوق', @Now, @Now),
            (N'Accounting.AccountCode.PosCash', N'1000', N'الحساب الافتراضي لصندوق نقاط البيع', @Now, @Now),
            (N'Accounting.AccountCode.Bank', N'1100', N'الحساب الافتراضي للبنك', @Now, @Now),
            (N'Accounting.AccountCode.AccountsReceivable', N'1200', N'الحساب الافتراضي للذمم المدينة', @Now, @Now),
            (N'Accounting.AccountCode.InputTax', N'1210', N'الحساب الافتراضي لضريبة المدخلات', @Now, @Now),
            (N'Accounting.AccountCode.Inventory', N'1300', N'الحساب الافتراضي للمخزون', @Now, @Now),
            (N'Accounting.AccountCode.OtherReceivables', N'1400', N'الحساب الافتراضي للذمم المدينة الأخرى', @Now, @Now),
            (N'Accounting.AccountCode.AccountsPayable', N'2000', N'الحساب الافتراضي للموردين', @Now, @Now),
            (N'Accounting.AccountCode.OutputTax', N'2100', N'الحساب الافتراضي لضريبة المخرجات', @Now, @Now),
            (N'Accounting.AccountCode.OtherPayables', N'2200', N'الحساب الافتراضي للذمم الدائنة الأخرى', @Now, @Now),
            (N'Accounting.AccountCode.SalesRevenue', N'4000', N'الحساب الافتراضي لإيراد المبيعات', @Now, @Now),
            (N'Accounting.AccountCode.SalesReturns', N'4100', N'الحساب الافتراضي لمردودات المبيعات', @Now, @Now),
            (N'Accounting.AccountCode.SalesDiscount', N'4200', N'الحساب الافتراضي لخصومات المبيعات', @Now, @Now),
            (N'Accounting.AccountCode.StockGain', N'4300', N'الحساب الافتراضي لأرباح المخزون', @Now, @Now),
            (N'Accounting.AccountCode.Cogs', N'5000', N'الحساب الافتراضي لتكلفة البضاعة المباعة', @Now, @Now),
            (N'Accounting.AccountCode.GeneralExpense', N'6000', N'الحساب الافتراضي للمصروفات', @Now, @Now),
            (N'Accounting.AccountCode.StockLoss', N'6100', N'الحساب الافتراضي لخسائر المخزون', @Now, @Now),
            (N'Accounting.AccountCode.InternalConsumption', N'6000', N'الحساب الافتراضي للصرف الداخلي', @Now, @Now);
        END;
    END;

    IF OBJECT_ID(N'dbo.FiscalYears', N'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.FiscalYears (Code, Name, StartDate, EndDate, [Status], IsClosed, CreatedBy, UpdatedBy, CreatedDate, UpdatedDate)
        VALUES (N'FY2026', N'السنة المالية 2026', DATEFROMPARTS(2026, 1, 1), DATEFROMPARTS(2026, 12, 31), 2, 0, @AdminId, @AdminId, @Now, @Now);
    END;

    DECLARE @FiscalYearId INT = CASE WHEN OBJECT_ID(N'dbo.FiscalYears', N'U') IS NOT NULL THEN (SELECT TOP 1 Id FROM dbo.FiscalYears WHERE Code = N'FY2026') END;

    IF OBJECT_ID(N'dbo.AccountingPeriods', N'U') IS NOT NULL AND @FiscalYearId IS NOT NULL
    BEGIN
        INSERT INTO dbo.AccountingPeriods (FiscalYearId, PeriodNumber, Name, StartDate, EndDate, [Status], IsClosed, CreatedBy, UpdatedBy, CreatedDate, UpdatedDate)
        VALUES (@FiscalYearId, 4, N'أبريل 2026', DATEFROMPARTS(2026, 4, 1), DATEFROMPARTS(2026, 4, 30), 2, 0, @AdminId, @AdminId, @Now, @Now);
    END;

    DECLARE @PeriodId INT = CASE WHEN OBJECT_ID(N'dbo.AccountingPeriods', N'U') IS NOT NULL THEN (SELECT TOP 1 Id FROM dbo.AccountingPeriods WHERE FiscalYearId = @FiscalYearId AND PeriodNumber = 4) END;

    IF OBJECT_ID(N'dbo.AccountOpeningBalances', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Accounts', N'U') IS NOT NULL AND @FiscalYearId IS NOT NULL
    BEGIN
        INSERT INTO dbo.AccountOpeningBalances
        (FiscalYearId, AccountId, BranchId, WarehouseId, Debit, Credit, ReferenceNumber, Notes, CreatedBy, UpdatedBy, CreatedDate, UpdatedDate)
        VALUES
        (@FiscalYearId, (SELECT Id FROM dbo.Accounts WHERE Code = N'1000'), @BranchId, @WarehouseId, 250.000, 0.000, N'OPEN-CASH', N'رصيد افتتاحي للصندوق', @AdminId, @AdminId, @Now, @Now),
        (@FiscalYearId, (SELECT Id FROM dbo.Accounts WHERE Code = N'1300'), @BranchId, @WarehouseId, 975.000, 0.000, N'OPEN-INV', N'رصيد افتتاحي للمخزون', @AdminId, @AdminId, @Now, @Now),
        (@FiscalYearId, (SELECT Id FROM dbo.Accounts WHERE Code = N'3000'), @BranchId, @WarehouseId, 0.000, 1225.000, N'OPEN-CAP', N'رصيد افتتاحي لرأس المال', @AdminId, @AdminId, @Now, @Now);
    END;

    /* Demo journals */
    IF OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.JournalEntryLines', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Accounts', N'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.JournalEntries
        (EntryNumber, EntryDate, Description, [Status], IsDraft, SourceType, SourceId, ReferenceType, ReferenceId, ReferenceNumber, Notes, FiscalYearId, AccountingPeriodId, BranchId, WarehouseId, CashierSessionId, CurrencyId, CreatedBy, UpdatedBy, ApprovedBy, ApprovedAt, CreatedDate, UpdatedDate)
        VALUES
        (N'JE-AR-0001', DATEADD(HOUR, -3, @Now), N'فاتورة بيع نقدي', 2, 0, 2, @InvoiceCashId, N'Invoice', @InvoiceCashId, N'INV-AR-0001', N'قيد بيع نقدي', @FiscalYearId, @PeriodId, @BranchId, @WarehouseId, @CashierSessionId, @CurrencyId, @AdminId, @AdminId, @AdminId, @Now, @Now, @Now),
        (N'JE-AR-0002', DATEADD(HOUR, -2, @Now), N'فاتورة بيع آجل', 2, 0, 2, @InvoiceCreditId, N'Invoice', @InvoiceCreditId, N'INV-AR-0002', N'قيد بيع آجل', @FiscalYearId, @PeriodId, @BranchId, @WarehouseId, @CashierSessionId, @CurrencyId, @AdminId, @AdminId, @AdminId, @Now, @Now, @Now),
        (N'JE-AR-0003', DATEADD(HOUR, -1, @Now), N'سند قبض عميل', 2, 0, 3, @VoucherReceiptId, N'Voucher', @VoucherReceiptId, N'VCH-REC-AR-0001', N'قيد سند قبض', @FiscalYearId, @PeriodId, @BranchId, @WarehouseId, @CashierSessionId, @CurrencyId, @AdminId, @AdminId, @AdminId, @Now, @Now, @Now),
        (N'JE-AR-0004', @Now, N'سند صرف مورد', 2, 0, 3, @VoucherPaymentId, N'Voucher', @VoucherPaymentId, N'VCH-PAY-AR-0001', N'قيد سند صرف', @FiscalYearId, @PeriodId, @BranchId, @WarehouseId, @CashierSessionId, @CurrencyId, @AdminId, @AdminId, @AdminId, @Now, @Now, @Now),
        (N'JE-AR-0005', DATEADD(HOUR, -5, @Now), N'توريد مخزون من مورد', 2, 0, 4, @StockInDocId, N'StockDocument', @StockInDocId, N'STIN-AR-0001', N'قيد إدخال مخزون', @FiscalYearId, @PeriodId, @BranchId, @WarehouseId, @CashierSessionId, @CurrencyId, @AdminId, @AdminId, @AdminId, @Now, @Now, @Now),
        (N'JE-AR-0006', DATEADD(HOUR, -1, @Now), N'صرف داخلي للمخزون', 2, 0, 4, @StockOutDocId, N'StockDocument', @StockOutDocId, N'STOUT-AR-0001', N'قيد صرف داخلي', @FiscalYearId, @PeriodId, @BranchId, @WarehouseId, @CashierSessionId, @CurrencyId, @AdminId, @AdminId, @AdminId, @Now, @Now, @Now);

        DECLARE @JE1 INT = (SELECT Id FROM dbo.JournalEntries WHERE EntryNumber = N'JE-AR-0001');
        DECLARE @JE2 INT = (SELECT Id FROM dbo.JournalEntries WHERE EntryNumber = N'JE-AR-0002');
        DECLARE @JE3 INT = (SELECT Id FROM dbo.JournalEntries WHERE EntryNumber = N'JE-AR-0003');
        DECLARE @JE4 INT = (SELECT Id FROM dbo.JournalEntries WHERE EntryNumber = N'JE-AR-0004');
        DECLARE @JE5 INT = (SELECT Id FROM dbo.JournalEntries WHERE EntryNumber = N'JE-AR-0005');
        DECLARE @JE6 INT = (SELECT Id FROM dbo.JournalEntries WHERE EntryNumber = N'JE-AR-0006');

        INSERT INTO dbo.JournalEntryLines
        (JournalEntryId, AccountId, LineNumber, Debit, Credit, Description, PartyUserId, CustomerId, SupplierId, CashierId, WarehouseId, BranchId, InvoiceId, VoucherId, StockDocumentId, FinancialTransactionId, ReferenceType, ReferenceId, CreatedDate, UpdatedDate)
        VALUES
        (@JE1, (SELECT Id FROM dbo.Accounts WHERE Code = N'1000'), 1, 8.000, 0.000, N'تحصيل نقدي', NULL, @CustomerRetailId, NULL, @CashierId, @WarehouseId, @BranchId, @InvoiceCashId, NULL, NULL, NULL, N'Invoice', @InvoiceCashId, @Now, @Now),
        (@JE1, (SELECT Id FROM dbo.Accounts WHERE Code = N'4000'), 2, 0.000, 6.900, N'إيراد المبيعات', NULL, @CustomerRetailId, NULL, @CashierId, @WarehouseId, @BranchId, @InvoiceCashId, NULL, NULL, NULL, N'Invoice', @InvoiceCashId, @Now, @Now),
        (@JE1, (SELECT Id FROM dbo.Accounts WHERE Code = N'2100'), 3, 0.000, 1.100, N'ضريبة مخرجات', NULL, @CustomerRetailId, NULL, @CashierId, @WarehouseId, @BranchId, @InvoiceCashId, NULL, NULL, NULL, N'Invoice', @InvoiceCashId, @Now, @Now),
        (@JE2, (SELECT Id FROM dbo.Accounts WHERE Code = N'1200'), 1, 13.500, 0.000, N'ذمة عميل', NULL, @CustomerWholesaleId, NULL, @CashierId, @WarehouseId, @BranchId, @InvoiceCreditId, NULL, NULL, NULL, N'Invoice', @InvoiceCreditId, @Now, @Now),
        (@JE2, (SELECT Id FROM dbo.Accounts WHERE Code = N'4000'), 2, 0.000, 11.569, N'إيراد المبيعات الآجلة', NULL, @CustomerWholesaleId, NULL, @CashierId, @WarehouseId, @BranchId, @InvoiceCreditId, NULL, NULL, NULL, N'Invoice', @InvoiceCreditId, @Now, @Now),
        (@JE2, (SELECT Id FROM dbo.Accounts WHERE Code = N'2100'), 3, 0.000, 1.931, N'ضريبة مخرجات', NULL, @CustomerWholesaleId, NULL, @CashierId, @WarehouseId, @BranchId, @InvoiceCreditId, NULL, NULL, NULL, N'Invoice', @InvoiceCreditId, @Now, @Now),
        (@JE3, (SELECT Id FROM dbo.Accounts WHERE Code = N'1000'), 1, 13.500, 0.000, N'تحصيل سند قبض', NULL, @CustomerWholesaleId, NULL, @CashierId, @WarehouseId, @BranchId, NULL, @VoucherReceiptId, NULL, NULL, N'Voucher', @VoucherReceiptId, @Now, @Now),
        (@JE3, (SELECT Id FROM dbo.Accounts WHERE Code = N'1200'), 2, 0.000, 13.500, N'تخفيض ذمة العميل', NULL, @CustomerWholesaleId, NULL, @CashierId, @WarehouseId, @BranchId, NULL, @VoucherReceiptId, NULL, NULL, N'Voucher', @VoucherReceiptId, @Now, @Now),
        (@JE4, (SELECT Id FROM dbo.Accounts WHERE Code = N'2000'), 1, 25.000, 0.000, N'سداد للمورد', NULL, NULL, @SupplierMainId, @CashierId, @WarehouseId, @BranchId, NULL, @VoucherPaymentId, NULL, NULL, N'Voucher', @VoucherPaymentId, @Now, @Now),
        (@JE4, (SELECT Id FROM dbo.Accounts WHERE Code = N'1100'), 2, 0.000, 25.000, N'صرف بشيك', NULL, NULL, @SupplierMainId, @CashierId, @WarehouseId, @BranchId, NULL, @VoucherPaymentId, NULL, NULL, N'Voucher', @VoucherPaymentId, @Now, @Now),
        (@JE5, (SELECT Id FROM dbo.Accounts WHERE Code = N'1300'), 1, 76.500, 0.000, N'زيادة مخزون من المورد', NULL, NULL, @SupplierMainId, @CashierId, @WarehouseId, @BranchId, NULL, NULL, @StockInDocId, NULL, N'StockDocument', @StockInDocId, @Now, @Now),
        (@JE5, (SELECT Id FROM dbo.Accounts WHERE Code = N'2000'), 2, 0.000, 76.500, N'التزام للمورد', NULL, NULL, @SupplierMainId, @CashierId, @WarehouseId, @BranchId, NULL, NULL, @StockInDocId, NULL, N'StockDocument', @StockInDocId, @Now, @Now),
        (@JE6, (SELECT Id FROM dbo.Accounts WHERE Code = N'6000'), 1, 7.650, 0.000, N'صرف داخلي للمخزون', NULL, NULL, NULL, @CashierId, @WarehouseId, @BranchId, NULL, NULL, @StockOutDocId, NULL, N'StockDocument', @StockOutDocId, @Now, @Now),
        (@JE6, (SELECT Id FROM dbo.Accounts WHERE Code = N'1300'), 2, 0.000, 7.650, N'تخفيض المخزون', NULL, NULL, NULL, @CashierId, @WarehouseId, @BranchId, NULL, NULL, @StockOutDocId, NULL, N'StockDocument', @StockOutDocId, @Now, @Now);
    END;

    /* Re-enable constraints */
    SET @sql = N'';
    SELECT @sql += N'ALTER TABLE [' + s.name + N'].[' + t.name + N'] WITH CHECK CHECK CONSTRAINT ALL;'
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory';
    EXEC sp_executesql @sql;

    COMMIT;

    SELECT
        (SELECT COUNT(*) FROM dbo.[User]) AS UsersCount,
        (SELECT COUNT(*) FROM dbo.Category) AS CategoriesCount,
        (SELECT COUNT(*) FROM dbo.SubCategory) AS SubCategoriesCount,
        (SELECT COUNT(*) FROM dbo.Brand) AS BrandsCount,
        (SELECT COUNT(*) FROM dbo.Unit) AS UnitsCount,
        (SELECT COUNT(*) FROM dbo.Product) AS ProductsCount,
        (SELECT COUNT(*) FROM dbo.ProductUnit) AS ProductUnitsCount,
        (SELECT COUNT(*) FROM dbo.Stock) AS StockRowsCount,
        (SELECT COUNT(*) FROM dbo.StockDocument) AS StockDocumentsCount,
        (SELECT COUNT(*) FROM dbo.Invoice) AS InvoicesCount,
        (SELECT COUNT(*) FROM dbo.Voucher) AS VouchersCount,
        (SELECT COUNT(*) FROM dbo.FinancialTransaction) AS FinancialTransactionsCount,
        CASE WHEN OBJECT_ID(N'dbo.Accounts', N'U') IS NOT NULL THEN (SELECT COUNT(*) FROM dbo.Accounts) ELSE 0 END AS AccountsCount,
        CASE WHEN OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL THEN (SELECT COUNT(*) FROM dbo.JournalEntries) ELSE 0 END AS JournalEntriesCount;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
