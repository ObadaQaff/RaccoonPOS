/*
  Controlled repair for the four confirmed posted purchase invoices.

  IMPORTANT:
    - Run PurchaseInvoiceReconciliationBackup.sql first.
    - This script is PREVIEW-ONLY by default.
    - Change @ApplyRepair to 1 only after reviewing the preview and confirming
      that the scoped backup tables exist.
    - It does not modify stock lots or stock transactions.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ApplyRepair bit = 0;

IF OBJECT_ID(N'dbo.RepairBackup_20261116_Invoice', N'U') IS NULL
    THROW 51000, 'Scoped backup is missing. Run the backup script first.', 1;

DECLARE @Corrections TABLE
(
    InvoiceId int PRIMARY KEY,
    CorrectTotal decimal(18, 6) NOT NULL,
    CorrectSubtotal decimal(18, 6) NOT NULL,
    CorrectTax decimal(18, 6) NOT NULL
);

INSERT INTO @Corrections (InvoiceId, CorrectTotal, CorrectSubtotal, CorrectTax)
VALUES
    (1187, 112.747000, 98.453000, 14.294000),
    (1188,  59.218000, 51.050000,  8.168000),
    (1191, 143.344000,135.760000,  7.584000),
    (1192,  13.639000, 13.114000,  0.525000);

IF EXISTS
(
    SELECT 1
    FROM @Corrections c
    LEFT JOIN dbo.Invoice i ON i.Id = c.InvoiceId
    WHERE i.Id IS NULL OR i.InvoiceType <> 2 OR i.Status <> 2
)
    THROW 51000, 'A repair target is missing or is not a posted purchase invoice.', 1;

IF EXISTS
(
    SELECT 1
    FROM @Corrections c
    WHERE (SELECT COUNT(*) FROM dbo.InvoicePayments p WHERE p.InvoiceId = c.InvoiceId) <> 1
)
    THROW 51000, 'A repair target does not have exactly one payment row; stop for manual review.', 1;

IF EXISTS
(
    SELECT 1
    FROM @Corrections c
    WHERE (SELECT COUNT(*) FROM dbo.JournalEntries j
           WHERE j.ReferenceType = N'Invoice'
             AND j.ReferenceId = c.InvoiceId
             AND j.Status = 2) <> 1
)
    THROW 51000, 'A repair target does not have exactly one posted invoice journal; stop for manual review.', 1;

/* Preview the exact changes before any write. */
SELECT
    c.InvoiceId,
    i.InvoiceNumber,
    i.TotalAmount AS StoredTotal,
    c.CorrectTotal,
    ROUND(i.TotalAmount - c.CorrectTotal, 3) AS TotalDifference,
    i.SubTotal AS StoredSubtotal,
    c.CorrectSubtotal,
    i.TotalTax AS StoredTax,
    c.CorrectTax,
    p.Amount AS StoredPayment,
    j.Id AS PostedJournalId
FROM @Corrections c
INNER JOIN dbo.Invoice i ON i.Id = c.InvoiceId
INNER JOIN dbo.InvoicePayments p ON p.InvoiceId = c.InvoiceId
INNER JOIN dbo.JournalEntries j
    ON j.ReferenceType = N'Invoice'
   AND j.ReferenceId = c.InvoiceId
   AND j.Status = 2
ORDER BY c.InvoiceId;

IF @ApplyRepair = 0
    RETURN;

BEGIN TRANSACTION;

/* Correct the invoice header and its single payment row. */
UPDATE i
SET i.SubTotal = c.CorrectSubtotal,
    i.TotalTax = c.CorrectTax,
    i.TotalAmount = c.CorrectTotal,
    i.NetSales = c.CorrectSubtotal - ISNULL(i.DiscountAmount, 0),
    i.UpdatedDate = SYSDATETIME()
FROM dbo.Invoice i
INNER JOIN @Corrections c ON c.InvoiceId = i.Id;

UPDATE p
SET p.Amount = c.CorrectTotal,
    p.UpdatedDate = SYSDATETIME()
FROM dbo.InvoicePayments p
INNER JOIN @Corrections c ON c.InvoiceId = p.InvoiceId;

