/*
   READ-ONLY purchase-invoice reconciliation report.

   Purpose:
     Compare stored purchase-invoice totals with the corrected calculation:
       line gross -> percentage bonus adjustment -> tax -> invoice total.

   This script contains no INSERT, UPDATE, DELETE, or transaction statements.
   Run it against a backup or approved reporting connection first.
*/
SET NOCOUNT ON;

WITH RawLines AS
(
    SELECT
        i.Id,
        i.InvoiceNumber,
        i.Status,
        i.CreatedDate,
        i.TotalAmount,
        i.SubTotal,
        i.TotalTax,
        ISNULL(i.DiscountAmount, 0) AS DiscountAmount,
        i.CostPriceIncludesTax,
        l.Quantity,
        l.UnitPrice,
        ISNULL(l.LineDiscountAmount, 0) AS LineDiscountAmount,
        l.TaxRate,
        l.TaxExempt,
        l.BonusPercentage,
        (l.Quantity * l.UnitPrice - ISNULL(l.LineDiscountAmount, 0)) AS GrossAmount,
        CASE
            WHEN l.BonusPercentage IS NOT NULL AND l.Quantity > 0
                THEN (l.Quantity * l.UnitPrice - ISNULL(l.LineDiscountAmount, 0))
                     / (1 + l.BonusPercentage / 100.0)
            ELSE l.Quantity * l.UnitPrice - ISNULL(l.LineDiscountAmount, 0)
        END AS BonusAdjustedAmount
    FROM dbo.Invoice AS i
    INNER JOIN dbo.InvoiceLine AS l ON l.InvoiceId = i.Id
    WHERE i.InvoiceType = 2 -- Purchase
), LineTotals AS
(
    SELECT *,
        ROUND(
            CASE
                WHEN CostPriceIncludesTax = 1 AND TaxExempt = 0 AND TaxRate > 0
                    THEN BonusAdjustedAmount / (1 + TaxRate / 100.0)
                ELSE BonusAdjustedAmount
            END,
            3) AS CorrectLineSubtotal
    FROM RawLines
), InvoiceTotals AS
(
    SELECT
        Id,
        InvoiceNumber,
        Status,
        CreatedDate,
        TotalAmount,
        SubTotal,
        TotalTax,
        DiscountAmount,
        CostPriceIncludesTax,
        SUM(GrossAmount) AS OldGrossAmount,
        SUM(BonusAdjustedAmount) AS BonusAdjustedGrossAmount,
        SUM(CorrectLineSubtotal) AS CorrectSubtotalBeforeDiscount,
        SUM(CASE
                WHEN TaxExempt = 1 OR TaxRate <= 0 THEN 0
                WHEN CostPriceIncludesTax = 1
                    THEN ROUND(BonusAdjustedAmount - CorrectLineSubtotal, 3)
                ELSE ROUND(CorrectLineSubtotal * TaxRate / 100.0, 3)
            END) AS TaxBeforeInvoiceDiscount,
        SUM(CASE WHEN BonusPercentage IS NOT NULL THEN 1 ELSE 0 END) AS BonusLineCount
    FROM LineTotals
    GROUP BY
        Id, InvoiceNumber, Status, CreatedDate, TotalAmount, SubTotal,
        TotalTax, DiscountAmount, CostPriceIncludesTax
), CorrectTotals AS
(
    SELECT *,
        ROUND(
            CASE
                WHEN DiscountAmount > CorrectSubtotalBeforeDiscount
                    THEN CorrectSubtotalBeforeDiscount
                ELSE DiscountAmount
            END,
            3) AS CorrectDiscount
    FROM InvoiceTotals
), FinalTotals AS
(
    SELECT *,
        CASE
            WHEN CorrectSubtotalBeforeDiscount > 0
                THEN (CorrectSubtotalBeforeDiscount - CorrectDiscount)
                     / CorrectSubtotalBeforeDiscount
            ELSE 0
        END AS TaxRatio
    FROM CorrectTotals
), Calculated AS
(
    SELECT *,
        ROUND(TaxBeforeInvoiceDiscount * TaxRatio, 3) AS CorrectTax
    FROM FinalTotals
)
SELECT
    c.Id,
    c.InvoiceNumber,
    c.Status,
    c.CreatedDate,
    c.BonusLineCount,
    c.TotalAmount AS StoredTotal,
    ROUND(c.CorrectSubtotalBeforeDiscount - c.CorrectDiscount + c.CorrectTax, 3) AS CorrectTotal,
    ROUND(c.TotalAmount - (c.CorrectSubtotalBeforeDiscount - c.CorrectDiscount + c.CorrectTax), 3) AS TotalDifference,
    c.SubTotal AS StoredSubtotal,
    ROUND(c.CorrectSubtotalBeforeDiscount, 3) AS CorrectSubtotal,
    c.TotalTax AS StoredTax,
    c.CorrectTax,
    p.PaymentTotal,
    j.JournalId,
    j.JournalDebit,
    j.JournalCredit,
    ft.FinancialAmount
FROM Calculated AS c
OUTER APPLY
(
    SELECT SUM(Amount) AS PaymentTotal
    FROM dbo.InvoicePayments
    WHERE InvoiceId = c.Id
) AS p
OUTER APPLY
(
    SELECT
        MAX(j.Id) AS JournalId,
        SUM(jl.Debit) AS JournalDebit,
        SUM(jl.Credit) AS JournalCredit
    FROM dbo.JournalEntries AS j
    INNER JOIN dbo.JournalEntryLines AS jl ON jl.JournalEntryId = j.Id
    WHERE j.ReferenceType = N'Invoice'
      AND j.ReferenceId = c.Id
      AND j.Status = 2 -- Posted
) AS j
OUTER APPLY
(
    SELECT SUM(Amount) AS FinancialAmount
    FROM dbo.FinancialTransaction
    WHERE SourceType = 4 -- PurchaseInvoice
      AND SourceId = c.Id
) AS ft
WHERE
       ABS(c.TotalAmount - (c.CorrectSubtotalBeforeDiscount - c.CorrectDiscount + c.CorrectTax)) > 0.005
    OR ABS(ISNULL(p.PaymentTotal, 0) - (c.CorrectSubtotalBeforeDiscount - c.CorrectDiscount + c.CorrectTax)) > 0.005
    OR ABS(ISNULL(j.JournalCredit, 0) - (c.CorrectSubtotalBeforeDiscount - c.CorrectDiscount + c.CorrectTax)) > 0.005
ORDER BY c.Id;
