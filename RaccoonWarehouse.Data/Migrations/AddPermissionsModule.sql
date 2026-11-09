/*
    MANUAL-ONLY SQL SCRIPT: Lightweight RBAC Permissions
    Provider: SQL Server
    Additive only. This script is not executed by the application.
    Existing Users and authentication data are preserved.
*/
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.PermissionGroups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PermissionGroups
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PermissionGroups PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_PermissionGroups_IsActive DEFAULT(1),
        IsProtectedAdministrator BIT NOT NULL CONSTRAINT DF_PermissionGroups_IsProtectedAdministrator DEFAULT(0),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_PermissionGroups_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_PermissionGroups_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_PermissionGroups_Name UNIQUE(Name)
    );
END;

IF OBJECT_ID(N'dbo.PermissionGroupPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PermissionGroupPermissions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PermissionGroupPermissions PRIMARY KEY,
        PermissionGroupId INT NOT NULL,
        AreaKey NVARCHAR(100) NOT NULL,
        [Level] INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_PermissionGroupPermissions_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_PermissionGroupPermissions_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_PermissionGroupPermissions_Groups FOREIGN KEY(PermissionGroupId)
            REFERENCES dbo.PermissionGroups(Id) ON DELETE CASCADE,
        CONSTRAINT CK_PermissionGroupPermissions_Level CHECK([Level] BETWEEN 1 AND 3),
        CONSTRAINT UQ_PermissionGroupPermissions_Group_Area UNIQUE(PermissionGroupId, AreaKey)
    );
END;

IF OBJECT_ID(N'dbo.UserPermissionGroups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPermissionGroups
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserPermissionGroups PRIMARY KEY,
        UserId INT NOT NULL,
        PermissionGroupId INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_UserPermissionGroups_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_UserPermissionGroups_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_UserPermissionGroups_Users FOREIGN KEY(UserId)
            REFERENCES dbo.[User](Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserPermissionGroups_Groups FOREIGN KEY(PermissionGroupId)
            REFERENCES dbo.PermissionGroups(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_UserPermissionGroups_User_Group UNIQUE(UserId, PermissionGroupId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_PermissionGroupPermissions_Area' AND object_id=OBJECT_ID(N'dbo.PermissionGroupPermissions'))
    CREATE INDEX IX_PermissionGroupPermissions_Area ON dbo.PermissionGroupPermissions(AreaKey, [Level]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_UserPermissionGroups_User' AND object_id=OBJECT_ID(N'dbo.UserPermissionGroups'))
    CREATE INDEX IX_UserPermissionGroups_User ON dbo.UserPermissionGroups(UserId, PermissionGroupId);

COMMIT TRANSACTION;

/*
    OPTIONAL ADMINISTRATOR BOOTSTRAP
    Execute separately after reviewing the selected existing UserId.
    Replace @TrustedUserId with an existing trusted administrator account.
    Do not use a username or password here.
*/
DECLARE @TrustedUserId INT = NULL; -- Set manually, for example: 12

IF @TrustedUserId IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.[User] WHERE Id = @TrustedUserId)
BEGIN
    DECLARE @AdminGroupId INT;

    SELECT @AdminGroupId = Id
    FROM dbo.PermissionGroups
    WHERE Name = N'System Administrators';

    IF @AdminGroupId IS NULL
    BEGIN
        INSERT INTO dbo.PermissionGroups(Name, Description, IsActive, IsProtectedAdministrator, CreatedDate, UpdatedDate)
        VALUES(N'System Administrators', N'Explicit bootstrap administrator group.', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
        SET @AdminGroupId = SCOPE_IDENTITY();
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.UserPermissionGroups WHERE UserId=@TrustedUserId AND PermissionGroupId=@AdminGroupId)
        INSERT INTO dbo.UserPermissionGroups(UserId, PermissionGroupId, CreatedDate, UpdatedDate)
        VALUES(@TrustedUserId, @AdminGroupId, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.PermissionGroupPermissions(PermissionGroupId, AreaKey, [Level], CreatedDate, UpdatedDate)
    SELECT @AdminGroupId, v.AreaKey, 3, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM (VALUES
        (N'POS'), (N'Products'), (N'Inventory'), (N'Purchasing'), (N'Customers'),
        (N'Suppliers'), (N'Accounting'), (N'Reports'), (N'Employees'), (N'Shifts'),
        (N'HR'), (N'Settings'), (N'Modules'), (N'Users'), (N'Permissions')
    ) v(AreaKey)
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.PermissionGroupPermissions p
        WHERE p.PermissionGroupId=@AdminGroupId AND p.AreaKey=v.AreaKey
    );
END;
