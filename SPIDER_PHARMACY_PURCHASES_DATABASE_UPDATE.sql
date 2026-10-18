/*
  Spider Pharmacy purchasing deployment script.
  STATUS: MANUAL ONLY. Review and test on a backup/staging database first.
  This script contains the non-destructive schema for the implemented purchase
  workflow slices: PR, PO traceability, GRN, invoice matching, allocations,
  advances, and purchase returns. No production database changes were executed.
*/
SET NOCOUNT ON;

IF COL_LENGTH(N'dbo.PurchaseOrderLines', N'PurchaseRequestLineId') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderLines ADD PurchaseRequestLineId int NULL;
END;

/* PR lines support free-text item names. ProductId remains optional because a
   PR may be prepared before catalog matching; PO/GRN posting still requires a
   real catalog product and unit. */
IF COL_LENGTH(N'dbo.PurchaseRequestLines', N'ProductId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PurchaseRequestLines ALTER COLUMN ProductId int NULL;
END;

IF COL_LENGTH(N'dbo.PurchaseRequestLines', N'ItemName') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseRequestLines ADD ItemName nvarchar(500) NULL;
END;

IF COL_LENGTH(N'dbo.PurchaseRequestLines', N'Description') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseRequestLines ADD Description nvarchar(max) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseOrderLines_PurchaseRequestLineId' AND object_id = OBJECT_ID(N'dbo.PurchaseOrderLines'))
    CREATE INDEX IX_PurchaseOrderLines_PurchaseRequestLineId ON dbo.PurchaseOrderLines (PurchaseRequestLineId);

IF OBJECT_ID(N'dbo.PurchaseRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseRequests
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseRequests PRIMARY KEY,
        RequestNumber nvarchar(100) NOT NULL,
        Status int NOT NULL CONSTRAINT DF_PurchaseRequests_Status DEFAULT (0),
        RequestDate datetime2 NOT NULL,
        RequesterId int NULL,
        BranchId int NULL,
        Department nvarchar(max) NULL,
        Priority nvarchar(max) NULL,
        Justification nvarchar(max) NULL,
        Notes nvarchar(max) NULL,
        SubmittedBy int NULL,
        SubmittedDate datetime2 NULL,
        ApprovedBy int NULL,
        ApprovedDate datetime2 NULL,
        RejectedBy int NULL,
        RejectedDate datetime2 NULL,
        RejectionReason nvarchar(max) NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        CONSTRAINT UQ_PurchaseRequests_RequestNumber UNIQUE (RequestNumber)
    );
END;

IF OBJECT_ID(N'dbo.PurchaseRequestLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseRequestLines
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseRequestLines PRIMARY KEY,
        PurchaseRequestId int NOT NULL,
        ProductId int NOT NULL,
        ProductUnitId int NULL,
        RequestedQuantity decimal(18,6) NOT NULL,
        ApprovedQuantity decimal(18,6) NOT NULL,
        AvailableStockSnapshot decimal(18,6) NOT NULL,
        ReservedStockSnapshot decimal(18,6) NOT NULL,
        AlreadyOrderedQuantitySnapshot decimal(18,6) NOT NULL,
        MinimumStockSnapshot decimal(18,6) NOT NULL,
        AverageSalesSnapshot decimal(18,6) NOT NULL,
        LastPurchaseDateSnapshot datetime2 NULL,
        ExpectedStockoutDate datetime2 NULL,
        Notes nvarchar(max) NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        CONSTRAINT FK_PurchaseRequestLines_PurchaseRequests FOREIGN KEY (PurchaseRequestId) REFERENCES dbo.PurchaseRequests(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_PurchaseRequestLines_Product UNIQUE (PurchaseRequestId, ProductId, ProductUnitId)
    );
END;

IF COL_LENGTH(N'dbo.PurchaseOrderLines', N'PurchaseRequestLineId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrderLines_PurchaseRequestLines')
BEGIN
    ALTER TABLE dbo.PurchaseOrderLines ADD CONSTRAINT FK_PurchaseOrderLines_PurchaseRequestLines
        FOREIGN KEY (PurchaseRequestLineId) REFERENCES dbo.PurchaseRequestLines(Id);
END;

IF OBJECT_ID(N'dbo.GoodsReceipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoodsReceipts
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GoodsReceipts PRIMARY KEY,
        ReceiptNumber nvarchar(100) NOT NULL,
        PurchaseOrderId int NOT NULL,
        SupplierId int NULL,
        WarehouseId int NULL,
        BranchId int NULL,
        ReceivedBy int NULL,
        ReceiptDate datetime2 NOT NULL,
        Status int NOT NULL CONSTRAINT DF_GoodsReceipts_Status DEFAULT (0),
        ConfirmedDate datetime2 NULL,
        Notes nvarchar(max) NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        CONSTRAINT UQ_GoodsReceipts_ReceiptNumber UNIQUE (ReceiptNumber),
        CONSTRAINT FK_GoodsReceipts_PurchaseOrders FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrders(Id)
    );
END;

IF OBJECT_ID(N'dbo.GoodsReceiptLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoodsReceiptLines
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GoodsReceiptLines PRIMARY KEY,
        GoodsReceiptId int NOT NULL,
        PurchaseOrderLineId int NOT NULL,
        ProductId int NOT NULL,
        ProductUnitId int NOT NULL,
        QuantityShipped decimal(18,6) NOT NULL,
        QuantityReceived decimal(18,6) NOT NULL,
        QuantityAccepted decimal(18,6) NOT NULL,
        QuantityRejected decimal(18,6) NOT NULL,
        QuantityDamaged decimal(18,6) NOT NULL,
        FreeQuantity decimal(18,6) NOT NULL,
        BatchNumber nvarchar(max) NULL,
        ExpiryDate datetime2 NULL,
        StorageLocation nvarchar(max) NULL,
        RejectionReason nvarchar(max) NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        CONSTRAINT FK_GoodsReceiptLines_GoodsReceipts FOREIGN KEY (GoodsReceiptId) REFERENCES dbo.GoodsReceipts(Id) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GoodsReceiptLines_Receipt_PO' AND object_id = OBJECT_ID(N'dbo.GoodsReceiptLines'))
    CREATE INDEX IX_GoodsReceiptLines_Receipt_PO ON dbo.GoodsReceiptLines (GoodsReceiptId, PurchaseOrderLineId);

IF OBJECT_ID(N'dbo.PurchaseInvoiceMatches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseInvoiceMatches
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseInvoiceMatches PRIMARY KEY,
        InvoiceId int NOT NULL,
        PurchaseOrderId int NULL,
        SupplierId int NULL,
        SupplierInvoiceNumber nvarchar(max) NULL,
        Status int NOT NULL CONSTRAINT DF_PurchaseInvoiceMatches_Status DEFAULT (0),
        PriceTolerance decimal(18,6) NOT NULL,
        QuantityTolerance decimal(18,6) NOT NULL,
        DiscrepancyReason nvarchar(max) NULL,
        MatchedDate datetime2 NULL,
        MatchedBy int NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        CONSTRAINT UQ_PurchaseInvoiceMatches_Invoice UNIQUE (InvoiceId),
        CONSTRAINT FK_PurchaseInvoiceMatches_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoice(Id)
    );
END;

IF OBJECT_ID(N'dbo.PurchaseInvoiceMatchLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseInvoiceMatchLines
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseInvoiceMatchLines PRIMARY KEY,
        PurchaseInvoiceMatchId int NOT NULL,
        InvoiceLineId int NOT NULL,
        PurchaseOrderLineId int NOT NULL,
        InvoicedQuantity decimal(18,6) NOT NULL,
        AcceptedQuantity decimal(18,6) NOT NULL,
        InvoicedUnitPrice decimal(18,6) NOT NULL,
        OrderedUnitPrice decimal(18,6) NOT NULL,
        PriceVariance decimal(18,6) NOT NULL,
        QuantityVariance decimal(18,6) NOT NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        CONSTRAINT FK_PurchaseInvoiceMatchLines_Matches FOREIGN KEY (PurchaseInvoiceMatchId) REFERENCES dbo.PurchaseInvoiceMatches(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_PurchaseInvoiceMatchLines_InvoiceLine UNIQUE (PurchaseInvoiceMatchId, InvoiceLineId)
    );
END;

IF OBJECT_ID(N'dbo.SupplierPaymentAllocations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierPaymentAllocations
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplierPaymentAllocations PRIMARY KEY,
        VoucherId int NOT NULL,
        InvoiceId int NOT NULL,
        SupplierId int NOT NULL,
        Amount decimal(18,6) NOT NULL,
        AllocationDate datetime2 NOT NULL,
        CreatedBy int NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        CONSTRAINT UQ_SupplierPaymentAllocations_Voucher_Invoice UNIQUE (VoucherId, InvoiceId),
        CONSTRAINT FK_SupplierPaymentAllocations_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoice(Id),
        CONSTRAINT FK_SupplierPaymentAllocations_Vouchers FOREIGN KEY (VoucherId) REFERENCES dbo.Voucher(Id)
    );
END;

IF OBJECT_ID(N'dbo.SupplierAdvances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierAdvances
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplierAdvances PRIMARY KEY,
        VoucherId int NOT NULL,
        SupplierId int NOT NULL,
        Amount decimal(18,6) NOT NULL,
        AllocatedAmount decimal(18,6) NOT NULL,
        AdvanceDate datetime2 NOT NULL,
        CreatedBy int NULL,
        Notes nvarchar(max) NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        CONSTRAINT UQ_SupplierAdvances_Voucher UNIQUE (VoucherId),
        CONSTRAINT FK_SupplierAdvances_Vouchers FOREIGN KEY (VoucherId) REFERENCES dbo.Voucher(Id)
    );
END;

IF OBJECT_ID(N'dbo.PurchaseReturns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseReturns
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseReturns PRIMARY KEY,
        ReturnNumber nvarchar(100) NOT NULL,
        SupplierId int NULL,
        OriginalInvoiceId int NULL,
        GoodsReceiptId int NULL,
        Status int NOT NULL CONSTRAINT DF_PurchaseReturns_Status DEFAULT (0),
        Reason nvarchar(max) NULL,
        ReturnDate datetime2 NOT NULL,
        CreatedBy int NULL,
        ConfirmedBy int NULL,
        ConfirmedDate datetime2 NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        CONSTRAINT UQ_PurchaseReturns_ReturnNumber UNIQUE (ReturnNumber)
    );
END;

IF OBJECT_ID(N'dbo.PurchaseReturnLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseReturnLines
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseReturnLines PRIMARY KEY,
        PurchaseReturnId int NOT NULL,
        GoodsReceiptLineId int NOT NULL,
        ProductId int NOT NULL,
        ProductUnitId int NOT NULL,
        Quantity decimal(18,6) NOT NULL,
        BatchNumber nvarchar(max) NULL,
        ExpiryDate datetime2 NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL,
        CONSTRAINT FK_PurchaseReturnLines_Returns FOREIGN KEY (PurchaseReturnId) REFERENCES dbo.PurchaseReturns(Id) ON DELETE CASCADE
    );
END;

SELECT 'PR/GRN/invoice-match/payment-allocation/return schema preflight completed. No demo Excel data was imported.' AS DeploymentStatus;

SELECT name AS RequiredTable
FROM sys.tables
WHERE name IN (N'PurchaseRequests', N'PurchaseRequestLines', N'GoodsReceipts', N'GoodsReceiptLines', N'PurchaseInvoiceMatches', N'PurchaseInvoiceMatchLines', N'SupplierPaymentAllocations', N'SupplierAdvances', N'PurchaseReturns', N'PurchaseReturnLines')
ORDER BY name;

SELECT c.name AS PurchaseOrderLineColumn
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.PurchaseOrderLines')
  AND c.name = N'PurchaseRequestLineId';

SELECT name AS RequiredIndex
FROM sys.indexes
WHERE name IN (N'IX_PurchaseOrderLines_PurchaseRequestLineId', N'IX_GoodsReceiptLines_Receipt_PO');

/* Free-text purchase-control register. It intentionally has no ProductId,
   ProductUnitId, quantity, tax, discount, or product foreign key. */
IF OBJECT_ID(N'dbo.PurchaseControlEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseControlEntries
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseControlEntries PRIMARY KEY,
        PaymentMethod nvarchar(200) NULL,
        PurchaseOrderNumber nvarchar(200) NULL,
        PurchaseOrderDate datetime2 NULL,
        PurchaseRequestNumber nvarchar(200) NULL,
        SupplierName nvarchar(300) NULL,
        PurchaseOrderAmount decimal(18,6) NULL,
        InvoiceNumber nvarchar(200) NULL,
        InvoiceDate datetime2 NULL,
        DeliveryDate datetime2 NULL,
        Description nvarchar(max) NULL,
        Category nvarchar(200) NULL,
        PaidAmount decimal(18,6) NULL,
        DueAmount decimal(18,6) NULL,
        ReceiptAmount decimal(18,6) NULL,
        PaymentStatus nvarchar(200) NULL,
        DueDate datetime2 NULL,
        LinkCheck nvarchar(200) NULL,
        Notes nvarchar(max) NULL,
        OverdueDays int NULL,
        ReceivedValue decimal(18,6) NULL,
        CreatedDate datetime2 NOT NULL,
        UpdatedDate datetime2 NOT NULL
    );
    CREATE INDEX IX_PurchaseControlEntries_PODate ON dbo.PurchaseControlEntries(PurchaseOrderDate);
    CREATE INDEX IX_PurchaseControlEntries_Supplier ON dbo.PurchaseControlEntries(SupplierName);
    CREATE INDEX IX_PurchaseControlEntries_Invoice ON dbo.PurchaseControlEntries(InvoiceNumber);
END;

/* PR -> PO linkage verification.
   The application creates PO lines only from approved PR lines and stores the
   relationship in PurchaseOrderLines.PurchaseRequestLineId. */
IF COL_LENGTH(N'dbo.PurchaseOrderLines', N'PurchaseRequestLineId') IS NOT NULL
   AND OBJECT_ID(N'dbo.PurchaseOrders', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.PurchaseRequestLines', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.PurchaseRequests', N'U') IS NOT NULL
BEGIN
    SELECT
        po.OrderNumber,
        po.Status AS PurchaseOrderStatus,
        pr.RequestNumber,
        pr.Status AS PurchaseRequestStatus,
        pol.Id AS PurchaseOrderLineId,
        pol.PurchaseRequestLineId,
        pol.ProductId,
        pol.Quantity
    FROM dbo.PurchaseOrderLines pol
    INNER JOIN dbo.PurchaseOrders po ON po.Id = pol.PurchaseOrderId
    INNER JOIN dbo.PurchaseRequestLines prl ON prl.Id = pol.PurchaseRequestLineId
    INNER JOIN dbo.PurchaseRequests pr ON pr.Id = prl.PurchaseRequestId
    ORDER BY po.Id DESC, pol.Id;
END;
