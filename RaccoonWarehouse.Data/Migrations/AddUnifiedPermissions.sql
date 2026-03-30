IF OBJECT_ID(N'dbo.PermissionDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PermissionDefinitions]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key] NVARCHAR(200) NOT NULL,
        [Module] NVARCHAR(100) NOT NULL,
        [Resource] NVARCHAR(100) NOT NULL,
        [Action] NVARCHAR(100) NOT NULL,
        [DisplayName] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [LegacyReportKey] NVARCHAR(150) NULL,
        [SortOrder] INT NOT NULL,
        [IsActive] BIT NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RolePermissions]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Role] INT NOT NULL,
        [PermissionKey] NVARCHAR(200) NOT NULL,
        [IsAllowed] BIT NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [UpdatedDate] DATETIME2 NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PermissionDefinitions_Key'
      AND object_id = OBJECT_ID(N'dbo.PermissionDefinitions')
)
BEGIN
    CREATE UNIQUE INDEX [IX_PermissionDefinitions_Key]
        ON [dbo].[PermissionDefinitions] ([Key]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_RolePermissions_Role_PermissionKey'
      AND object_id = OBJECT_ID(N'dbo.RolePermissions')
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolePermissions_Role_PermissionKey]
        ON [dbo].[RolePermissions] ([Role], [PermissionKey]);
END;
