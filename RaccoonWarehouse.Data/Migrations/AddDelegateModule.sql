IF OBJECT_ID(N'dbo.[Delegate]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Delegate]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [PhoneNumber] NVARCHAR(50) NULL,
        [AlternatePhoneNumber] NVARCHAR(50) NULL,
        [Status] INT NOT NULL DEFAULT(1),
        [DelegateType] INT NOT NULL DEFAULT(5),
        [RegionId] INT NULL,
        [AreaName] NVARCHAR(200) NULL,
        [HireDate] DATETIME2 NULL,
        [Notes] NVARCHAR(1000) NULL,
        [CreatedBy] INT NULL,
        [ModifiedBy] INT NULL,
        [IsDeleted] BIT NOT NULL DEFAULT(0),
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF COL_LENGTH('Delegate', 'AreaName') IS NULL
BEGIN
    ALTER TABLE [dbo].[Delegate] ADD [AreaName] NVARCHAR(200) NULL;
END;

IF COL_LENGTH('Invoice', 'DelegateId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Invoice] ADD [DelegateId] INT NULL;
END;

IF OBJECT_ID(N'dbo.[AppSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppSettings]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key] NVARCHAR(150) NOT NULL,
        [Value] NVARCHAR(MAX) NULL,
        [Description] NVARCHAR(500) NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'EnableDelegateSystem'
)
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description], [CreatedDate], [UpdatedDate])
    VALUES (N'EnableDelegateSystem', N'False', N'Enable or disable the delegate business module.', SYSDATETIME(), SYSDATETIME());
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes WHERE name = N'IX_Delegate_Code' AND object_id = OBJECT_ID(N'dbo.[Delegate]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Delegate_Code] ON [dbo].[Delegate]([Code]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes WHERE name = N'IX_Delegate_UserId' AND object_id = OBJECT_ID(N'dbo.[Delegate]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Delegate_UserId] ON [dbo].[Delegate]([UserId]) WHERE [UserId] IS NOT NULL;
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoice_DelegateId' AND object_id = OBJECT_ID(N'dbo.[Invoice]')
)
BEGIN
    CREATE INDEX [IX_Invoice_DelegateId] ON [dbo].[Invoice]([DelegateId]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes WHERE name = N'IX_AppSettings_Key' AND object_id = OBJECT_ID(N'dbo.[AppSettings]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_AppSettings_Key] ON [dbo].[AppSettings]([Key]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Delegate_User_UserId'
)
BEGIN
    ALTER TABLE [dbo].[Delegate]
        ADD CONSTRAINT [FK_Delegate_User_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Invoice_Delegate_DelegateId'
)
BEGIN
    ALTER TABLE [dbo].[Invoice]
        ADD CONSTRAINT [FK_Invoice_Delegate_DelegateId]
        FOREIGN KEY ([DelegateId]) REFERENCES [dbo].[Delegate]([Id]);
END;
