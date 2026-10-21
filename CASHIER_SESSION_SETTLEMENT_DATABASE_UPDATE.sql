/*
    Cashier session settlement fields
    Apply manually to the application database after taking a backup.
    This script is SQL Server compatible and safe to run more than once.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.CashierSession', N'U') IS NULL
    THROW 51000, 'dbo.CashierSession does not exist. Stop and verify the database.', 1;

IF COL_LENGTH(N'dbo.CashierSession', N'ExpectedVisa') IS NULL
    ALTER TABLE dbo.CashierSession ADD ExpectedVisa decimal(18, 2) NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'CountedVisa') IS NULL
    ALTER TABLE dbo.CashierSession ADD CountedVisa decimal(18, 2) NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'VisaDifference') IS NULL
    ALTER TABLE dbo.CashierSession ADD VisaDifference decimal(18, 2) NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'ClosingNotes') IS NULL
    ALTER TABLE dbo.CashierSession ADD ClosingNotes nvarchar(2000) NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'ClosedBy') IS NULL
    ALTER TABLE dbo.CashierSession ADD ClosedBy int NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'SettlementCalculatedAt') IS NULL
    ALTER TABLE dbo.CashierSession ADD SettlementCalculatedAt datetime2 NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'SettlementCountedCash') IS NULL
    ALTER TABLE dbo.CashierSession ADD SettlementCountedCash decimal(18, 2) NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'SettlementCountedVisa') IS NULL
    ALTER TABLE dbo.CashierSession ADD SettlementCountedVisa decimal(18, 2) NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'SettlementExpectedCash') IS NULL
    ALTER TABLE dbo.CashierSession ADD SettlementExpectedCash decimal(18, 2) NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'SettlementExpectedVisa') IS NULL
    ALTER TABLE dbo.CashierSession ADD SettlementExpectedVisa decimal(18, 2) NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'SettlementCashDifference') IS NULL
    ALTER TABLE dbo.CashierSession ADD SettlementCashDifference decimal(18, 2) NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'SettlementVisaDifference') IS NULL
    ALTER TABLE dbo.CashierSession ADD SettlementVisaDifference decimal(18, 2) NULL;

IF COL_LENGTH(N'dbo.CashierSession', N'SettlementLocked') IS NULL
    ALTER TABLE dbo.CashierSession ADD SettlementLocked bit NOT NULL CONSTRAINT DF_CashierSession_SettlementLocked DEFAULT (0);

IF OBJECT_ID(N'dbo.PurchaseReturns', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PurchaseReturns', N'CashierSessionId') IS NULL
    ALTER TABLE dbo.PurchaseReturns ADD CashierSessionId int NULL;

IF OBJECT_ID(N'dbo.PurchaseReturns', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PurchaseReturns', N'CashierSessionId') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = N'FK_PurchaseReturns_CashierSession'
         AND parent_object_id = OBJECT_ID(N'dbo.PurchaseReturns')
   )
    ALTER TABLE dbo.PurchaseReturns
        ADD CONSTRAINT FK_PurchaseReturns_CashierSession
        FOREIGN KEY (CashierSessionId) REFERENCES dbo.CashierSession(Id);

IF OBJECT_ID(N'dbo.PurchaseReturns', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PurchaseReturns', N'CashierSessionId') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1 FROM sys.indexes
       WHERE name = N'IX_PurchaseReturns_CashierSessionId'
         AND object_id = OBJECT_ID(N'dbo.PurchaseReturns')
   )
    CREATE INDEX IX_PurchaseReturns_CashierSessionId
        ON dbo.PurchaseReturns(CashierSessionId);

IF OBJECT_ID(N'dbo.Voucher', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_Voucher_CashierSessionId_PostingStatus'
         AND object_id = OBJECT_ID(N'dbo.Voucher')
   )
BEGIN
    CREATE INDEX IX_Voucher_CashierSessionId_PostingStatus
        ON dbo.Voucher(CashierSessionId, PostingStatus)
        INCLUDE (VoucherType, PaymentType, Amount, CreatedDate);
END;

COMMIT TRANSACTION;

/* Validation */
SELECT name, system_type_name = TYPE_NAME(system_type_id), max_length, is_nullable
FROM sys.columns
WHERE object_id = OBJECT_ID(N'dbo.CashierSession')
  AND name IN (N'ExpectedClosingBalance', N'DifferenceAmount', N'ExpectedVisa', N'CountedVisa', N'VisaDifference', N'ClosingNotes', N'ClosedBy', N'SettlementCalculatedAt', N'SettlementCountedCash', N'SettlementCountedVisa', N'SettlementExpectedCash', N'SettlementExpectedVisa', N'SettlementCashDifference', N'SettlementVisaDifference', N'SettlementLocked');

IF OBJECT_ID(N'dbo.PurchaseReturns', N'U') IS NOT NULL
    SELECT name, system_type_name = TYPE_NAME(system_type_id), max_length, is_nullable
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PurchaseReturns')
      AND name = N'CashierSessionId';

IF OBJECT_ID(N'dbo.Voucher', N'U') IS NOT NULL
    SELECT name, system_type_name = TYPE_NAME(system_type_id), max_length, is_nullable
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Voucher')
      AND name IN
      (
          N'CashierSessionId',
          N'CasherId',
          N'VoucherType',
          N'PaymentType',
          N'PostingStatus',
          N'Amount'
      );

/*
    Notes:
    - Existing EndingBalance remains the counted cash amount.
    - Existing ExpectedClosingBalance remains the expected cash amount.
    - Existing DifferenceAmount remains the cash difference.
    - Visa values use the new columns above.
    - Settlement* values preserve a calculated open-session snapshot. SettlementLocked prevents re-entry.
    - SessionStatus.Closing is stored as enum value 3 and blocks new POS sales until final closure.
    - No existing rows are updated by this script.
*/
