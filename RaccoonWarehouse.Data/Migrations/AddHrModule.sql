/*
    MANUAL-ONLY SQL MIGRATION: HR Module
    Provider: SQL Server
    Additive only. Do not execute automatically.
    No accounting, journal, invoice, POS, user, employee, or shift data is deleted.
    Contribution rates are intentionally not seeded; configure legally applicable
    rules through the HR configuration workflow before generating payroll.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.AttendanceRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AttendanceRecords
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AttendanceRecords PRIMARY KEY,
        EmployeeId INT NOT NULL,
        ShiftAssignmentId INT NULL,
        WorkDate DATETIME2 NOT NULL,
        CheckIn DATETIME2 NULL,
        CheckOut DATETIME2 NULL,
        WorkedHours DECIMAL(18,6) NOT NULL CONSTRAINT DF_AttendanceRecords_WorkedHours DEFAULT(0),
        LateMinutes INT NOT NULL CONSTRAINT DF_AttendanceRecords_LateMinutes DEFAULT(0),
        EarlyLeaveMinutes INT NOT NULL CONSTRAINT DF_AttendanceRecords_EarlyLeaveMinutes DEFAULT(0),
        Status INT NOT NULL CONSTRAINT DF_AttendanceRecords_Status DEFAULT(1),
        CorrectionReason NVARCHAR(1000) NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AttendanceRecords_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_AttendanceRecords_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_AttendanceRecords_Employees FOREIGN KEY(EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AttendanceRecords_ShiftAssignments FOREIGN KEY(ShiftAssignmentId) REFERENCES dbo.EmployeeShiftAssignments(Id) ON DELETE NO ACTION,
        CONSTRAINT UQ_AttendanceRecords_Employee_Date UNIQUE(EmployeeId, WorkDate)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AttendanceRecords_Date' AND object_id=OBJECT_ID(N'dbo.AttendanceRecords'))
    CREATE INDEX IX_AttendanceRecords_Date ON dbo.AttendanceRecords(WorkDate, EmployeeId);

IF OBJECT_ID(N'dbo.LeaveTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveTypes
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LeaveTypes PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        IsPaid BIT NOT NULL CONSTRAINT DF_LeaveTypes_IsPaid DEFAULT(1),
        DefaultAnnualBalance DECIMAL(18,6) NOT NULL CONSTRAINT DF_LeaveTypes_DefaultBalance DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_LeaveTypes_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_LeaveTypes_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_LeaveTypes_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_LeaveTypes_Name UNIQUE(Name)
    );
END;

IF OBJECT_ID(N'dbo.LeaveRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveRequests
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LeaveRequests PRIMARY KEY,
        EmployeeId INT NOT NULL,
        LeaveTypeId INT NOT NULL,
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NOT NULL,
        Days DECIMAL(18,6) NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_LeaveRequests_Status DEFAULT(2),
        Reason NVARCHAR(1000) NULL,
        DecisionReason NVARCHAR(1000) NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_LeaveRequests_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_LeaveRequests_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_LeaveRequests_Employees FOREIGN KEY(EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LeaveRequests_LeaveTypes FOREIGN KEY(LeaveTypeId) REFERENCES dbo.LeaveTypes(Id)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_LeaveRequests_Search' AND object_id=OBJECT_ID(N'dbo.LeaveRequests'))
    CREATE INDEX IX_LeaveRequests_Search ON dbo.LeaveRequests(EmployeeId, StartDate, EndDate, Status);

IF OBJECT_ID(N'dbo.LeaveBalances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveBalances
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LeaveBalances PRIMARY KEY,
        EmployeeId INT NOT NULL,
        LeaveTypeId INT NOT NULL,
        [Year] INT NOT NULL,
        EntitledDays DECIMAL(18,6) NOT NULL CONSTRAINT DF_LeaveBalances_EntitledDays DEFAULT(0),
        UsedDays DECIMAL(18,6) NOT NULL CONSTRAINT DF_LeaveBalances_UsedDays DEFAULT(0),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_LeaveBalances_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_LeaveBalances_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_LeaveBalances_Employees FOREIGN KEY(EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LeaveBalances_LeaveTypes FOREIGN KEY(LeaveTypeId) REFERENCES dbo.LeaveTypes(Id),
        CONSTRAINT UQ_LeaveBalances_Employee_Type_Year UNIQUE(EmployeeId, LeaveTypeId, [Year])
    );
END;

IF OBJECT_ID(N'dbo.HrAdjustments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrAdjustments
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrAdjustments PRIMARY KEY,
        EmployeeId INT NOT NULL,
        PeriodMonth DATETIME2 NOT NULL,
        [Type] INT NOT NULL,
        Amount DECIMAL(18,6) NOT NULL CONSTRAINT DF_HrAdjustments_Amount DEFAULT(0),
        Hours DECIMAL(18,6) NOT NULL CONSTRAINT DF_HrAdjustments_Hours DEFAULT(0),
        Approved BIT NOT NULL CONSTRAINT DF_HrAdjustments_Approved DEFAULT(0),
        Reason NVARCHAR(1000) NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_HrAdjustments_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_HrAdjustments_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_HrAdjustments_Employees FOREIGN KEY(EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_HrAdjustments_Period' AND object_id=OBJECT_ID(N'dbo.HrAdjustments'))
    CREATE INDEX IX_HrAdjustments_Period ON dbo.HrAdjustments(EmployeeId, PeriodMonth, [Type], Approved);

IF OBJECT_ID(N'dbo.SocialSecurityRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SocialSecurityRules
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SocialSecurityRules PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        EffectiveFrom DATETIME2 NOT NULL,
        EffectiveTo DATETIME2 NULL,
        EmployeeRate DECIMAL(18,6) NOT NULL,
        EmployerRate DECIMAL(18,6) NOT NULL,
        MinimumBase DECIMAL(18,6) NULL,
        MaximumBase DECIMAL(18,6) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SocialSecurityRules_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_SocialSecurityRules_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_SocialSecurityRules_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT CK_SocialSecurityRules_Rates CHECK(EmployeeRate >= 0 AND EmployerRate >= 0),
        CONSTRAINT CK_SocialSecurityRules_Dates CHECK(EffectiveTo IS NULL OR EffectiveTo >= EffectiveFrom)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_SocialSecurityRules_Effective' AND object_id=OBJECT_ID(N'dbo.SocialSecurityRules'))
    CREATE INDEX IX_SocialSecurityRules_Effective ON dbo.SocialSecurityRules(EffectiveFrom, EffectiveTo, IsActive);

IF OBJECT_ID(N'dbo.PayrollPeriods', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayrollPeriods
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PayrollPeriods PRIMARY KEY,
        PeriodMonth DATETIME2 NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_PayrollPeriods_Status DEFAULT(1),
        ApprovedAt DATETIME2 NULL,
        PaidAt DATETIME2 NULL,
        ApprovalNote NVARCHAR(1000) NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_PayrollPeriods_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_PayrollPeriods_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_PayrollPeriods_Month UNIQUE(PeriodMonth)
    );
END;

IF OBJECT_ID(N'dbo.EmployeePayrolls', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeePayrolls
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeePayrolls PRIMARY KEY,
        PayrollPeriodId INT NOT NULL,
        EmployeeId INT NOT NULL,
        EmployeeNameSnapshot NVARCHAR(200) NOT NULL,
        BasicSalary DECIMAL(18,6) NOT NULL,
        Allowances DECIMAL(18,6) NOT NULL,
        Bonuses DECIMAL(18,6) NOT NULL,
        Overtime DECIMAL(18,6) NOT NULL,
        UnpaidAbsence DECIMAL(18,6) NOT NULL,
        Deductions DECIMAL(18,6) NOT NULL,
        EmployeeSocialSecurity DECIMAL(18,6) NOT NULL,
        EmployerSocialSecurity DECIMAL(18,6) NOT NULL,
        GrossAmount DECIMAL(18,6) NOT NULL,
        NetAmount DECIMAL(18,6) NOT NULL,
        CalculationSnapshotJson NVARCHAR(MAX) NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_EmployeePayrolls_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_EmployeePayrolls_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_EmployeePayrolls_Periods FOREIGN KEY(PayrollPeriodId) REFERENCES dbo.PayrollPeriods(Id) ON DELETE CASCADE,
        CONSTRAINT FK_EmployeePayrolls_Employees FOREIGN KEY(EmployeeId) REFERENCES dbo.Employees(Id),
        CONSTRAINT UQ_EmployeePayrolls_Period_Employee UNIQUE(PayrollPeriodId, EmployeeId)
    );
END;

IF OBJECT_ID(N'dbo.EmployeePayrollLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeePayrollLines
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeePayrollLines PRIMARY KEY,
        EmployeePayrollId INT NOT NULL,
        Description NVARCHAR(300) NOT NULL,
        Amount DECIMAL(18,6) NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_EmployeePayrollLines_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_EmployeePayrollLines_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_EmployeePayrollLines_Payrolls FOREIGN KEY(EmployeePayrollId) REFERENCES dbo.EmployeePayrolls(Id) ON DELETE CASCADE
    );
END;

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

IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N'EnableHrModule')
BEGIN
    INSERT INTO dbo.AppSettings([Key], [Value], [Description], CreatedDate, UpdatedDate)
    VALUES(N'EnableHrModule', N'false', N'Enable or disable the HR module.', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
