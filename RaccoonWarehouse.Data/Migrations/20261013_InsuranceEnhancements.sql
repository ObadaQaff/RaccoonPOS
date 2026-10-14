BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceAttachments] (
        [Id] int NOT NULL IDENTITY,
        [OwnerType] int NOT NULL,
        [OwnerId] int NOT NULL,
        [FileName] nvarchar(max) NOT NULL,
        [StoragePath] nvarchar(max) NOT NULL,
        [ContentType] nvarchar(max) NULL,
        [FileSize] bigint NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceAttachments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceBeneficiaryCategories] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [NameEnglish] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceBeneficiaryCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaimAdjustments] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceClaimId] int NOT NULL,
        [Type] int NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [IsAccountingPosted] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimAdjustments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaimBatches] (
        [Id] int NOT NULL IDENTITY,
        [BatchNumber] nvarchar(max) NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [PeriodFrom] datetime2 NOT NULL,
        [PeriodTo] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [SubmittedDate] datetime2 NULL,
        [ExportFileName] nvarchar(max) NULL,
        [SubmittedAmount] decimal(18,6) NOT NULL,
        [ApprovedAmount] decimal(18,6) NOT NULL,
        [DeductionAmount] decimal(18,6) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimBatches] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaimLines] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceClaimId] int NOT NULL,
        [InvoiceLineId] int NOT NULL,
        [SubmittedAmount] decimal(18,6) NOT NULL,
        [ApprovedAmount] decimal(18,6) NOT NULL,
        [RejectedAmount] decimal(18,6) NOT NULL,
        [DeductionAmount] decimal(18,6) NOT NULL,
        [DeductionReason] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimLines] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaimPaymentAllocations] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceClaimPaymentId] int NOT NULL,
        [InsuranceClaimId] int NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimPaymentAllocations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaimPayments] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceCompanyId] int NOT NULL,
        [ReceiptNumber] nvarchar(max) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [FinancialTransactionId] int NULL,
        [ReferenceNumber] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaimPayments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceClaims] (
        [Id] int NOT NULL IDENTITY,
        [ClaimNumber] nvarchar(450) NOT NULL,
        [InvoiceId] int NOT NULL,
        [InsuranceInvoiceCoverageId] int NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [InsuranceMemberPolicyId] int NOT NULL,
        [InsuranceClaimBatchId] int NULL,
        [Status] int NOT NULL,
        [SubmissionDate] datetime2 NULL,
        [DecisionDate] datetime2 NULL,
        [SubmittedAmount] decimal(18,6) NOT NULL,
        [ApprovedAmount] decimal(18,6) NOT NULL,
        [RejectedAmount] decimal(18,6) NOT NULL,
        [DeductionAmount] decimal(18,6) NOT NULL,
        [PaidAmount] decimal(18,6) NOT NULL,
        [OutstandingAmount] decimal(18,6) NOT NULL,
        [RejectionReason] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceClaims] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceCompanies] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [NameEnglish] nvarchar(max) NULL,
        [TaxNumber] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [ReceivableAccountId] int NULL,
        [DeductionAccountId] int NULL,
        [PaymentAccountId] int NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceCompanies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceContracts] (
        [Id] int NOT NULL IDENTITY,
        [ContractNumber] nvarchar(450) NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [NetworkId] int NULL,
        [Name] nvarchar(max) NOT NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [SettlementTerms] nvarchar(max) NULL,
        [PaymentDueDays] int NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceContracts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceContractVersions] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceContractId] int NOT NULL,
        [VersionNumber] int NOT NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceContractVersions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceCoverageRules] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceContractVersionId] int NOT NULL,
        [ProductId] int NULL,
        [CategoryId] int NULL,
        [ServiceCode] nvarchar(max) NULL,
        [CoveragePercent] decimal(18,6) NOT NULL,
        [CopayPercent] decimal(18,6) NOT NULL,
        [DeductibleAmount] decimal(18,6) NOT NULL,
        [PerClaimLimit] decimal(18,6) NULL,
        [AnnualLimit] decimal(18,6) NULL,
        [RequiresAuthorization] bit NOT NULL,
        [IsExcluded] bit NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceCoverageRules] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceExclusions] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceContractVersionId] int NOT NULL,
        [ProductId] int NULL,
        [CategoryId] int NULL,
        [ServiceCode] nvarchar(max) NULL,
        [Reason] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceExclusions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceInvoiceCoverages] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [InsuranceContractId] int NOT NULL,
        [InsuranceContractVersionId] int NOT NULL,
        [InsuranceMemberPolicyId] int NOT NULL,
        [GrossAmount] decimal(18,6) NOT NULL,
        [DiscountAmount] decimal(18,6) NOT NULL,
        [DeductibleAmount] decimal(18,6) NOT NULL,
        [CopayAmount] decimal(18,6) NOT NULL,
        [InsurerAmount] decimal(18,6) NOT NULL,
        [PatientAmount] decimal(18,6) NOT NULL,
        [SnapshotJson] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceInvoiceCoverages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceMemberPolicies] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [InsuranceContractId] int NOT NULL,
        [BeneficiaryCategoryId] int NULL,
        [MemberNumber] nvarchar(450) NOT NULL,
        [PolicyNumber] nvarchar(max) NULL,
        [CardNumber] nvarchar(max) NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [IsPrimary] bit NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceMemberPolicies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsuranceNetworks] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [NameEnglish] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceNetworks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE TABLE [InsurancePreAuthorizations] (
        [Id] int NOT NULL IDENTITY,
        [AuthorizationNumber] nvarchar(450) NOT NULL,
        [InsuranceCompanyId] int NOT NULL,
        [InsuranceMemberPolicyId] int NOT NULL,
        [InvoiceId] int NULL,
        [Status] int NOT NULL,
        [RequestedDate] datetime2 NOT NULL,
        [DecisionDate] datetime2 NULL,
        [ValidTo] datetime2 NULL,
        [RequestedAmount] decimal(18,6) NOT NULL,
        [ApprovedAmount] decimal(18,6) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsurancePreAuthorizations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE INDEX [IX_InsuranceAttachments_OwnerType_OwnerId] ON [InsuranceAttachments] ([OwnerType], [OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceBeneficiaryCategories_Code] ON [InsuranceBeneficiaryCategories] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceClaimLines_InsuranceClaimId_InvoiceLineId] ON [InsuranceClaimLines] ([InsuranceClaimId], [InvoiceLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceClaimPaymentAllocations_InsuranceClaimPaymentId_InsuranceClaimId] ON [InsuranceClaimPaymentAllocations] ([InsuranceClaimPaymentId], [InsuranceClaimId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceClaims_ClaimNumber] ON [InsuranceClaims] ([ClaimNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE INDEX [IX_InsuranceClaims_InsuranceCompanyId_Status_SubmissionDate] ON [InsuranceClaims] ([InsuranceCompanyId], [Status], [SubmissionDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceCompanies_Code] ON [InsuranceCompanies] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceContracts_ContractNumber] ON [InsuranceContracts] ([ContractNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceContractVersions_InsuranceContractId_VersionNumber] ON [InsuranceContractVersions] ([InsuranceContractId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceInvoiceCoverages_InvoiceId] ON [InsuranceInvoiceCoverages] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE INDEX [IX_InsuranceMemberPolicies_InsuranceCompanyId_MemberNumber] ON [InsuranceMemberPolicies] ([InsuranceCompanyId], [MemberNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceNetworks_Code] ON [InsuranceNetworks] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsurancePreAuthorizations_AuthorizationNumber] ON [InsurancePreAuthorizations] ([AuthorizationNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013123927_AddInsuranceManagement', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceMemberPolicies] ADD [EligibilityStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceMemberPolicies] ADD [ExternalVerificationReference] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceMemberPolicies] ADD [InsurancePlanId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceMemberPolicies] ADD [LastVerificationMethod] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceMemberPolicies] ADD [LastVerifiedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceContracts] ADD [AdministratorId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceContracts] ADD [ContractualPayerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    ALTER TABLE [InsuranceCompanies] ADD [OrganizationType] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE TABLE [InsuranceEligibilityChecks] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceMemberPolicyId] int NOT NULL,
        [Status] int NOT NULL,
        [Method] int NOT NULL,
        [PerformedByUserId] int NULL,
        [CheckedAt] datetime2 NOT NULL,
        [ValidTo] datetime2 NULL,
        [ExternalReference] nvarchar(max) NULL,
        [ResponseSummary] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceEligibilityChecks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE TABLE [InsuranceInvoiceCoverageLines] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceInvoiceCoverageId] int NOT NULL,
        [InvoiceLineId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [ContractedUnitPrice] decimal(18,6) NOT NULL,
        [GrossAmount] decimal(18,6) NOT NULL,
        [CoveredAmount] decimal(18,6) NOT NULL,
        [PatientAmount] decimal(18,6) NOT NULL,
        [ExcludedAmount] decimal(18,6) NOT NULL,
        [DeductibleAmount] decimal(18,6) NOT NULL,
        [CopayAmount] decimal(18,6) NOT NULL,
        [ApprovedQuantity] decimal(18,6) NOT NULL,
        [RequiresAuthorization] bit NOT NULL,
        [IsExcluded] bit NOT NULL,
        [DecisionReason] nvarchar(max) NULL,
        [InsurancePreAuthorizationId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsuranceInvoiceCoverageLines] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE TABLE [InsurancePlans] (
        [Id] int NOT NULL IDENTITY,
        [InsuranceContractId] int NOT NULL,
        [InsuranceContractVersionId] int NULL,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [NameEnglish] nvarchar(max) NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsurancePlans] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE TABLE [InsurancePreAuthorizationLines] (
        [Id] int NOT NULL IDENTITY,
        [InsurancePreAuthorizationId] int NOT NULL,
        [ProductId] int NOT NULL,
        [RequestedQuantity] decimal(18,6) NOT NULL,
        [ApprovedQuantity] decimal(18,6) NOT NULL,
        [RequestedAmount] decimal(18,6) NOT NULL,
        [ApprovedAmount] decimal(18,6) NOT NULL,
        [Status] int NOT NULL,
        [RejectionReason] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InsurancePreAuthorizationLines] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE INDEX [IX_InsuranceEligibilityChecks_InsuranceMemberPolicyId_CheckedAt] ON [InsuranceEligibilityChecks] ([InsuranceMemberPolicyId], [CheckedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceInvoiceCoverageLines_InsuranceInvoiceCoverageId_InvoiceLineId] ON [InsuranceInvoiceCoverageLines] ([InsuranceInvoiceCoverageId], [InvoiceLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsurancePlans_InsuranceContractId_Code] ON [InsurancePlans] ([InsuranceContractId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsurancePreAuthorizationLines_InsurancePreAuthorizationId_ProductId] ON [InsurancePreAuthorizationLines] ([InsurancePreAuthorizationId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013132624_AddInsuranceEnhancements', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133107_AddInsurancePlanCoverageScope'
)
BEGIN
    ALTER TABLE [InsuranceExclusions] ADD [InsurancePlanId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133107_AddInsurancePlanCoverageScope'
)
BEGIN
    ALTER TABLE [InsuranceCoverageRules] ADD [InsurancePlanId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133107_AddInsurancePlanCoverageScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013133107_AddInsurancePlanCoverageScope', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InsuranceCompanyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InsuranceContractId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InsuranceContractVersionId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InsuranceMemberPolicyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [InsuranceResponsibility] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [IsInsuranceSale] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    ALTER TABLE [Invoice] ADD [PatientResponsibility] decimal(18,6) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013133255_AddInsuranceInvoiceBillingFields', N'9.0.6');
END;

COMMIT;
GO

