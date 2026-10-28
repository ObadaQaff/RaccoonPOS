SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.CustomerRequests', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CustomerRequests] (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CustomerRequests] PRIMARY KEY,
        [RequestNumber] nvarchar(32) NOT NULL,
        [CustomerId] int NOT NULL,
        [CustomerName] nvarchar(200) NOT NULL,
        [CustomerPhone] nvarchar(50) NULL,
        [Status] int NOT NULL,
        [ReceivingStatus] int NOT NULL,
        [SalesStatus] int NOT NULL,
        [TotalAmount] decimal(18,6) NOT NULL,
        [PaidAmount] decimal(18,6) NOT NULL,
        [PaymentNote] nvarchar(1000) NULL,
        [Notes] nvarchar(2000) NULL,
        [ApprovedBy] int NULL,
        [ApprovedDate] datetime2 NULL,
        [CancelledBy] int NULL,
        [CancelledDate] datetime2 NULL,
        [PurchaseRequestId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX [IX_CustomerRequests_RequestNumber] ON [dbo].[CustomerRequests]([RequestNumber]);
END;
IF OBJECT_ID(N'dbo.CustomerRequestLines', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CustomerRequestLines] (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CustomerRequestLines] PRIMARY KEY,
        [CustomerRequestId] int NOT NULL,
        [ProductId] int NULL,
        [ProductUnitId] int NULL,
        [ItemName] nvarchar(500) NOT NULL,
        [UnitName] nvarchar(100) NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [UnitPrice] decimal(18,6) NOT NULL,
        [ReceivedQuantity] decimal(18,6) NOT NULL,
        [SoldQuantity] decimal(18,6) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [FK_CustomerRequestLines_CustomerRequests_CustomerRequestId] FOREIGN KEY ([CustomerRequestId]) REFERENCES [dbo].[CustomerRequests]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_CustomerRequestLines_CustomerRequestId] ON [dbo].[CustomerRequestLines]([CustomerRequestId]);
END;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261028090000_AddCustomerRequests')
    INSERT INTO dbo.__EFMigrationsHistory(MigrationId, ProductVersion) VALUES (N'20261028090000_AddCustomerRequests', N'9.0.6');
COMMIT TRANSACTION;
