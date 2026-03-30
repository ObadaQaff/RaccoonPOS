IF OBJECT_ID(N'dbo.[AppSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppSettings]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key] NVARCHAR(200) NOT NULL,
        [Value] NVARCHAR(MAX) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AppSettings_Key'
      AND object_id = OBJECT_ID(N'dbo.[AppSettings]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_AppSettings_Key] ON [dbo].[AppSettings]([Key]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'EnableAccountingSystem'
)
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description], [CreatedDate], [UpdatedDate])
    VALUES (N'EnableAccountingSystem', N'False', N'Enable or disable the accounting business module.', SYSDATETIME(), SYSDATETIME());
END;
