SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.UserNumberSequence', N'SO') IS NULL
    CREATE SEQUENCE [dbo].[UserNumberSequence] AS int START WITH 1 INCREMENT BY 1;

IF COL_LENGTH(N'dbo.Users', N'UserNumber') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [UserNumber] int NOT NULL
            CONSTRAINT [DF_Users_UserNumber]
            DEFAULT (NEXT VALUE FOR dbo.UserNumberSequence) WITH VALUES;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Users_UserNumber'
      AND object_id = OBJECT_ID(N'dbo.Users')
)
    CREATE UNIQUE INDEX [IX_Users_UserNumber] ON [dbo].[Users]([UserNumber]);

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20261028150000_AddUserNumber'
)
    INSERT INTO dbo.__EFMigrationsHistory(MigrationId, ProductVersion)
    VALUES (N'20261028150000_AddUserNumber', N'9.0.6');

COMMIT TRANSACTION;
