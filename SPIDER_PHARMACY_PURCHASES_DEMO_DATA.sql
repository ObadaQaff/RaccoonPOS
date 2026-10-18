/*
  Spider Pharmacy purchasing demo data.
  MANUAL / TEST DATABASE ONLY.
  This script is intentionally separate from the deployment script.
  It creates linked PR -> PO -> GRN examples and Purchase Control rows.
  It does not create products, suppliers, users, payments, or accounting entries.
  Run only on a backup/staging database after the production schema script.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @now datetime2 = SYSDATETIME();
DECLARE @productId int;
DECLARE @productUnitId int;
DECLARE @supplierId int;
DECLARE @userId int;
DECLARE @prId int;
DECLARE @prLineId int;
DECLARE @poId int;
DECLARE @poLineId int;
DECLARE @receiptId int;

/* Use the first active catalog item with a purchase unit. */
SELECT TOP (1)
    @productId = p.Id,
    @productUnitId = pu.Id,
    @supplierId = COALESCE(p.SupplierId, u.Id)
FROM dbo.Products p
INNER JOIN dbo.ProductUnits pu
    ON pu.ProductId = p.Id
LEFT JOIN dbo.Users u
    ON u.Id = p.SupplierId
WHERE ISNULL(p.IsDeleted, 0) = 0
ORDER BY p.Id;

SELECT TOP (1) @userId = Id
FROM dbo.Users
ORDER BY Id;

IF @productId IS NULL OR @productUnitId IS NULL
    THROW 51000, 'Demo seed requires at least one existing product with a product unit.', 1;

IF @userId IS NULL
    THROW 51001, 'Demo seed requires at least one existing user.', 1;

/* Approved PR that can be converted to a linked PO. */
IF NOT EXISTS (SELECT 1 FROM dbo.PurchaseRequests WHERE RequestNumber = N'DEMO-PR-0001')
BEGIN
    INSERT dbo.PurchaseRequests
    (
        RequestNumber, Status, RequestDate, RequesterId, Department, Priority,
        Justification, Notes, ApprovedBy, ApprovedDate, CreatedDate, UpdatedDate
    )
    VALUES
    (N'DEMO-PR-0001', 2, CAST(@now AS date), @userId, N'Pharmacy', N'HIGH',
     N'Demo approved pharmacy replenishment request.', N'DEMO DATA', @userId, @now, @now, @now);

    SET @prId = SCOPE_IDENTITY();

    INSERT dbo.PurchaseRequestLines
    (
        PurchaseRequestId, ProductId, ProductUnitId, RequestedQuantity, ApprovedQuantity,
        AvailableStockSnapshot, ReservedStockSnapshot, AlreadyOrderedQuantitySnapshot,
        MinimumStockSnapshot, AverageSalesSnapshot, Notes, CreatedDate, UpdatedDate
    )
    VALUES
    (@prId, @productId, @productUnitId, 10, 10, 2, 0, 0, 5, 1.5, N'DEMO DATA', @now, @now);
END;

SELECT @prId = Id
FROM dbo.PurchaseRequests
WHERE RequestNumber = N'DEMO-PR-0001';

SELECT TOP (1) @prLineId = Id
FROM dbo.PurchaseRequestLines
WHERE PurchaseRequestId = @prId
ORDER BY Id;

/* Submitted PR for approval testing. */
IF NOT EXISTS (SELECT 1 FROM dbo.PurchaseRequests WHERE RequestNumber = N'DEMO-PR-0002')
BEGIN
    INSERT dbo.PurchaseRequests
    (
        RequestNumber, Status, RequestDate, RequesterId, Department, Priority,
        Justification, Notes, SubmittedBy, SubmittedDate, CreatedDate, UpdatedDate
    )
    VALUES
    (N'DEMO-PR-0002', 1, CAST(@now AS date), @userId, N'Warehouse', N'MEDIUM',
     N'Demo submitted request awaiting manager approval.', N'DEMO DATA', @userId, @now, @now, @now);

    SET @prId = SCOPE_IDENTITY();

    INSERT dbo.PurchaseRequestLines
    (
        PurchaseRequestId, ProductId, ProductUnitId, RequestedQuantity, ApprovedQuantity,
        Notes, CreatedDate, UpdatedDate
    )
    VALUES
    (@prId, @productId, @productUnitId, 5, 5, N'DEMO DATA', @now, @now);
END;

/* Rejected PR for negative workflow testing. */
IF NOT EXISTS (SELECT 1 FROM dbo.PurchaseRequests WHERE RequestNumber = N'DEMO-PR-0003')
BEGIN
    INSERT dbo.PurchaseRequests
    (
        RequestNumber, Status, RequestDate, RequesterId, Department, Priority,
        Justification, Notes, RejectedBy, RejectedDate, RejectionReason,
        CreatedDate, UpdatedDate
    )
    VALUES
    (N'DEMO-PR-0003', 3, CAST(@now AS date), @userId, N'Administration', N'LOW',
     N'Demo rejected request.', N'DEMO DATA', @userId, @now, N'Demo rejection reason', @now, @now);

    SET @prId = SCOPE_IDENTITY();

    INSERT dbo.PurchaseRequestLines
    (
        PurchaseRequestId, ProductId, ProductUnitId, RequestedQuantity, ApprovedQuantity,
        Notes, CreatedDate, UpdatedDate
    )
    VALUES
    (@prId, @productId, @productUnitId, 3, 0, N'DEMO DATA', @now, @now);