/* Correct line subtotal/tax snapshots using the fixed bonus-before-tax rule. */
;WITH RawLines AS
(
    SELECT
        l.Id,
        l.InvoiceId,
        l.Quantity,
        l.UnitPrice,
        ISNULL(l.LineDiscountAmount, 0) AS LineDiscountAmount,
        l.TaxRate,
        l.TaxExempt,
        l.BonusPercentage,
        i.CostPriceIncludesTax,
        (l.Quantity * l.UnitPrice - ISNULL(l.LineDiscountAmount, 0)) AS GrossAmount
    FROM dbo.InvoiceLine l
    INNER JOIN dbo.Invoice i ON i.Id = l.InvoiceId
    INNER JOIN @Corrections c ON c.InvoiceId = l.InvoiceId
), Adjusted AS
(
    SELECT *,
        CASE WHEN BonusPercentage IS NOT NULL AND Quantity > 0
            THEN GrossAmount / (1 + BonusPercentage / 100.0)
            ELSE GrossAmount END AS AdjustedAmount
    FROM RawLines
), Subtotals AS
(
    SELECT *,
        ROUND(CASE WHEN CostPriceIncludesTax = 1 AND TaxExempt = 0 AND TaxRate > 0
            THEN AdjustedAmount / (1 + TaxRate / 100.0)
            ELSE AdjustedAmount END, 3) AS CorrectLineSubtotal
    FROM Adjusted
), Taxed AS
(
    SELECT *,
        ROUND(CASE WHEN TaxExempt = 1 OR TaxRate <= 0 THEN 0
            WHEN CostPriceIncludesTax = 1 THEN AdjustedAmount - CorrectLineSubtotal
            ELSE CorrectLineSubtotal * TaxRate / 100.0 END, 3) AS TaxBeforeDiscount
    FROM Subtotals
), Ratios AS
(
    SELECT t.*,
        i.DiscountAmount AS InvoiceDiscount,
        CASE WHEN SUM(CorrectLineSubtotal) OVER (PARTITION BY InvoiceId) > 0
            THEN (SUM(CorrectLineSubtotal) OVER (PARTITION BY InvoiceId)
                  - ISNULL(i.DiscountAmount, 0))
                 / SUM(CorrectLineSubtotal) OVER (PARTITION BY InvoiceId)
            ELSE 0 END AS TaxRatio
    FROM Taxed t
    INNER JOIN dbo.Invoice i ON i.Id = t.InvoiceId
)
UPDATE l
SET l.LineSubTotal = r.CorrectLineSubtotal,
    l.TaxAmount = ROUND(r.TaxBeforeDiscount * r.TaxRatio, 3),
    l.ProfitBeforeTax = r.CorrectLineSubtotal - ISNULL(l.PurchaseAcquisitionCost, l.Quantity * l.UnitCost),
    l.Profit = r.CorrectLineSubtotal - ISNULL(l.PurchaseAcquisitionCost, l.Quantity * l.UnitCost),
    l.UpdatedDate = SYSDATETIME()
FROM dbo.InvoiceLine l
INNER JOIN Ratios r ON r.Id = l.Id;

/* Reverse each original journal, preserving the audit trail. */
INSERT INTO dbo.JournalEntries
(
    EntryNumber, EntryDate, Description, Status, ReferenceType, ReferenceId,
    CreatedDate, UpdatedDate, IsDraft
)
SELECT
    CONCAT(N'REPAIR-REV-', j.Id, N'-', CONVERT(char(8), SYSDATETIME(), 112)),
    SYSDATETIME(),
    CONCAT(N'Reversal of ', j.EntryNumber, N': purchase invoice bonus-total repair'),
    2,
    N'Reversal',
    j.Id,
    SYSDATETIME(),
    SYSDATETIME(),
    0
FROM dbo.JournalEntries j
INNER JOIN @Corrections c ON c.InvoiceId = j.ReferenceId
WHERE j.ReferenceType = N'Invoice'
  AND j.Status = 2;

INSERT INTO dbo.JournalEntryLines
(
    JournalEntryId, AccountId, Debit, Credit, Description, CreatedDate,
    UpdatedDate, LineNumber, PartyUserId, CustomerId, SupplierId, CashierId,
    WarehouseId, CostCenterId, BranchId, InvoiceId, VoucherId,
    StockDocumentId, FinancialTransactionId, ReferenceType, ReferenceId,
    CurrencyId, ForeignAmount, ExchangeRate, TaxRateId, TaxAmount
)
SELECT
    reversal.Id,
    line.AccountId,
    line.Credit,
    line.Debit,
    CONCAT(N'Reversal - ', line.Description),
    SYSDATETIME(),
    SYSDATETIME(),
    line.LineNumber,
    line.PartyUserId,
    line.CustomerId,
    line.SupplierId,
    line.CashierId,
    line.WarehouseId,
    line.CostCenterId,
    line.BranchId,
    line.InvoiceId,
    line.VoucherId,
    line.StockDocumentId,
    line.FinancialTransactionId,
    N'Reversal',
    line.Id,
    line.CurrencyId,
    line.ForeignAmount,
    line.ExchangeRate,
    line.TaxRateId,
    line.TaxAmount
