/*
    MANUAL-ONLY SQL MIGRATION: Employees Management Module
    Provider: SQL Server
    This script is additive and must be reviewed/executed manually.
    It does not alter or delete Users, authentication data, POS cashier sessions,
    accounting records, inventory, invoices, or legacy HR tables.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employees PRIMARY KEY,
        FullName NVARCHAR(200) NOT NULL,
        PhoneNumber NVARCHAR(50) NOT NULL,
        NationalId NVARCHAR(100) NULL,
        Address NVARCHAR(500) NULL,
        Email NVARCHAR(250) NULL,
        EmploymentDate DATETIME2 NULL,
        Status INT NOT NULL CONSTRAINT DF_Employees_Status DEFAULT(1),
        JobTitle NVARCHAR(200) NULL,
        MonthlyBasicSalary DECIMAL(18,6) NOT NULL CONSTRAINT DF_Employees_MonthlyBasicSalary DEFAULT(0),
        SocialSecurityEnrolled BIT NOT NULL CONSTRAINT DF_Employees_SocialSecurityEnrolled DEFAULT(0),
        SocialSecurityNumber NVARCHAR(100) NULL,
        SocialSecurityEnrollmentDate DATETIME2 NULL,
        ContractType INT NOT NULL CONSTRAINT DF_Employees_ContractType DEFAULT(1),
        ContractStartDate DATETIME2 NOT NULL CONSTRAINT DF_Employees_ContractStartDate DEFAULT(SYSUTCDATETIME()),
        ContractEndDate DATETIME2 NULL,
        PrimaryBranchId INT NOT NULL,
        UserId INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Employees_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_Employees_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Employees_Branches_PrimaryBranchId FOREIGN KEY (PrimaryBranchId) REFERENCES dbo.Branches(Id),
        CONSTRAINT FK_Employees_User_UserId FOREIGN KEY (UserId) REFERENCES dbo.[User](Id) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employees_PhoneNumber' AND object_id = OBJECT_ID(N'dbo.Employees'))
    CREATE INDEX IX_Employees_PhoneNumber ON dbo.Employees(PhoneNumber);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employees_FullName' AND object_id = OBJECT_ID(N'dbo.Employees'))
    CREATE INDEX IX_Employees_FullName ON dbo.Employees(FullName);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Employees_NationalId' AND object_id = OBJECT_ID(N'dbo.Employees'))
    CREATE UNIQUE INDEX UX_Employees_NationalId ON dbo.Employees(NationalId) WHERE NationalId IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Employees_UserId' AND object_id = OBJECT_ID(N'dbo.Employees'))
    CREATE UNIQUE INDEX UX_Employees_UserId ON dbo.Employees(UserId) WHERE UserId IS NOT NULL;

IF OBJECT_ID(N'dbo.EmployeeBranchAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeBranchAssignments
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeBranchAssignments PRIMARY KEY,
        EmployeeId INT NOT NULL,
        BranchId INT NOT NULL,
        StartDate DATETIME2 NOT NULL CONSTRAINT DF_EmployeeBranchAssignments_StartDate DEFAULT(SYSUTCDATETIME()),
        EndDate DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_EmployeeBranchAssignments_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_EmployeeBranchAssignments_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_EmployeeBranchAssignments_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_EmployeeBranchAssignments_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
        CONSTRAINT FK_EmployeeBranchAssignments_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmployeeBranchAssignments_Search' AND object_id = OBJECT_ID(N'dbo.EmployeeBranchAssignments'))
    CREATE INDEX IX_EmployeeBranchAssignments_Search ON dbo.EmployeeBranchAssignments(EmployeeId, BranchId, StartDate);

IF OBJECT_ID(N'dbo.ShiftTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ShiftTemplates
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShiftTemplates PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL,
        IsOvernight BIT NOT NULL CONSTRAINT DF_ShiftTemplates_IsOvernight DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_ShiftTemplates_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_ShiftTemplates_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_ShiftTemplates_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_ShiftTemplates_Name UNIQUE(Name)
    );
END;

IF OBJECT_ID(N'dbo.ShiftTemplateWorkingDays', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ShiftTemplateWorkingDays
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShiftTemplateWorkingDays PRIMARY KEY,
        ShiftTemplateId INT NOT NULL,
        DayOfWeek INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_ShiftTemplateWorkingDays_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_ShiftTemplateWorkingDays_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT CK_ShiftTemplateWorkingDays_DayOfWeek CHECK(DayOfWeek BETWEEN 0 AND 6),
        CONSTRAINT FK_ShiftTemplateWorkingDays_ShiftTemplates FOREIGN KEY (ShiftTemplateId) REFERENCES dbo.ShiftTemplates(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_ShiftTemplateWorkingDays_Template_Day UNIQUE(ShiftTemplateId, DayOfWeek)
    );
END;

IF OBJECT_ID(N'dbo.EmployeeShiftAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeShiftAssignments
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeShiftAssignments PRIMARY KEY,
        EmployeeId INT NOT NULL,
        ShiftTemplateId INT NOT NULL,
        DayOfWeek INT NOT NULL,
        StartDate DATETIME2 NOT NULL CONSTRAINT DF_EmployeeShiftAssignments_StartDate DEFAULT(SYSUTCDATETIME()),
        EndDate DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_EmployeeShiftAssignments_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_EmployeeShiftAssignments_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_EmployeeShiftAssignments_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT CK_EmployeeShiftAssignments_DayOfWeek CHECK(DayOfWeek BETWEEN 0 AND 6),
        CONSTRAINT FK_EmployeeShiftAssignments_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
        CONSTRAINT FK_EmployeeShiftAssignments_ShiftTemplates FOREIGN KEY (ShiftTemplateId) REFERENCES dbo.ShiftTemplates(Id)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmployeeShiftAssignments_Search' AND object_id = OBJECT_ID(N'dbo.EmployeeShiftAssignments'))
    CREATE INDEX IX_EmployeeShiftAssignments_Search ON dbo.EmployeeShiftAssignments(EmployeeId, DayOfWeek, StartDate);

IF OBJECT_ID(N'dbo.AppSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppSettings
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppSettings PRIMARY KEY,
        [Key] NVARCHAR(150) NOT NULL,
        [Value] NVARCHAR(MAX) NULL,
        [Description] NVARCHAR(500) NULL,
        CreatedDate DATETIME2 NOT NULL,
        UpdatedDate DATETIME2 NOT NULL
    );
    CREATE UNIQUE INDEX IX_AppSettings_Key ON dbo.AppSettings([Key]);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N'EnableEmployeesModule')
BEGIN
    INSERT INTO dbo.AppSettings([Key], [Value], [Description], CreatedDate, UpdatedDate)
    VALUES(N'EnableEmployeesModule', N'true', N'Enable or disable the Employees module.', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