END;

/* Linked PO with a partial receipt example: ordered 10, received 6. */
IF NOT EXISTS (SELECT 1 FROM dbo.PurchaseOrders WHERE OrderNumber = N'DEMO-PO-0001')
BEGIN
    INSERT dbo.PurchaseOrders
    (
        OrderNumber, Status, SupplierId, DocumentDate, DueDate, Notes,
        SubTotal, TotalTax, TotalAmount, CostPriceIncludesTax,
        CreatedBy, SubmittedBy, CreatedDate, UpdatedDate
    )
    VALUES
    (N'DEMO-PO-0001', 2, @supplierId, CAST(@now AS date), DATEADD(day, 30, CAST(@now AS date)),
     N'DEMO DATA - linked to DEMO-PR-0001', 100, 0, 100, 1, @userId, @userId, @now, @now);

    SET @poId = SCOPE_IDENTITY();

    INSERT dbo.PurchaseOrderLines
    (
        PurchaseOrderId, PurchaseRequestLineId, ProductId, ProductUnitId,
        Quantity, QuantityPerUnitSnapshot, BaseQuantity, UnitPrice, UnitCost,
        LineDiscountAmount, FreeQuantity, TaxExempt, TaxRate, TaxAmount,
        LineSubTotal, CreatedDate, UpdatedDate
    )
    VALUES
    (@poId, @prLineId, @productId, @productUnitId,
     10, 1, 10, 10, 10, 0, 0, 1, 0, 0, 100, @now, @now);
END;

SELECT @poId = Id
FROM dbo.PurchaseOrders
WHERE OrderNumber = N'DEMO-PO-0001';

SELECT TOP (1) @poLineId = Id
FROM dbo.PurchaseOrderLines
WHERE PurchaseOrderId = @poId
ORDER BY Id;

IF NOT EXISTS (SELECT 1 FROM dbo.GoodsReceipts WHERE ReceiptNumber = N'DEMO-GRN-0001')
BEGIN
    INSERT dbo.GoodsReceipts
    (
        ReceiptNumber, PurchaseOrderId, SupplierId, ReceivedBy, ReceiptDate,
        Status, ConfirmedDate, Notes, CreatedDate, UpdatedDate
    )
    VALUES
    (N'DEMO-GRN-0001', @poId, @supplierId, @userId, CAST(@now AS date),
     2, @now, N'DEMO DATA - partial receipt 6 of 10', @now, @now);

    SET @receiptId = SCOPE_IDENTITY();

    INSERT dbo.GoodsReceiptLines
    (
        GoodsReceiptId, PurchaseOrderLineId, ProductId, ProductUnitId,
        QuantityShipped, QuantityReceived, QuantityAccepted, QuantityRejected,
        QuantityDamaged, FreeQuantity, BatchNumber, ExpiryDate, StorageLocation,
        CreatedDate, UpdatedDate
    )
    VALUES
    (@receiptId, @poLineId, @productId, @productUnitId,
     10, 6, 6, 0, 0, 0, N'DEMO-BATCH-001', DATEADD(month, 18, CAST(@now AS date)), N'DEMO-A1', @now, @now);
END;

/* Dashboard/control-register examples. */
IF OBJECT_ID(N'dbo.PurchaseControlEntries', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.PurchaseControlEntries WHERE PurchaseOrderNumber = N'DEMO-PO-0001')
    BEGIN
        INSERT dbo.PurchaseControlEntries
        (
            PaymentMethod, PurchaseOrderNumber, PurchaseOrderDate, PurchaseRequestNumber,
            SupplierName, PurchaseOrderAmount, InvoiceNumber, InvoiceDate, DeliveryDate,
            Description, Category, PaidAmount, DueAmount, ReceiptAmount, PaymentStatus,
            DueDate, LinkCheck, Notes, OverdueDays, ReceivedValue, CreatedDate, UpdatedDate
        )
        VALUES
        (N'Cash', N'DEMO-PO-0001', CAST(@now AS date), N'DEMO-PR-0001',
         N'Demo Supplier', 100, N'DEMO-INV-0001', CAST(@now AS date), CAST(@now AS date),
         N'Demo pharmacy purchase', N'Medicines for resale', 40, 60, 60, N'Partially paid',
         DATEADD(day, 30, CAST(@now AS date)), N'PR-PO-GRN linked', N'DEMO DATA', 0, 60, @now, @now);
    END;
END;

COMMIT TRANSACTION;

SELECT N'Demo purchasing data inserted or already existed.' AS DemoStatus;
SELECT RequestNumber, Status FROM dbo.PurchaseRequests WHERE RequestNumber LIKE N'DEMO-PR-%';
SELECT OrderNumber, Status FROM dbo.PurchaseOrders WHERE OrderNumber = N'DEMO-PO-0001';
SELECT ReceiptNumber, Status FROM dbo.GoodsReceipts WHERE ReceiptNumber = N'DEMO-GRN-0001';