FROM dbo.JournalEntries original
INNER JOIN dbo.JournalEntryLines line ON line.JournalEntryId = original.Id
INNER JOIN dbo.JournalEntries reversal
    ON reversal.ReferenceType = N'Reversal'
   AND reversal.ReferenceId = original.Id
WHERE original.ReferenceType = N'Invoice'
  AND original.Status = 2
  AND EXISTS (SELECT 1 FROM @Corrections c WHERE c.InvoiceId = original.ReferenceId);

/* Post corrected journal entries using the original account identities. */
INSERT INTO dbo.JournalEntries
(
    EntryNumber, EntryDate, Description, Status, ReferenceType, ReferenceId,
    CreatedDate, UpdatedDate, IsDraft
)
SELECT
    CONCAT(N'REPAIR-CORR-', i.Id, N'-', CONVERT(char(8), SYSDATETIME(), 112)),
    SYSDATETIME(),
    CONCAT(N'Corrected purchase invoice #', i.InvoiceNumber, N' bonus-total repair'),
    2,
    N'Invoice',
    i.Id,
    SYSDATETIME(),
    SYSDATETIME(),
    0
FROM dbo.Invoice i
INNER JOIN @Corrections c ON c.InvoiceId = i.Id;

INSERT INTO dbo.JournalEntryLines
(
    JournalEntryId, AccountId, Debit, Credit, Description, CreatedDate,
    UpdatedDate, LineNumber, PartyUserId, CustomerId, SupplierId, CashierId,
    WarehouseId, CostCenterId, BranchId, InvoiceId, VoucherId,
    StockDocumentId, FinancialTransactionId, ReferenceType, ReferenceId,
    CurrencyId, ForeignAmount, ExchangeRate, TaxRateId, TaxAmount
)
SELECT
    corrected.Id,
    originalLine.AccountId,
    CASE WHEN originalLine.Description LIKE N'%inventory%'
         THEN c.CorrectSubtotal - ISNULL(i.DiscountAmount, 0)
         WHEN originalLine.Description LIKE N'%input%'
         THEN c.CorrectTax ELSE 0 END,
    CASE WHEN originalLine.Description LIKE N'%payment%'
         THEN c.CorrectTotal ELSE 0 END,
    CONCAT(N'Corrected - ', originalLine.Description),
    SYSDATETIME(),
    SYSDATETIME(),
    originalLine.LineNumber,
    originalLine.PartyUserId,
    originalLine.CustomerId,
    originalLine.SupplierId,
    originalLine.CashierId,
    originalLine.WarehouseId,
    originalLine.CostCenterId,
    originalLine.BranchId,
    originalLine.InvoiceId,
    originalLine.VoucherId,
    originalLine.StockDocumentId,
    originalLine.FinancialTransactionId,
    N'Invoice',
    originalLine.ReferenceId,
    originalLine.CurrencyId,
    originalLine.ForeignAmount,
    originalLine.ExchangeRate,
    originalLine.TaxRateId,
    CASE WHEN originalLine.Description LIKE N'%input%' THEN c.CorrectTax ELSE NULL END
FROM dbo.JournalEntries original
INNER JOIN dbo.JournalEntryLines originalLine ON originalLine.JournalEntryId = original.Id
INNER JOIN dbo.Invoice i ON i.Id = original.ReferenceId
INNER JOIN @Corrections c ON c.InvoiceId = i.Id
INNER JOIN dbo.JournalEntries corrected
    ON corrected.ReferenceType = N'Invoice'
   AND corrected.ReferenceId = i.Id
   AND corrected.EntryNumber LIKE CONCAT(N'REPAIR-CORR-', i.Id, N'-%')
WHERE original.ReferenceType = N'Invoice'
  AND original.Status = 2;

COMMIT TRANSACTION;

SELECT N'Repair committed. Run PurchaseInvoiceReconciliationAudit.sql again.' AS Result;
