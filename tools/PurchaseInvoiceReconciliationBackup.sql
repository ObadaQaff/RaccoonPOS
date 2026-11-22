/*
  Scoped backup for the confirmed purchase-invoice reconciliation set.

  Covered invoices:
    45386 (Id 1187)
    8214  (Id 1188)
    15    (Id 1191)
    17    (Id 1192)

  Backs up only invoice, payment, accounting, financial-transaction, and stock
  rows related to these invoice IDs. It does not back up or modify products,
  users, settings, or unrelated invoices.

  Run manually after taking a normal full database backup.
  This script creates backup tables and contains no UPDATE or DELETE statements.
  It intentionally fails if these backup tables already exist.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.RepairBackup_20261116_Invoice', N'U') IS NOT NULL
    THROW 51000, 'Repair backup already exists: Invoice', 1;
IF OBJECT_ID(N'dbo.RepairBackup_20261116_InvoiceLine', N'U') IS NOT NULL
    THROW 51000, 'Repair backup already exists: InvoiceLine', 1;
IF OBJECT_ID(N'dbo.RepairBackup_20261116_InvoicePayments', N'U') IS NOT NULL
    THROW 51000, 'Repair backup already exists: InvoicePayments', 1;
IF OBJECT_ID(N'dbo.RepairBackup_20261116_JournalEntries', N'U') IS NOT NULL
    THROW 51000, 'Repair backup already exists: JournalEntries', 1;
IF OBJECT_ID(N'dbo.RepairBackup_20261116_JournalEntryLines', N'U') IS NOT NULL
    THROW 51000, 'Repair backup already exists: JournalEntryLines', 1;
IF OBJECT_ID(N'dbo.RepairBackup_20261116_FinancialTransaction', N'U') IS NOT NULL
    THROW 51000, 'Repair backup already exists: FinancialTransaction', 1;
IF OBJECT_ID(N'dbo.RepairBackup_20261116_StockLot', N'U') IS NOT NULL
    THROW 51000, 'Repair backup already exists: StockLot', 1;
IF OBJECT_ID(N'dbo.RepairBackup_20261116_StockTransaction', N'U') IS NOT NULL
    THROW 51000, 'Repair backup already exists: StockTransaction', 1;

DECLARE @InvoiceIds TABLE (Id int PRIMARY KEY);
DECLARE @InvoiceLineIds TABLE (Id int PRIMARY KEY);
DECLARE @JournalEntryIds TABLE (Id int PRIMARY KEY);
DECLARE @StockLotIds TABLE (Id int PRIMARY KEY);

INSERT INTO @InvoiceIds (Id)
VALUES (1187), (1188), (1191), (1192);

IF EXISTS
(
    SELECT 1
    FROM @InvoiceIds requested
    LEFT JOIN dbo.Invoice invoice ON invoice.Id = requested.Id
    WHERE invoice.Id IS NULL OR invoice.InvoiceType <> 2
)
    THROW 51000, 'One or more requested rows are missing or are not purchase invoices.', 1;

INSERT INTO @InvoiceLineIds (Id)
SELECT line.Id
FROM dbo.InvoiceLine line
WHERE line.InvoiceId IN (SELECT Id FROM @InvoiceIds);

INSERT INTO @JournalEntryIds (Id)
SELECT DISTINCT entry.Id
FROM dbo.JournalEntries entry
WHERE entry.ReferenceType = N'Invoice'
  AND entry.ReferenceId IN (SELECT Id FROM @InvoiceIds);

INSERT INTO @JournalEntryIds (Id)
SELECT DISTINCT reversal.ReferenceId
FROM dbo.JournalEntries reversal
WHERE reversal.ReferenceType = N'Reversal'
  AND reversal.ReferenceId IS NOT NULL
  AND reversal.ReferenceId NOT IN (SELECT Id FROM @JournalEntryIds)
  AND EXISTS
  (
      SELECT 1
      FROM dbo.JournalEntries original
      WHERE original.Id = reversal.ReferenceId
        AND original.ReferenceType = N'Invoice'
        AND original.ReferenceId IN (SELECT Id FROM @InvoiceIds)
  );

INSERT INTO @StockLotIds (Id)
SELECT DISTINCT transactionRow.StockLotId
FROM dbo.StockTransaction transactionRow
WHERE transactionRow.InvoiceId IN (SELECT Id FROM @InvoiceIds)
  AND transactionRow.StockLotId IS NOT NULL;

INSERT INTO @StockLotIds (Id)
SELECT DISTINCT lot.Id
FROM dbo.StockLot lot
WHERE lot.SourcePurchaseLineId IN (SELECT Id FROM @InvoiceLineIds)
  AND lot.Id NOT IN (SELECT Id FROM @StockLotIds);

BEGIN TRANSACTION;

SELECT * INTO dbo.RepairBackup_20261116_Invoice
FROM dbo.Invoice
WHERE Id IN (SELECT Id FROM @InvoiceIds);

SELECT * INTO dbo.RepairBackup_20261116_InvoiceLine
FROM dbo.InvoiceLine
WHERE InvoiceId IN (SELECT Id FROM @InvoiceIds);

SELECT * INTO dbo.RepairBackup_20261116_InvoicePayments
FROM dbo.InvoicePayments
WHERE InvoiceId IN (SELECT Id FROM @InvoiceIds);

SELECT * INTO dbo.RepairBackup_20261116_JournalEntries
FROM dbo.JournalEntries
WHERE Id IN (SELECT Id FROM @JournalEntryIds)
   OR (ReferenceType = N'Reversal' AND ReferenceId IN (SELECT Id FROM @JournalEntryIds));

SELECT * INTO dbo.RepairBackup_20261116_JournalEntryLines
FROM dbo.JournalEntryLines
WHERE JournalEntryId IN
(
    SELECT Id FROM dbo.RepairBackup_20261116_JournalEntries
);

SELECT * INTO dbo.RepairBackup_20261116_FinancialTransaction
FROM dbo.FinancialTransaction
WHERE SourceType = 4
  AND SourceId IN (SELECT Id FROM @InvoiceIds);

SELECT * INTO dbo.RepairBackup_20261116_StockLot
FROM dbo.StockLot
WHERE Id IN (SELECT Id FROM @StockLotIds);

SELECT * INTO dbo.RepairBackup_20261116_StockTransaction
FROM dbo.StockTransaction
WHERE InvoiceId IN (SELECT Id FROM @InvoiceIds)
   OR StockLotId IN (SELECT Id FROM @StockLotIds);

COMMIT TRANSACTION;

SELECT 'Invoice' AS BackupTable, COUNT_BIG(*) AS [BackupRows] FROM dbo.RepairBackup_20261116_Invoice
UNION ALL
SELECT 'InvoiceLine', COUNT_BIG(*) FROM dbo.RepairBackup_20261116_InvoiceLine
UNION ALL
SELECT 'InvoicePayments', COUNT_BIG(*) FROM dbo.RepairBackup_20261116_InvoicePayments
UNION ALL
SELECT 'JournalEntries', COUNT_BIG(*) FROM dbo.RepairBackup_20261116_JournalEntries
UNION ALL
SELECT 'JournalEntryLines', COUNT_BIG(*) FROM dbo.RepairBackup_20261116_JournalEntryLines
UNION ALL
SELECT 'FinancialTransaction', COUNT_BIG(*) FROM dbo.RepairBackup_20261116_FinancialTransaction
UNION ALL
SELECT 'StockLot', COUNT_BIG(*) FROM dbo.RepairBackup_20261116_StockLot
UNION ALL
SELECT 'StockTransaction', COUNT_BIG(*) FROM dbo.RepairBackup_20261116_StockTransaction;
