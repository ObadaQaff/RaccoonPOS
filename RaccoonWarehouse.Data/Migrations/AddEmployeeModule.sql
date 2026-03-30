IF OBJECT_ID(N'dbo.Employee', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Employee]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [PhoneNumber] NVARCHAR(50) NULL,
        [AlternatePhoneNumber] NVARCHAR(50) NULL,
        [Email] NVARCHAR(200) NULL,
        [NationalId] NVARCHAR(100) NULL,
        [HireDate] DATETIME2 NULL,
        [TerminationDate] DATETIME2 NULL,
        [Status] INT NOT NULL DEFAULT(1),
        [Gender] INT NULL,
        [JobTitle] NVARCHAR(150) NULL,
        [DepartmentId] INT NULL,
        [BranchId] INT NULL,
        [ManagerId] INT NULL,
        [BasicSalary] DECIMAL(18,2) NULL,
        [Notes] NVARCHAR(1000) NULL,
        [Address] NVARCHAR(500) NULL,
        [DateOfBirth] DATETIME2 NULL,
        [CreatedBy] INT NULL,
        [ModifiedBy] INT NULL,
        [IsDeleted] BIT NOT NULL DEFAULT(0),
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Employee_Code'
      AND object_id = OBJECT_ID(N'dbo.Employee')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employee_Code] ON [dbo].[Employee]([Code]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Employee_UserId'
      AND object_id = OBJECT_ID(N'dbo.Employee')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employee_UserId]
        ON [dbo].[Employee]([UserId])
        WHERE [UserId] IS NOT NULL;
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Employee_BranchId'
      AND object_id = OBJECT_ID(N'dbo.Employee')
)
BEGIN
    CREATE INDEX [IX_Employee_BranchId] ON [dbo].[Employee]([BranchId]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Employee_DepartmentId'
      AND object_id = OBJECT_ID(N'dbo.Employee')
)
BEGIN
    CREATE INDEX [IX_Employee_DepartmentId] ON [dbo].[Employee]([DepartmentId]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Employee_Status'
      AND object_id = OBJECT_ID(N'dbo.Employee')
)
BEGIN
    CREATE INDEX [IX_Employee_Status] ON [dbo].[Employee]([Status]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Employee_User_UserId'
)
BEGIN
    ALTER TABLE [dbo].[Employee]
        ADD CONSTRAINT [FK_Employee_User_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Employee_Employee_ManagerId'
)
BEGIN
    ALTER TABLE [dbo].[Employee]
        ADD CONSTRAINT [FK_Employee_Employee_ManagerId]
        FOREIGN KEY ([ManagerId]) REFERENCES [dbo].[Employee]([Id]);
END;
