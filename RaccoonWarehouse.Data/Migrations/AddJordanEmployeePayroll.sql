BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Employee WHERE DATALENGTH(NationalId)>200)
        THROW 51000, 'Employee NationalId values exceed 100 characters; resolve manually before migration.', 1;
    IF EXISTS (SELECT NationalId FROM dbo.Employee WHERE NationalId IS NOT NULL AND NationalId<>N'' GROUP BY NationalId HAVING COUNT(*)>1)
        THROW 51001, 'Duplicate employee NationalId values must be resolved manually; no records were changed.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employee]') AND [c].[name] = N'NationalId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Employee] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [Employee] ALTER COLUMN [NationalId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    ALTER TABLE [Employee] ADD [EmploymentType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    ALTER TABLE [Employee] ADD [SocialSecurityNumberEncrypted] nvarchar(512) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [EmployeeEmploymentHistory] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [BeforeJson] nvarchar(max) NOT NULL,
        [AfterJson] nvarchar(max) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [ChangedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeeEmploymentHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeEmploymentHistory_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [EmployeeSalaryVersion] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [BasicMonthlySalary] decimal(18,6) NOT NULL,
        [FixedAllowances] decimal(18,6) NOT NULL,
        [StandardDailyHours] decimal(18,6) NOT NULL,
        [WorkingDaysPerWeek] int NOT NULL,
        [StandardMonthlyHours] decimal(18,6) NOT NULL,
        [HourlyRateMethod] int NOT NULL,
        [OvertimeMode] int NOT NULL,
        [NormalOvertimeMultiplier] decimal(18,6) NOT NULL,
        [HolidayOvertimeMultiplier] decimal(18,6) NOT NULL,
        [SocialSecurityCovered] bit NOT NULL,
        [SocialSecurityEligibleWage] decimal(18,6) NULL,
        [ContributionCategory] nvarchar(50) NOT NULL,
        [ChangeReason] nvarchar(1000) NOT NULL,
        [ChangedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeeSalaryVersion] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeSalaryVersion_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [PayrollPolicy] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [EffectiveTo] datetime2 NOT NULL,
        [SalaryDayDivisor] decimal(18,6) NOT NULL,
        [EmployeeSscPercent] decimal(18,6) NOT NULL,
        [EmployerSscPercent] decimal(18,6) NOT NULL,
        [InsurableWageCeiling] decimal(18,6) NOT NULL,
        [DefaultDailyHours] decimal(18,6) NOT NULL,
        [DefaultWorkingDaysPerWeek] int NOT NULL,
        [DefaultMonthlyHours] decimal(18,6) NOT NULL,
        [LegalDailyHours] decimal(18,6) NOT NULL,
        [LegalWeeklyHours] decimal(18,6) NOT NULL,
        [MinimumNormalMultiplier] decimal(18,6) NOT NULL,
        [MinimumHolidayMultiplier] decimal(18,6) NOT NULL,
        [AdvanceDeductionMaximumPercent] decimal(18,6) NOT NULL,
        [WeekStartsOn] int NOT NULL,
        [ProrationMethod] int NOT NULL,
        [IncludeFixedAllowancesInWageBasis] bit NOT NULL,
        [IsLegallyReviewed] bit NOT NULL,
        [SourceReference] nvarchar(2000) NOT NULL,
        [ReviewedBy] int NULL,
        [ReviewReason] nvarchar(1000) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PayrollPolicy] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [EmployeePayroll] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Year] int NOT NULL,
        [Month] int NOT NULL,
        [SalaryVersionId] int NOT NULL,
        [PolicyId] int NOT NULL,
        [State] int NOT NULL,
        [Revision] uniqueidentifier NOT NULL,
        [InputsJson] nvarchar(max) NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [ApprovedAt] datetime2 NULL,
        [ApprovedBy] int NULL,
        [ModifiedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeePayroll] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeePayroll_EmployeeSalaryVersion_SalaryVersionId] FOREIGN KEY ([SalaryVersionId]) REFERENCES [EmployeeSalaryVersion] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeePayroll_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeePayroll_PayrollPolicy_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [PayrollPolicy] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [PayrollAdjustment] (
        [Id] int NOT NULL IDENTITY,
        [EmployeePayrollId] int NOT NULL,
        [Kind] int NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [DeductionBasis] int NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [LegalReference] nvarchar(1000) NULL,
        [Notes] nvarchar(2000) NULL,
        [AuthorizedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PayrollAdjustment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollAdjustment_EmployeePayroll_EmployeePayrollId] FOREIGN KEY ([EmployeePayrollId]) REFERENCES [EmployeePayroll] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE TABLE [PayrollCorrection] (
        [Id] int NOT NULL IDENTITY,
        [EmployeePayrollId] int NOT NULL,
        [OriginalRevision] uniqueidentifier NOT NULL,
        [OriginalInputsJson] nvarchar(max) NOT NULL,
        [OriginalSnapshotJson] nvarchar(max) NOT NULL,
        [OriginalAdjustmentsJson] nvarchar(max) NOT NULL,
        [OriginalApprovedAt] datetime2 NULL,
        [OriginalApprovedBy] int NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [CorrectedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PayrollCorrection] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollCorrection_EmployeePayroll_EmployeePayrollId] FOREIGN KEY ([EmployeePayrollId]) REFERENCES [EmployeePayroll] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Employee_NationalId] ON [Employee] ([NationalId]) WHERE [NationalId] IS NOT NULL AND [NationalId] <> N''''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE INDEX [IX_EmployeeEmploymentHistory_EmployeeId] ON [EmployeeEmploymentHistory] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeePayroll_EmployeeId_Year_Month] ON [EmployeePayroll] ([EmployeeId], [Year], [Month]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE INDEX [IX_EmployeePayroll_PolicyId] ON [EmployeePayroll] ([PolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE INDEX [IX_EmployeePayroll_SalaryVersionId] ON [EmployeePayroll] ([SalaryVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeeSalaryVersion_EmployeeId_EffectiveFrom] ON [EmployeeSalaryVersion] ([EmployeeId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE INDEX [IX_PayrollAdjustment_EmployeePayrollId] ON [PayrollAdjustment] ([EmployeePayrollId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE INDEX [IX_PayrollCorrection_EmployeePayrollId] ON [PayrollCorrection] ([EmployeePayrollId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PayrollPolicy_Category_EffectiveFrom] ON [PayrollPolicy] ([Category], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.PayrollPolicy WHERE Category=N'JO-PRIVATE-GENERAL' AND EffectiveFrom='20260101')
    INSERT INTO dbo.PayrollPolicy
    (Name,Category,EffectiveFrom,EffectiveTo,SalaryDayDivisor,EmployeeSscPercent,EmployerSscPercent,InsurableWageCeiling,
     DefaultDailyHours,DefaultWorkingDaysPerWeek,DefaultMonthlyHours,LegalDailyHours,LegalWeeklyHours,MinimumNormalMultiplier,MinimumHolidayMultiplier,
     AdvanceDeductionMaximumPercent,WeekStartsOn,ProrationMethod,IncludeFixedAllowancesInWageBasis,IsLegallyReviewed,SourceReference,ReviewedBy,ReviewReason,CreatedDate,UpdatedDate)
    VALUES
    (N'الأردن — القطاع الخاص العام — ٢٠٢٦',N'JO-PRIVATE-GENERAL','20260101','20261231',30,7.5,14.25,3733,
     8,6,208,8,48,1.25,1.50,10,6,1,0,0,N'https://www.ssc.gov.jo/wp-content/uploads/2021/10/قانون-الضمان-الاجتماعي-رقم-١-لسنة-٢٠١٤-1.pdf ; https://www.mol.gov.jo/AR/List/القوانين ; https://org.petra.gov.jo/Include/InnerPage.jsp?ID=348404&lang=ar&name=news ; قاسم اليوم وطريقة الإجازة يلزمان مراجعة العقد والسياسة، ولا يعتمدان تلقائياً.',NULL,NULL,SYSDATETIME(),SYSDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023072627_AddJordanEmployeePayroll', N'9.0.6');
END;

COMMIT;
GO

