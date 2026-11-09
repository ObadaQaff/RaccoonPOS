/*
  Run against db_acff96_pharmacdevelopment.
  Repairs the first imported invoice (137909) so its stock lots link to
  invoice lines and the purchase-invoice screen can display sale prices.
  Take a backup first.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

;WITH InvoiceLines AS
(
    SELECT
        l.Id AS InvoiceLineId,
        l.ProductId,
        l.ProductUnitId,
        ROW_NUMBER() OVER (PARTITION BY l.ProductId, l.ProductUnitId ORDER BY l.Id) AS RowNumber
    FROM dbo.InvoiceLine l
    INNER JOIN dbo.Invoice i ON i.Id = l.InvoiceId
    WHERE i.InvoiceNumber = N'137909'
), StockLots AS
(
    SELECT
        sl.Id AS StockLotId,
        sl.ProductId,
        sl.ProductUnitId,
        ROW_NUMBER() OVER (PARTITION BY sl.ProductId, sl.ProductUnitId ORDER BY sl.Id) AS RowNumber
    FROM dbo.StockLot sl
    INNER JOIN dbo.StockTransaction st ON st.StockLotId = sl.Id
    INNER JOIN dbo.Invoice i ON i.Id = st.InvoiceId
    WHERE i.InvoiceNumber = N'137909'
)
UPDATE sl
SET SourcePurchaseLineId = il.InvoiceLineId
FROM dbo.StockLot sl
INNER JOIN StockLots x ON x.StockLotId = sl.Id
INNER JOIN InvoiceLines il
    ON il.ProductId = x.ProductId
   AND il.ProductUnitId = x.ProductUnitId
   AND il.RowNumber = x.RowNumber;

COMMIT TRANSACTION;

SELECT
    COUNT_BIG(*) AS LinkedLots,
    COUNT_BIG(CASE WHEN sl.SalePrice > 0 THEN 1 END) AS LotsWithSalePrice
FROM dbo.StockLot sl
INNER JOIN dbo.InvoiceLine l ON l.Id = sl.SourcePurchaseLineId
INNER JOIN dbo.Invoice i ON i.Id = l.InvoiceId
WHERE i.InvoiceNumber = N'137909';
