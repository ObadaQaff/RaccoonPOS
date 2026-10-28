IF OBJECT_ID(N'dbo.LostCustomers', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LostCustomers]
    (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LostCustomers] PRIMARY KEY,
        [Type] int NOT NULL,
        [Reason] int NOT NULL,
        [ProductId] int NULL,
        [ProductUnitId] int NULL,
        [ProductName] nvarchar(300) NOT NULL,
        [Quantity] decimal(18,6) NULL,
        [Message] nvarchar(2000) NULL,
        [Notes] nvarchar(2000) NULL,
        [CreatedBy] int NULL,
        [CreatedByName] nvarchar(200) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL
    );
    CREATE INDEX [IX_LostCustomers_CreatedDate] ON [dbo].[LostCustomers] ([CreatedDate]);
    CREATE INDEX [IX_LostCustomers_ProductId] ON [dbo].[LostCustomers] ([ProductId]);
END;
GO
