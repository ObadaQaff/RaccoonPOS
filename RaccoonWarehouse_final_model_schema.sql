SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251014134038_FixCascadePaths')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251014134038_FixCascadePaths', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251016070154_StockHandle')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251016070154_StockHandle', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251017101922_updateSupplier')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251017101922_updateSupplier', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251017102029_updateSupplier2')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251017102029_updateSupplier2', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251017141331_hello')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251017141331_hello', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251023020504_tempenddate')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251023020504_tempenddate', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251023021110_tempenddate2')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251023021110_tempenddate2', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251023021226_tempenddate22')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251023021226_tempenddate22', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251106022539_FixCascadeRelations')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251106022539_FixCascadeRelations', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251106022828_FixCascadeRelations1')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251106022828_FixCascadeRelations1', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251111044311_AddIsDeletedToEntities')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251111044311_AddIsDeletedToEntities', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251118010348_AddCheckAndUpdateInvoiceAndVoucher')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251118010348_AddCheckAndUpdateInvoiceAndVoucher', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251120050656_aaassad')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251120050656_aaassad', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251122135854_asas')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251122135854_asas', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251128102337_addExpierDate')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251128102337_addExpierDate', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251128112214_addExpierDate2')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20251128112214_addExpierDate2', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260212051339_initnewmigration')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260212051339_initnewmigration', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260302170500_AddProductUnitDefaultsAndNormalizedQuantities')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260302170500_AddProductUnitDefaultsAndNormalizedQuantities', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260326011000_AddCurrentStockPrices')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260326011000_AddCurrentStockPrices', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260326021000_AddStockLots')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260326021000_AddStockLots', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260327030000_AddStockAdjustmentAndBatchLifecycle')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260327030000_AddStockAdjustmentAndBatchLifecycle', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260411115659_Phase1AccountingFoundation')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260411115659_Phase1AccountingFoundation', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260513180000_AddAccountHierarchyColumns')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260513180000_AddAccountHierarchyColumns', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260523073000_AddFiscalYearLegacyFlag')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260523073000_AddFiscalYearLegacyFlag', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260523080500_AddOpeningBalanceBaseScopeUniqueIndex')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260523080500_AddOpeningBalanceBaseScopeUniqueIndex', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260523084500_AddExchangeRatesAndJournalLineCurrency')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260523084500_AddExchangeRatesAndJournalLineCurrency', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260523093000_AddTaxRatesAndJournalLineTaxFields')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260523093000_AddTaxRatesAndJournalLineTaxFields', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260523102000_AddInvoiceDueDate')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260523102000_AddInvoiceDueDate', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260523111500_AddBankReconciliationTables')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260523111500_AddBankReconciliationTables', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260523123000_AddRecurringJournals')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260523123000_AddRecurringJournals', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260523140000_AddAccountCashFlowCategory')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260523140000_AddAccountCashFlowCategory', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260528000100_AddCustomerBankDetails')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260528000100_AddCustomerBankDetails', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260730083917_AddIntegrationInbox')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260730083917_AddIntegrationInbox', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260804000820_AddVoucherPartyLinks')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260804000820_AddVoucherPartyLinks', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260804010000_RepairCreditPurchaseSupplierLinks')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260804010000_RepairCreditPurchaseSupplierLinks', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260819073117_AddInvoiceNotes')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260819073117_AddInvoiceNotes', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260819215403_AddStockDocumentDiscount')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260819215403_AddStockDocumentDiscount', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260819225929_AddStockDocumentPaymentTypeAndChecks')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260819225929_AddStockDocumentPaymentTypeAndChecks', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260820130922_AddProductUnitAlternateBarcode')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260820130922_AddProductUnitAlternateBarcode', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260821133518_IncreaseDecimalPrecisionToSix')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260821133518_IncreaseDecimalPrecisionToSix', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260824000000_AddHeldInvoiceColor')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260824000000_AddHeldInvoiceColor', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260824100000_AddFalconInvoiceNumber')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260824100000_AddFalconInvoiceNumber', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260825123502_AddPurchaseInvoiceLineDiscountAndFreeQuantity')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260825123502_AddPurchaseInvoiceLineDiscountAndFreeQuantity', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260825144746_AddStockItemLineDiscountAndFreeQuantity')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260825144746_AddStockItemLineDiscountAndFreeQuantity', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260827032526_AddInvoicePaymentAllocations')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260827032526_AddInvoicePaymentAllocations', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260830150000_AddStockOutOperationTypes')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260830150000_AddStockOutOperationTypes', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260913222727_AddAuditLogs')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260913222727_AddAuditLogs', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261012171306_AddPurchaseOrdersAndUserPermissions')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261012171306_AddPurchaseOrdersAndUserPermissions', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261013123927_AddInsuranceManagement')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261013123927_AddInsuranceManagement', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261013132624_AddInsuranceEnhancements')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261013132624_AddInsuranceEnhancements', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261013133107_AddInsurancePlanCoverageScope')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261013133107_AddInsurancePlanCoverageScope', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261013133255_AddInsuranceInvoiceBillingFields')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261013133255_AddInsuranceInvoiceBillingFields', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261017150000_RepairStockDocumentColumns')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261017150000_RepairStockDocumentColumns', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261017160000_AddProductSupplierAndAgent')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261017160000_AddProductSupplierAndAgent', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261021122000_AddPosProductCatalog')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261021122000_AddPosProductCatalog', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261021194648_AddPurchaseRequestDetails')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261021194648_AddPurchaseRequestDetails', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261021230000_AddPurchaseRequestApproval')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261021230000_AddPurchaseRequestApproval', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261022093000_RepairPurchaseRequestLineNullability')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261022093000_RepairPurchaseRequestLineNullability', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261022094500_AddPurchaseOrderPaymentTracking')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261022094500_AddPurchaseOrderPaymentTracking', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261022120000_AddInsuranceCompanyContractualDiscount')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261022120000_AddInsuranceCompanyContractualDiscount', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261022160000_AddPosInsuranceBilling')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261022160000_AddPosInsuranceBilling', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261023010000_AddPharmacyPurchaseBonus')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261023010000_AddPharmacyPurchaseBonus', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261023072627_AddJordanEmployeePayroll')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261023072627_AddJordanEmployeePayroll', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261023201000_AddPurchaseBonusPercentage')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261023201000_AddPurchaseBonusPercentage', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261026060636_AddJordanEInvoiceFields')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261026060636_AddJordanEInvoiceFields', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261027193000_AddJordanEInvoiceUuid')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261027193000_AddJordanEInvoiceUuid', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261028090000_AddCustomerRequests')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261028090000_AddCustomerRequests', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261028150000_AddUserNumber')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261028150000_AddUserNumber', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261028160000_AddCustomerRequestPhone')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261028160000_AddCustomerRequestPhone', N'9.0.6');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261028180000_AddLostCustomers')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261028180000_AddLostCustomers', N'9.0.6');
GO
CREATE SEQUENCE [dbo].[UserNumberSequence] AS int START WITH 1 INCREMENT BY 1 NO CYCLE;
GO


CREATE TABLE [AccountingOperations] (
    [Id] int NOT NULL IDENTITY,
    [ReferenceType] nvarchar(50) NOT NULL,
    [ReferenceId] int NOT NULL,
    [ReferenceNumber] nvarchar(100) NOT NULL,
    [OperationType] nvarchar(100) NOT NULL,
    [PayloadJson] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [RetryCount] int NOT NULL,
    [LastError] nvarchar(4000) NULL,
    [LastAttemptDate] datetime2 NULL,
    [CompletedDate] datetime2 NULL,
    [NextAttemptDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountingOperations] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Accounts] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(450) NOT NULL,
    [AccountCode] nvarchar(32) NULL,
    [AccountLevel] int NULL,
    [AccountNature] nvarchar(10) NULL,
    [AccountCategory] nvarchar(20) NULL,
    [AccountTypeCode] nvarchar(2) NULL,
    [CashFlowCategory] int NULL,
    [Name] nvarchar(max) NOT NULL,
    [ArabicName] nvarchar(max) NULL,
    [EnglishName] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [AccountType] int NOT NULL,
    [NormalBalanceType] int NOT NULL,
    [IsPosting] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [IsSystemGenerated] bit NOT NULL,
    [AllowManualEntry] bit NOT NULL,
    [Level] int NOT NULL,
    [CurrencyId] int NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [ParentAccountId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Accounts_Accounts_ParentAccountId] FOREIGN KEY ([ParentAccountId]) REFERENCES [Accounts] ([Id])
);
GO


IF OBJECT_ID(N'[AppSettings]', N'U') IS NULL
CREATE TABLE [AppSettings] (
    [Id] int NOT NULL IDENTITY,
    [Key] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_AppSettings] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Role] int NULL,
    [SessionId] uniqueidentifier NOT NULL,
    [Action] nvarchar(200) NOT NULL,
    [EntityType] nvarchar(200) NULL,
    [EntityId] int NULL,
    [Succeeded] bit NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Branches] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [ArabicName] nvarchar(max) NULL,
    [EnglishName] nvarchar(max) NULL,
    [Address] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Branches] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Brand] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Brand] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Category] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ImageUrl] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Category] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CostCenters] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [ArabicName] nvarchar(max) NULL,
    [EnglishName] nvarchar(max) NULL,
    [ParentCostCenterId] int NULL,
    [Level] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_CostCenters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CostCenters_CostCenters_ParentCostCenterId] FOREIGN KEY ([ParentCostCenterId]) REFERENCES [CostCenters] ([Id])
);
GO


CREATE TABLE [Currencies] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [ArabicName] nvarchar(max) NULL,
    [EnglishName] nvarchar(max) NULL,
    [Symbol] nvarchar(max) NULL,
    [ExchangeRate] decimal(18,6) NOT NULL,
    [IsBaseCurrency] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Currencies] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CustomerRequests] (
    [Id] int NOT NULL IDENTITY,
    [RequestNumber] nvarchar(max) NOT NULL,
    [CustomerId] int NOT NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    [CustomerPhone] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [ReceivingStatus] int NOT NULL,
    [SalesStatus] int NOT NULL,
    [TotalAmount] decimal(18,6) NOT NULL,
    [PaidAmount] decimal(18,6) NOT NULL,
    [PaymentNote] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [ApprovedBy] int NULL,
    [ApprovedDate] datetime2 NULL,
    [CancelledBy] int NULL,
    [CancelledDate] datetime2 NULL,
    [PurchaseRequestId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_CustomerRequests] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [FiscalYears] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [IsClosed] bit NOT NULL,
    [IsLegacy] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_FiscalYears] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [GoodsReceipts] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptNumber] nvarchar(450) NOT NULL,
    [PurchaseOrderId] int NOT NULL,
    [SupplierId] int NULL,
    [WarehouseId] int NULL,
    [BranchId] int NULL,
    [ReceivedBy] int NULL,
    [ReceiptDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [ConfirmedDate] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_GoodsReceipts] PRIMARY KEY ([Id])
);
GO


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
GO


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
GO


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
GO


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
GO


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
GO


CREATE TABLE [InsuranceClaimPaymentAllocations] (
    [Id] int NOT NULL IDENTITY,
    [InsuranceClaimPaymentId] int NOT NULL,
    [InsuranceClaimId] int NOT NULL,
    [Amount] decimal(18,6) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_InsuranceClaimPaymentAllocations] PRIMARY KEY ([Id])
);
GO


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
GO


CREATE TABLE [InsuranceClaims] (
    [Id] int NOT NULL IDENTITY,
    [ClaimNumber] nvarchar(450) NOT NULL,
    [InvoiceId] int NOT NULL,
    [InsuranceInvoiceCoverageId] int NOT NULL,
    [InsuranceCompanyId] int NOT NULL,
    [InsuranceMemberPolicyId] int NULL,
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
GO


CREATE TABLE [InsuranceCompanies] (
    [Id] int NOT NULL IDENTITY,
    [ContractualDiscountPercent] decimal(18,6) NOT NULL DEFAULT 0.0,
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
    [OrganizationType] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_InsuranceCompanies] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [InsuranceContracts] (
    [Id] int NOT NULL IDENTITY,
    [ContractNumber] nvarchar(450) NOT NULL,
    [InsuranceCompanyId] int NOT NULL,
    [NetworkId] int NULL,
    [AdministratorId] int NULL,
    [ContractualPayerId] int NULL,
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
GO


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
GO


CREATE TABLE [InsuranceCoverageRules] (
    [Id] int NOT NULL IDENTITY,
    [InsuranceContractVersionId] int NOT NULL,
    [InsurancePlanId] int NULL,
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
    [DiscountPercent] decimal(18,6) NOT NULL,
    [FixedDiscountAmount] decimal(18,6) NOT NULL,
    [ValidFrom] datetime2 NOT NULL,
    [ValidTo] datetime2 NOT NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_InsuranceCoverageRules] PRIMARY KEY ([Id])
);
GO


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
GO


CREATE TABLE [InsuranceExclusions] (
    [Id] int NOT NULL IDENTITY,
    [InsuranceContractVersionId] int NOT NULL,
    [InsurancePlanId] int NULL,
    [ProductId] int NULL,
    [CategoryId] int NULL,
    [ServiceCode] nvarchar(max) NULL,
    [Reason] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_InsuranceExclusions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [InsuranceInvoiceCoverageLines] (
    [Id] int NOT NULL IDENTITY,
    [InsuranceInvoiceCoverageId] int NOT NULL,
    [InvoiceLineId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [ContractedUnitPrice] decimal(18,6) NOT NULL,
    [InsuranceDiscountPercent] decimal(18,6) NOT NULL,
    [InsuranceDiscountAmount] decimal(18,6) NOT NULL,
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
GO


CREATE TABLE [InsuranceInvoiceCoverages] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [InsuranceCompanyId] int NOT NULL,
    [InsuranceContractId] int NULL,
    [InsuranceContractVersionId] int NULL,
    [InsuranceMemberPolicyId] int NULL,
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
GO


CREATE TABLE [InsuranceMemberPolicies] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NOT NULL,
    [InsuranceCompanyId] int NOT NULL,
    [InsuranceContractId] int NOT NULL,
    [InsurancePlanId] int NULL,
    [BeneficiaryCategoryId] int NULL,
    [MemberNumber] nvarchar(450) NOT NULL,
    [PolicyNumber] nvarchar(max) NULL,
    [CardNumber] nvarchar(max) NULL,
    [ValidFrom] datetime2 NOT NULL,
    [ValidTo] datetime2 NOT NULL,
    [IsPrimary] bit NOT NULL,
    [Status] int NOT NULL,
    [EligibilityStatus] int NOT NULL,
    [LastVerifiedDate] datetime2 NULL,
    [LastVerificationMethod] int NULL,
    [ExternalVerificationReference] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_InsuranceMemberPolicies] PRIMARY KEY ([Id])
);
GO


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
GO


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
GO


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
GO


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
GO


CREATE TABLE [IntegrationInbox] (
    [Id] int NOT NULL IDENTITY,
    [EventId] uniqueidentifier NOT NULL,
    [EventType] nvarchar(100) NOT NULL,
    [EventVersion] int NOT NULL,
    [SourceSystem] nvarchar(50) NOT NULL,
    [ExternalOrderId] nvarchar(100) NOT NULL,
    [PayloadHash] nvarchar(64) NOT NULL,
    [Status] int NOT NULL,
    [ReceivedAtUtc] datetime2 NOT NULL,
    [ProcessedAtUtc] datetime2 NULL,
    [RaccoonInvoiceId] int NULL,
    [LastErrorCode] nvarchar(100) NULL,
    [LastErrorSummary] nvarchar(1000) NULL,
    [RowVersion] rowversion NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_IntegrationInbox] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LeaveTypes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [IsPaid] bit NOT NULL,
    [DefaultAnnualBalance] decimal(18,6) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_LeaveTypes] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LostCustomers] (
    [Id] int NOT NULL IDENTITY,
    [Type] int NOT NULL,
    [Reason] int NOT NULL,
    [ProductId] int NULL,
    [ProductUnitId] int NULL,
    [ProductName] nvarchar(max) NOT NULL,
    [Quantity] decimal(18,6) NULL,
    [Message] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedBy] int NULL,
    [CreatedByName] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_LostCustomers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PayrollPeriods] (
    [Id] int NOT NULL IDENTITY,
    [PeriodMonth] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [ApprovedAt] datetime2 NULL,
    [PaidAt] datetime2 NULL,
    [ApprovalNote] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PayrollPeriods] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PermissionGroups] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(450) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsProtectedAdministrator] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PermissionGroups] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PosProductCatalogEntry] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NOT NULL,
    [SubCategoryId] int NOT NULL,
    [ProductName] nvarchar(450) NULL,
    [ItemCode] bigint NULL,
    [TaxExempt] bit NULL,
    [TaxRate] decimal(18,6) NULL,
    [AlternateBarcode] nvarchar(450) NULL,
    [UnitName] nvarchar(max) NULL,
    [UnitSalePrice] decimal(18,6) NOT NULL,
    [UnitPurchasePrice] decimal(18,6) NOT NULL,
    [QuantityPerUnit] decimal(18,6) NOT NULL,
    [AllowFractionalSale] bit NOT NULL,
    [FractionalPartsPerUnit] decimal(18,6) NOT NULL,
    [FractionalPartName] nvarchar(max) NULL,
    [FractionalSalePrice] decimal(18,6) NULL,
    [FractionalSaleCost] decimal(18,6) NULL,
    [IsBaseUnit] bit NOT NULL,
    [IsDefaultSaleUnit] bit NOT NULL,
    [IsDefaultPurchaseUnit] bit NOT NULL,
    [SupplierId] int NULL,
    [SupplierName] nvarchar(max) NULL,
    [StockQuantity] decimal(18,6) NOT NULL,
    [StockPurchasePrice] decimal(18,6) NOT NULL,
    [StockSalePrice] decimal(18,6) NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [AvailableToSale] bit NOT NULL,
    [StockStatus] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PosProductCatalogEntry] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PurchaseControlEntries] (
    [Id] int NOT NULL IDENTITY,
    [PaymentMethod] nvarchar(max) NULL,
    [PurchaseOrderNumber] nvarchar(max) NULL,
    [PurchaseOrderDate] datetime2 NULL,
    [PurchaseRequestNumber] nvarchar(max) NULL,
    [SupplierName] nvarchar(max) NULL,
    [PurchaseOrderAmount] decimal(18,6) NULL,
    [InvoiceNumber] nvarchar(max) NULL,
    [InvoiceDate] datetime2 NULL,
    [DeliveryDate] datetime2 NULL,
    [Description] nvarchar(max) NULL,
    [Category] nvarchar(max) NULL,
    [PaidAmount] decimal(18,6) NULL,
    [DueAmount] decimal(18,6) NULL,
    [ReceiptAmount] decimal(18,6) NULL,
    [PaymentStatus] nvarchar(max) NULL,
    [DueDate] datetime2 NULL,
    [LinkCheck] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [OverdueDays] int NULL,
    [ReceivedValue] decimal(18,6) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseControlEntries] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PurchaseInvoiceMatches] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [PurchaseOrderId] int NULL,
    [SupplierId] int NULL,
    [SupplierInvoiceNumber] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [PriceTolerance] decimal(18,6) NOT NULL,
    [QuantityTolerance] decimal(18,6) NOT NULL,
    [DiscrepancyReason] nvarchar(max) NULL,
    [MatchedDate] datetime2 NULL,
    [MatchedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseInvoiceMatches] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PurchaseRequests] (
    [Id] int NOT NULL IDENTITY,
    [RequestNumber] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [Category] int NOT NULL,
    [RequestDate] datetime2 NOT NULL,
    [RequesterId] int NULL,
    [RequesterName] nvarchar(200) NULL,
    [BranchId] int NULL,
    [Department] nvarchar(max) NULL,
    [Priority] nvarchar(max) NULL,
    [Justification] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [SubmittedBy] int NULL,
    [SubmittedDate] datetime2 NULL,
    [ApprovedBy] int NULL,
    [ApprovedDate] datetime2 NULL,
    [RejectedBy] int NULL,
    [RejectedDate] datetime2 NULL,
    [RejectionReason] nvarchar(max) NULL,
    [ReviewNotes] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseRequests] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PurchaseReturns] (
    [Id] int NOT NULL IDENTITY,
    [ReturnNumber] nvarchar(450) NOT NULL,
    [SupplierId] int NULL,
    [OriginalInvoiceId] int NULL,
    [GoodsReceiptId] int NULL,
    [CashierSessionId] int NULL,
    [Status] int NOT NULL,
    [Reason] nvarchar(max) NULL,
    [ReturnDate] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ConfirmedBy] int NULL,
    [ConfirmedDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseReturns] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [RecurringJournals] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Frequency] int NOT NULL,
    [NextRunDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [LastPostedDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_RecurringJournals] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ShiftTemplates] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(450) NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [IsOvernight] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_ShiftTemplates] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SocialSecurityRules] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NULL,
    [EmployeeRate] decimal(18,6) NOT NULL,
    [EmployerRate] decimal(18,6) NOT NULL,
    [MinimumBase] decimal(18,6) NULL,
    [MaximumBase] decimal(18,6) NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_SocialSecurityRules] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SupplierAdvances] (
    [Id] int NOT NULL IDENTITY,
    [VoucherId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [Amount] decimal(18,6) NOT NULL,
    [AllocatedAmount] decimal(18,6) NOT NULL,
    [AdvanceDate] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierAdvances] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SupplierPaymentAllocations] (
    [Id] int NOT NULL IDENTITY,
    [VoucherId] int NOT NULL,
    [InvoiceId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [Amount] decimal(18,6) NOT NULL,
    [AllocationDate] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierPaymentAllocations] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Tax] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Rate] decimal(18,6) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Tax] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Unit] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Unit] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [User] (
    [Id] int NOT NULL IDENTITY,
    [UserNumber] int NOT NULL DEFAULT (NEXT VALUE FOR dbo.UserNumberSequence),
    [Name] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [Password] nvarchar(max) NOT NULL,
    [Role] int NOT NULL,
    [BankName] nvarchar(max) NULL,
    [BankAccountNumber] nvarchar(max) NULL,
    [BankIban] nvarchar(max) NULL,
    [BankSwiftCode] nvarchar(max) NULL,
    [CreditLimit] decimal(18,6) NOT NULL,
    [CreditDays] int NOT NULL,
    [OpeningBalance] decimal(18,6) NOT NULL,
    [CurrentBalance] decimal(18,6) NOT NULL,
    [LastPaymentDate] datetime2 NULL,
    [CreditStatus] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Warehouse] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(max) NULL,
    [Name] nvarchar(max) NOT NULL,
    [Location] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [PhoneNumber] int NULL,
    [BranchId] int NULL,
    [Status] int NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Warehouse] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [TaxRates] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(450) NOT NULL,
    [Rate] decimal(18,6) NOT NULL,
    [TaxAccountId] int NOT NULL,
    [TaxType] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_TaxRates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaxRates_Accounts_TaxAccountId] FOREIGN KEY ([TaxAccountId]) REFERENCES [Accounts] ([Id])
);
GO


CREATE TABLE [SubCategory] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [ParentCategoryId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_SubCategory] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SubCategory_Category_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Category] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [BankAccounts] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [AccountNumber] nvarchar(450) NOT NULL,
    [BankName] nvarchar(max) NOT NULL,
    [GlAccountId] int NOT NULL,
    [CurrencyId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_BankAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankAccounts_Accounts_GlAccountId] FOREIGN KEY ([GlAccountId]) REFERENCES [Accounts] ([Id]),
    CONSTRAINT [FK_BankAccounts_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [Currencies] ([Id])
);
GO


CREATE TABLE [ExchangeRates] (
    [Id] int NOT NULL IDENTITY,
    [FromCurrencyId] int NOT NULL,
    [ToCurrencyId] int NOT NULL,
    [Rate] decimal(18,6) NOT NULL,
    [EffectiveDate] datetime2 NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_ExchangeRates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExchangeRates_Currencies_FromCurrencyId] FOREIGN KEY ([FromCurrencyId]) REFERENCES [Currencies] ([Id]),
    CONSTRAINT [FK_ExchangeRates_Currencies_ToCurrencyId] FOREIGN KEY ([ToCurrencyId]) REFERENCES [Currencies] ([Id])
);
GO


CREATE TABLE [CustomerRequestLines] (
    [Id] int NOT NULL IDENTITY,
    [CustomerRequestId] int NOT NULL,
    [ProductId] int NULL,
    [ProductUnitId] int NULL,
    [ItemName] nvarchar(max) NOT NULL,
    [UnitName] nvarchar(max) NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [UnitPrice] decimal(18,6) NOT NULL,
    [ReceivedQuantity] decimal(18,6) NOT NULL,
    [SoldQuantity] decimal(18,6) NOT NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_CustomerRequestLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerRequestLines_CustomerRequests_CustomerRequestId] FOREIGN KEY ([CustomerRequestId]) REFERENCES [CustomerRequests] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AccountingPeriods] (
    [Id] int NOT NULL IDENTITY,
    [FiscalYearId] int NOT NULL,
    [PeriodNumber] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [IsClosed] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountingPeriods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountingPeriods_FiscalYears_FiscalYearId] FOREIGN KEY ([FiscalYearId]) REFERENCES [FiscalYears] ([Id])
);
GO


CREATE TABLE [AccountOpeningBalances] (
    [Id] int NOT NULL IDENTITY,
    [FiscalYearId] int NOT NULL,
    [AccountId] int NOT NULL,
    [BranchId] int NULL,
    [CostCenterId] int NULL,
    [WarehouseId] int NULL,
    [PartyUserId] int NULL,
    [Debit] decimal(18,6) NOT NULL,
    [Credit] decimal(18,6) NOT NULL,
    [ReferenceNumber] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountOpeningBalances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountOpeningBalances_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]),
    CONSTRAINT [FK_AccountOpeningBalances_FiscalYears_FiscalYearId] FOREIGN KEY ([FiscalYearId]) REFERENCES [FiscalYears] ([Id])
);
GO


CREATE TABLE [GoodsReceiptLines] (
    [Id] int NOT NULL IDENTITY,
    [GoodsReceiptId] int NOT NULL,
    [PurchaseOrderLineId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NOT NULL,
    [QuantityShipped] decimal(18,6) NOT NULL,
    [QuantityReceived] decimal(18,6) NOT NULL,
    [QuantityAccepted] decimal(18,6) NOT NULL,
    [QuantityRejected] decimal(18,6) NOT NULL,
    [QuantityDamaged] decimal(18,6) NOT NULL,
    [FreeQuantity] decimal(18,6) NOT NULL,
    [BatchNumber] nvarchar(max) NULL,
    [ExpiryDate] datetime2 NULL,
    [StorageLocation] nvarchar(max) NULL,
    [RejectionReason] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_GoodsReceiptLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId] FOREIGN KEY ([GoodsReceiptId]) REFERENCES [GoodsReceipts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PermissionGroupPermissions] (
    [Id] int NOT NULL IDENTITY,
    [PermissionGroupId] int NOT NULL,
    [AreaKey] nvarchar(450) NOT NULL,
    [Level] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PermissionGroupPermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PermissionGroupPermissions_PermissionGroups_PermissionGroupId] FOREIGN KEY ([PermissionGroupId]) REFERENCES [PermissionGroups] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseInvoiceMatchLines] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseInvoiceMatchId] int NOT NULL,
    [InvoiceLineId] int NOT NULL,
    [PurchaseOrderLineId] int NOT NULL,
    [InvoicedQuantity] decimal(18,6) NOT NULL,
    [AcceptedQuantity] decimal(18,6) NOT NULL,
    [InvoicedUnitPrice] decimal(18,6) NOT NULL,
    [OrderedUnitPrice] decimal(18,6) NOT NULL,
    [PriceVariance] decimal(18,6) NOT NULL,
    [QuantityVariance] decimal(18,6) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseInvoiceMatchLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseInvoiceMatchLines_PurchaseInvoiceMatches_PurchaseInvoiceMatchId] FOREIGN KEY ([PurchaseInvoiceMatchId]) REFERENCES [PurchaseInvoiceMatches] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseRequestLines] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseRequestId] int NOT NULL,
    [ProductId] int NULL,
    [ProductUnitId] int NULL,
    [Barcode] nvarchar(100) NULL,
    [ItemName] nvarchar(500) NOT NULL,
    [UnitName] nvarchar(100) NULL,
    [UnitPrice] decimal(18,6) NOT NULL,
    [ApprovedUnitPrice] decimal(18,6) NOT NULL,
    [SupplierId] int NULL,
    [SupplierName] nvarchar(200) NULL,
    [Description] nvarchar(max) NULL,
    [RequestedQuantity] decimal(18,6) NOT NULL,
    [ApprovedQuantity] decimal(18,6) NOT NULL,
    [Decision] int NOT NULL,
    [AvailableStockSnapshot] decimal(18,6) NOT NULL,
    [ReservedStockSnapshot] decimal(18,6) NOT NULL,
    [AlreadyOrderedQuantitySnapshot] decimal(18,6) NOT NULL,
    [MinimumStockSnapshot] decimal(18,6) NOT NULL,
    [AverageSalesSnapshot] decimal(18,6) NOT NULL,
    [LastPurchaseDateSnapshot] datetime2 NULL,
    [ExpectedStockoutDate] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [ApprovalNotes] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseRequestLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseRequestLines_PurchaseRequests_PurchaseRequestId] FOREIGN KEY ([PurchaseRequestId]) REFERENCES [PurchaseRequests] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseReturnLines] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseReturnId] int NOT NULL,
    [GoodsReceiptLineId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NOT NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [BatchNumber] nvarchar(max) NULL,
    [ExpiryDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseReturnLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseReturnLines_PurchaseReturns_PurchaseReturnId] FOREIGN KEY ([PurchaseReturnId]) REFERENCES [PurchaseReturns] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RecurringJournalLines] (
    [Id] int NOT NULL IDENTITY,
    [RecurringJournalId] int NOT NULL,
    [AccountId] int NOT NULL,
    [CostCenterId] int NULL,
    [DebitAmount] decimal(18,6) NOT NULL,
    [CreditAmount] decimal(18,6) NOT NULL,
    [Description] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_RecurringJournalLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecurringJournalLines_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]),
    CONSTRAINT [FK_RecurringJournalLines_CostCenters_CostCenterId] FOREIGN KEY ([CostCenterId]) REFERENCES [CostCenters] ([Id]),
    CONSTRAINT [FK_RecurringJournalLines_RecurringJournals_RecurringJournalId] FOREIGN KEY ([RecurringJournalId]) REFERENCES [RecurringJournals] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ShiftTemplateWorkingDays] (
    [Id] int NOT NULL IDENTITY,
    [ShiftTemplateId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_ShiftTemplateWorkingDays] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShiftTemplateWorkingDays_ShiftTemplates_ShiftTemplateId] FOREIGN KEY ([ShiftTemplateId]) REFERENCES [ShiftTemplates] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CashierSession] (
    [Id] int NOT NULL IDENTITY,
    [CashierId] int NOT NULL,
    [SessionNumber] nvarchar(max) NULL,
    [BranchId] int NULL,
    [OpenedAt] datetime2 NOT NULL,
    [ClosedAt] datetime2 NULL,
    [StatrBalance] decimal(18,6) NOT NULL,
    [EndingBalance] decimal(18,6) NOT NULL,
    [ExpectedClosingBalance] decimal(18,6) NULL,
    [DifferenceAmount] decimal(18,6) NULL,
    [ExpectedVisa] decimal(18,6) NULL,
    [CountedVisa] decimal(18,6) NULL,
    [VisaDifference] decimal(18,6) NULL,
    [ClosingNotes] nvarchar(max) NULL,
    [ClosedBy] int NULL,
    [SettlementCalculatedAt] datetime2 NULL,
    [SettlementCountedCash] decimal(18,6) NULL,
    [SettlementCountedVisa] decimal(18,6) NULL,
    [SettlementExpectedCash] decimal(18,6) NULL,
    [SettlementExpectedVisa] decimal(18,6) NULL,
    [SettlementCashDifference] decimal(18,6) NULL,
    [SettlementVisaDifference] decimal(18,6) NULL,
    [SettlementLocked] bit NOT NULL,
    [Status] int NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_CashierSession] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashierSession_User_CashierId] FOREIGN KEY ([CashierId]) REFERENCES [User] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Delegate] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Code] nvarchar(450) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [AlternatePhoneNumber] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [DelegateType] int NOT NULL,
    [RegionId] int NULL,
    [AreaName] nvarchar(max) NULL,
    [HireDate] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedBy] int NULL,
    [ModifiedBy] int NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Delegate] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Delegate_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id])
);
GO


CREATE TABLE [Employees] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(450) NOT NULL,
    [PhoneNumber] nvarchar(450) NOT NULL,
    [NationalId] nvarchar(450) NULL,
    [Address] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [EmploymentDate] datetime2 NULL,
    [Status] int NOT NULL,
    [JobTitle] nvarchar(max) NULL,
    [MonthlyBasicSalary] decimal(18,6) NOT NULL,
    [SocialSecurityEnrolled] bit NOT NULL,
    [SocialSecurityNumber] nvarchar(max) NULL,
    [SocialSecurityEnrollmentDate] datetime2 NULL,
    [ContractType] int NOT NULL,
    [ContractStartDate] datetime2 NOT NULL,
    [ContractEndDate] datetime2 NULL,
    [PrimaryBranchId] int NOT NULL,
    [UserId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Employees_Branches_PrimaryBranchId] FOREIGN KEY ([PrimaryBranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employees_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [PurchaseOrders] (
    [Id] int NOT NULL IDENTITY,
    [OrderNumber] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [PaymentStatus] int NOT NULL,
    [SupplierId] int NULL,
    [WarehouseId] int NULL,
    [CurrencyId] int NULL,
    [PaymentType] int NULL,
    [DocumentDate] datetime2 NULL,
    [DueDate] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [DiscountAmount] decimal(18,6) NULL,
    [SubTotal] decimal(18,6) NOT NULL,
    [TotalTax] decimal(18,6) NOT NULL,
    [TotalAmount] decimal(18,6) NOT NULL,
    [CostPriceIncludesTax] bit NOT NULL,
    [CreatedBy] int NULL,
    [SubmittedBy] int NULL,
    [ApprovedBy] int NULL,
    [ApprovedDate] datetime2 NULL,
    [RejectedBy] int NULL,
    [RejectedDate] datetime2 NULL,
    [RejectionReason] nvarchar(max) NULL,
    [PaidBy] int NULL,
    [PaidDate] datetime2 NULL,
    [PaymentNote] nvarchar(max) NULL,
    [ConvertedInvoiceId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrders_User_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [User] ([Id])
);
GO


CREATE TABLE [StockDocument] (
    [Id] int NOT NULL IDENTITY,
    [DocumentNumber] nvarchar(450) NOT NULL,
    [FalconInvoiceNumber] nvarchar(max) NULL,
    [Type] int NOT NULL,
    [OperationType] int NOT NULL,
    [SourceDocumentId] nvarchar(max) NULL,
    [DocumentDate] datetime2 NULL,
    [ReferenceNumber] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [SupplierId] int NULL,
    [CustomerId] int NULL,
    [WarehouseId] int NULL,
    [BranchId] int NULL,
    [PostingStatus] int NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [DiscountAmount] decimal(18,6) NULL,
    [PaymentType] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_StockDocument] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockDocument_User_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_StockDocument_User_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [User] ([Id])
);
GO


CREATE TABLE [UserPermissionGroups] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [PermissionGroupId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_UserPermissionGroups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserPermissionGroups_PermissionGroups_PermissionGroupId] FOREIGN KEY ([PermissionGroupId]) REFERENCES [PermissionGroups] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserPermissionGroups_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Product] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(450) NULL,
    [ITEMCODE] bigint NULL,
    [ImageUrl] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [Status] int NULL,
    [TaxExempt] bit NULL,
    [TaxRate] decimal(18,6) NULL,
    [MiniQuantity] decimal(18,6) NULL,
    [SubCategoryId] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [IsSoldOut] bit NULL,
    [BrandId] int NULL,
    [SupplierId] int NULL,
    [AgentName] nvarchar(max) NULL,
    [EndDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Product] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Product_Brand_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brand] ([Id]),
    CONSTRAINT [FK_Product_SubCategory_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [SubCategory] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Product_User_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [User] ([Id])
);
GO


CREATE TABLE [SubCategoryBrand] (
    [Id] int NOT NULL IDENTITY,
    [SubCategoryId] int NOT NULL,
    [BrandId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_SubCategoryBrand] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SubCategoryBrand_Brand_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brand] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SubCategoryBrand_SubCategory_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [SubCategory] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [BankStatements] (
    [Id] int NOT NULL IDENTITY,
    [BankAccountId] int NOT NULL,
    [StatementDate] datetime2 NOT NULL,
    [OpeningBalance] decimal(18,6) NOT NULL,
    [ClosingBalance] decimal(18,6) NOT NULL,
    [ImportedAt] datetime2 NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_BankStatements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankStatements_BankAccounts_BankAccountId] FOREIGN KEY ([BankAccountId]) REFERENCES [BankAccounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [JournalEntries] (
    [Id] int NOT NULL IDENTITY,
    [EntryNumber] nvarchar(450) NOT NULL,
    [EntryDate] datetime2 NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [IsDraft] bit NOT NULL,
    [SourceType] int NULL,
    [SourceId] int NULL,
    [ReferenceType] nvarchar(max) NULL,
    [ReferenceId] int NULL,
    [ReferenceNumber] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [FiscalYearId] int NULL,
    [AccountingPeriodId] int NULL,
    [BranchId] int NULL,
    [WarehouseId] int NULL,
    [CashierSessionId] int NULL,
    [CurrencyId] int NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [ApprovedBy] int NULL,
    [ApprovedAt] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_JournalEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JournalEntries_AccountingPeriods_AccountingPeriodId] FOREIGN KEY ([AccountingPeriodId]) REFERENCES [AccountingPeriods] ([Id]),
    CONSTRAINT [FK_JournalEntries_FiscalYears_FiscalYearId] FOREIGN KEY ([FiscalYearId]) REFERENCES [FiscalYears] ([Id])
);
GO


CREATE TABLE [FinancialTransaction] (
    [Id] int NOT NULL IDENTITY,
    [TransactionNumber] nvarchar(max) NOT NULL,
    [Type] int NOT NULL,
    [Direction] int NOT NULL,
    [Method] int NOT NULL,
    [Amount] decimal(18,6) NOT NULL,
    [Date] datetime2 NOT NULL,
    [InvoiceId] int NULL,
    [VoucherId] int NULL,
    [SourceType] int NOT NULL,
    [SourceId] int NULL,
    [ReferenceNumber] nvarchar(max) NULL,
    [CashierSessionId] int NULL,
    [CasherId] int NULL,
    [BranchId] int NULL,
    [WarehouseId] int NULL,
    [CurrencyId] int NULL,
    [ExchangeRate] decimal(18,6) NOT NULL,
    [Notes] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [PostingStatus] int NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_FinancialTransaction] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FinancialTransaction_CashierSession_CashierSessionId] FOREIGN KEY ([CashierSessionId]) REFERENCES [CashierSession] ([Id]),
    CONSTRAINT [FK_FinancialTransaction_User_CasherId] FOREIGN KEY ([CasherId]) REFERENCES [User] ([Id])
);
GO


CREATE TABLE [Voucher] (
    [Id] int NOT NULL IDENTITY,
    [VoucherNumber] nvarchar(max) NULL,
    [VoucherDate] datetime2 NULL,
    [VoucherType] int NOT NULL,
    [Amount] decimal(18,6) NOT NULL,
    [PaymentType] int NOT NULL,
    [CasherId] int NULL,
    [BranchId] int NULL,
    [WarehouseId] int NULL,
    [CustomerId] int NULL,
    [SupplierId] int NULL,
    [CurrencyId] int NULL,
    [ExchangeRate] decimal(18,6) NOT NULL,
    [ReferenceNumber] nvarchar(450) NULL,
    [Notes] nvarchar(max) NULL,
    [CashierSessionId] int NULL,
    [PostingStatus] int NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Voucher] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Voucher_CashierSession_CashierSessionId] FOREIGN KEY ([CashierSessionId]) REFERENCES [CashierSession] ([Id])
);
GO


CREATE TABLE [EmployeeBranchAssignments] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [BranchId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_EmployeeBranchAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeBranchAssignments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeBranchAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [EmployeePayrolls] (
    [Id] int NOT NULL IDENTITY,
    [PayrollPeriodId] int NOT NULL,
    [EmployeeId] int NOT NULL,
    [EmployeeNameSnapshot] nvarchar(max) NOT NULL,
    [BasicSalary] decimal(18,6) NOT NULL,
    [Allowances] decimal(18,6) NOT NULL,
    [Bonuses] decimal(18,6) NOT NULL,
    [Overtime] decimal(18,6) NOT NULL,
    [UnpaidAbsence] decimal(18,6) NOT NULL,
    [Deductions] decimal(18,6) NOT NULL,
    [EmployeeSocialSecurity] decimal(18,6) NOT NULL,
    [EmployerSocialSecurity] decimal(18,6) NOT NULL,
    [GrossAmount] decimal(18,6) NOT NULL,
    [NetAmount] decimal(18,6) NOT NULL,
    [CalculationSnapshotJson] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_EmployeePayrolls] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeePayrolls_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeePayrolls_PayrollPeriods_PayrollPeriodId] FOREIGN KEY ([PayrollPeriodId]) REFERENCES [PayrollPeriods] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [EmployeeShiftAssignments] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [ShiftTemplateId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_EmployeeShiftAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeShiftAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_EmployeeShiftAssignments_ShiftTemplates_ShiftTemplateId] FOREIGN KEY ([ShiftTemplateId]) REFERENCES [ShiftTemplates] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [HrAdjustments] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [PeriodMonth] datetime2 NOT NULL,
    [Type] int NOT NULL,
    [Amount] decimal(18,6) NOT NULL,
    [Hours] decimal(18,6) NOT NULL,
    [Approved] bit NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_HrAdjustments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HrAdjustments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [LeaveBalances] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [LeaveTypeId] int NOT NULL,
    [Year] int NOT NULL,
    [EntitledDays] decimal(18,6) NOT NULL,
    [UsedDays] decimal(18,6) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_LeaveBalances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveBalances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LeaveBalances_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LeaveRequests] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [LeaveTypeId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Days] decimal(18,6) NOT NULL,
    [Status] int NOT NULL,
    [Reason] nvarchar(max) NULL,
    [DecisionReason] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LeaveRequests_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PurchaseOrderLines] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseOrderId] int NOT NULL,
    [PurchaseRequestLineId] int NULL,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NOT NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL,
    [BaseQuantity] decimal(18,6) NOT NULL,
    [UnitPrice] decimal(18,6) NOT NULL,
    [UnitCost] decimal(18,6) NOT NULL,
    [LineDiscountAmount] decimal(18,6) NOT NULL,
    [FreeQuantity] decimal(18,6) NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [TaxExempt] bit NOT NULL,
    [TaxRate] decimal(18,6) NOT NULL,
    [TaxAmount] decimal(18,6) NOT NULL,
    [LineSubTotal] decimal(18,6) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseOrderLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ProductUnit] (
    [Id] int NOT NULL IDENTITY,
    [AlternateBarcode] nvarchar(450) NULL,
    [SalePrice] decimal(18,6) NOT NULL,
    [UnTaxedPrice] decimal(18,6) NOT NULL,
    [PurchasePrice] decimal(18,6) NOT NULL,
    [QuantityPerUnit] decimal(18,6) NOT NULL,
    [AllowFractionalSale] bit NOT NULL,
    [FractionalPartsPerUnit] decimal(18,6) NOT NULL,
    [FractionalPartName] nvarchar(max) NULL,
    [FractionalSalePrice] decimal(18,6) NULL,
    [FractionalSaleCost] decimal(18,6) NULL,
    [IsBaseUnit] bit NOT NULL,
    [IsDefaultSaleUnit] bit NOT NULL,
    [IsDefaultPurchaseUnit] bit NOT NULL,
    [ProductId] int NOT NULL,
    [UnitId] int NOT NULL,
    [UnitId1] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_ProductUnit] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductUnit_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]),
    CONSTRAINT [FK_ProductUnit_Unit_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Unit] ([Id]),
    CONSTRAINT [FK_ProductUnit_Unit_UnitId1] FOREIGN KEY ([UnitId1]) REFERENCES [Unit] ([Id])
);
GO


CREATE TABLE [JournalEntryLines] (
    [Id] int NOT NULL IDENTITY,
    [JournalEntryId] int NOT NULL,
    [AccountId] int NOT NULL,
    [LineNumber] int NOT NULL,
    [Debit] decimal(18,6) NOT NULL,
    [Credit] decimal(18,6) NOT NULL,
    [Description] nvarchar(max) NULL,
    [PartyUserId] int NULL,
    [CustomerId] int NULL,
    [SupplierId] int NULL,
    [CashierId] int NULL,
    [WarehouseId] int NULL,
    [CostCenterId] int NULL,
    [TaxRateId] int NULL,
    [TaxAmount] decimal(18,6) NULL,
    [CurrencyId] int NULL,
    [ForeignAmount] decimal(18,6) NULL,
    [ExchangeRate] decimal(18,6) NULL,
    [BranchId] int NULL,
    [InvoiceId] int NULL,
    [VoucherId] int NULL,
    [StockDocumentId] int NULL,
    [FinancialTransactionId] int NULL,
    [ReferenceType] nvarchar(max) NULL,
    [ReferenceId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_JournalEntryLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JournalEntryLines_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]),
    CONSTRAINT [FK_JournalEntryLines_JournalEntries_JournalEntryId] FOREIGN KEY ([JournalEntryId]) REFERENCES [JournalEntries] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Invoice] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceNumber] nvarchar(max) NOT NULL,
    [FalconInvoiceNumber] nvarchar(max) NULL,
    [OriginalInvoiceId] nvarchar(max) NULL,
    [InvoiceType] int NOT NULL,
    [PaymentType] int NULL,
    [CasherId] int NULL,
    [SupplierId] int NULL,
    [CustomerId] int NULL,
    [IsInsuranceSale] bit NOT NULL,
    [InsuranceMemberPolicyId] int NULL,
    [InsuranceCompanyId] int NULL,
    [InsuranceContractId] int NULL,
    [InsuranceContractVersionId] int NULL,
    [PatientResponsibility] decimal(18,6) NOT NULL,
    [InsuranceResponsibility] decimal(18,6) NOT NULL,
    [UserId] int NULL,
    [DelegateId] int NULL,
    [VoucherId] int NULL,
    [BranchId] int NULL,
    [WarehouseId] int NULL,
    [CurrencyId] int NULL,
    [ExchangeRate] decimal(18,6) NOT NULL,
    [DocumentDate] datetime2 NULL,
    [DueDate] datetime2 NULL,
    [ReferenceNumber] nvarchar(450) NULL,
    [PostingStatus] int NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [TotalAmount] decimal(18,6) NOT NULL,
    [Notes] nvarchar(max) NULL,
    [CashierSessionId] int NULL,
    [Status] int NULL,
    [IsPOS] bit NULL,
    [OpenedAt] datetime2 NULL,
    [ClosedAt] datetime2 NULL,
    [HeldColor] nvarchar(max) NULL,
    [DiscountAmount] decimal(18,6) NULL,
    [SubTotal] decimal(18,6) NOT NULL,
    [TotalTax] decimal(18,6) NOT NULL,
    [TotalCOGS] decimal(18,6) NOT NULL,
    [GrossProfit] decimal(18,6) NOT NULL,
    [NetSales] decimal(18,6) NOT NULL,
    [JordanEInvoiceStatus] int NOT NULL,
    [JordanEInvoiceReference] nvarchar(max) NULL,
    [JordanEInvoiceUuid] nvarchar(max) NULL,
    [JordanEInvoiceQr] nvarchar(max) NULL,
    [JordanEInvoiceResponse] nvarchar(max) NULL,
    [JordanEInvoiceSubmittedAtUtc] datetime2 NULL,
    [JordanEInvoiceLastAttemptAtUtc] datetime2 NULL,
    [JordanEInvoiceAttemptCount] int NOT NULL,
    [SupplierJoFotaraNumber] nvarchar(max) NULL,
    [SupplierJoFotaraUuid] nvarchar(max) NULL,
    [SupplierJoFotaraQr] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Invoice] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Invoice_CashierSession_CashierSessionId] FOREIGN KEY ([CashierSessionId]) REFERENCES [CashierSession] ([Id]),
    CONSTRAINT [FK_Invoice_Delegate_DelegateId] FOREIGN KEY ([DelegateId]) REFERENCES [Delegate] ([Id]),
    CONSTRAINT [FK_Invoice_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_Invoice_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id])
);
GO


CREATE TABLE [EmployeePayrollLines] (
    [Id] int NOT NULL IDENTITY,
    [EmployeePayrollId] int NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Amount] decimal(18,6) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_EmployeePayrollLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeePayrollLines_EmployeePayrolls_EmployeePayrollId] FOREIGN KEY ([EmployeePayrollId]) REFERENCES [EmployeePayrolls] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AttendanceRecords] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [ShiftAssignmentId] int NULL,
    [WorkDate] datetime2 NOT NULL,
    [CheckIn] datetime2 NULL,
    [CheckOut] datetime2 NULL,
    [WorkedHours] decimal(18,6) NOT NULL,
    [LateMinutes] int NOT NULL,
    [EarlyLeaveMinutes] int NOT NULL,
    [Status] int NOT NULL,
    [CorrectionReason] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendanceRecords_EmployeeShiftAssignments_ShiftAssignmentId] FOREIGN KEY ([ShiftAssignmentId]) REFERENCES [EmployeeShiftAssignments] ([Id]),
    CONSTRAINT [FK_AttendanceRecords_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Stock] (
    [Id] int NOT NULL IDENTITY,
    [WarehouseId] int NULL,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NOT NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [PurchasePrice] decimal(18,6) NOT NULL,
    [SalePrice] decimal(18,6) NOT NULL,
    [LastMovementDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Stock] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Stock_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Stock_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [StockItem] (
    [Id] int NOT NULL IDENTITY,
    [StockId] int NOT NULL,
    [LineNumber] int NOT NULL,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NOT NULL,
    [StockLotId] int NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL,
    [BaseQuantity] decimal(18,6) NOT NULL,
    [PurchasePrice] decimal(18,6) NOT NULL,
    [SalePrice] decimal(18,6) NOT NULL,
    [LineDiscountAmount] decimal(18,6) NOT NULL,
    [FreeQuantity] decimal(18,6) NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_StockItem] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockItem_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockItem_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockItem_StockDocument_StockId] FOREIGN KEY ([StockId]) REFERENCES [StockDocument] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [StockLot] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NOT NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [RemainingQuantity] decimal(18,6) NOT NULL,
    [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL,
    [BaseQuantity] decimal(18,6) NOT NULL,
    [RemainingBaseQuantity] decimal(18,6) NOT NULL,
    [PurchasePrice] decimal(18,6) NOT NULL,
    [PurchaseAcquisitionCost] decimal(18,6) NULL,
    [SourcePurchaseLineId] int NULL,
    [SalePrice] decimal(18,6) NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [ClosedDate] datetime2 NULL,
    [ClosedReason] nvarchar(max) NULL,
    [ClosedByUserId] int NULL,
    [ReplacesStockLotId] int NULL,
    [ReplacedByStockLotId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_StockLot] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockLot_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockLot_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockLot_StockLot_ReplacesStockLotId] FOREIGN KEY ([ReplacesStockLotId]) REFERENCES [StockLot] ([Id])
);
GO


CREATE TABLE [BankStatementLines] (
    [Id] int NOT NULL IDENTITY,
    [BankStatementId] int NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Amount] decimal(18,6) NOT NULL,
    [Reference] nvarchar(max) NULL,
    [IsReconciled] bit NOT NULL,
    [MatchedJournalEntryLineId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_BankStatementLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankStatementLines_BankStatements_BankStatementId] FOREIGN KEY ([BankStatementId]) REFERENCES [BankStatements] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BankStatementLines_JournalEntryLines_MatchedJournalEntryLineId] FOREIGN KEY ([MatchedJournalEntryLineId]) REFERENCES [JournalEntryLines] ([Id])
);
GO


CREATE TABLE [Check] (
    [Id] int NOT NULL IDENTITY,
    [CheckNumber] nvarchar(max) NOT NULL,
    [BankName] nvarchar(max) NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [Amount] decimal(18,6) NOT NULL,
    [Status] int NOT NULL,
    [Notes] nvarchar(max) NULL,
    [VoucherId] int NULL,
    [InvoiceId] int NULL,
    [StockDocumentId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Check] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Check_Invoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoice] ([Id]),
    CONSTRAINT [FK_Check_StockDocument_StockDocumentId] FOREIGN KEY ([StockDocumentId]) REFERENCES [StockDocument] ([Id]),
    CONSTRAINT [FK_Check_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id])
);
GO


CREATE TABLE [InvoiceLine] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [OriginalInvoiceId] nvarchar(max) NULL,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NOT NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [LineDiscountAmount] decimal(18,6) NOT NULL,
    [FreeQuantity] decimal(18,6) NOT NULL,
    [BonusRatio] nvarchar(40) NULL,
    [BonusPercentage] decimal(18,6) NULL,
    [PurchaseAcquisitionCost] decimal(18,6) NULL,
    [SourcePurchaseLineId] int NULL,
    [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL,
    [BaseQuantity] decimal(18,6) NOT NULL,
    [UnitPrice] decimal(18,6) NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [UnitCost] decimal(18,6) NOT NULL,
    [TaxExempt] bit NOT NULL,
    [TaxRate] decimal(18,6) NOT NULL,
    [TaxAmount] decimal(18,6) NOT NULL,
    [LineSubTotal] decimal(18,6) NOT NULL,
    [Profit] decimal(18,6) NOT NULL,
    [ProfitBeforeTax] decimal(18,6) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_InvoiceLine] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoiceLine_Invoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoice] ([Id]),
    CONSTRAINT [FK_InvoiceLine_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]),
    CONSTRAINT [FK_InvoiceLine_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id])
);
GO


CREATE TABLE [InvoicePayments] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [PaymentType] int NOT NULL,
    [Amount] decimal(18,6) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_InvoicePayments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoicePayments_Invoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoice] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [StockAdjustment] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NOT NULL,
    [StockLotId] int NOT NULL,
    [NewStockLotId] int NULL,
    [AdjustmentType] int NOT NULL,
    [QuantityDelta] decimal(18,6) NOT NULL,
    [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL,
    [BaseQuantityDelta] decimal(18,6) NOT NULL,
    [PurchasePrice] decimal(18,6) NULL,
    [SalePrice] decimal(18,6) NULL,
    [ExpiryDate] datetime2 NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Reference] nvarchar(max) NULL,
    [AdjustmentDate] datetime2 NOT NULL,
    [UserId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_StockAdjustment] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockAdjustment_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockAdjustment_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockAdjustment_StockLot_NewStockLotId] FOREIGN KEY ([NewStockLotId]) REFERENCES [StockLot] ([Id]),
    CONSTRAINT [FK_StockAdjustment_StockLot_StockLotId] FOREIGN KEY ([StockLotId]) REFERENCES [StockLot] ([Id]),
    CONSTRAINT [FK_StockAdjustment_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id])
);
GO


CREATE TABLE [StockTransaction] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [ProductUnitId] int NOT NULL,
    [StockId] int NULL,
    [WarehouseId] int NULL,
    [StockLotId] int NULL,
    [StockAdjustmentId] int NULL,
    [StockDocumentId] int NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [QuantityPerUnitSnapshot] decimal(18,6) NOT NULL,
    [BaseQuantity] decimal(18,6) NOT NULL,
    [UnitPrice] decimal(18,6) NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [TransactionType] int NOT NULL,
    [InvoiceId] int NULL,
    [VoucherId] int NULL,
    [CasherId] int NULL,
    [CashierSessionId] int NULL,
    [CustomerId] int NULL,
    [BranchId] int NULL,
    [TransactionDate] datetime2 NOT NULL,
    [Notes] nvarchar(max) NULL,
    [ReferenceNumber] nvarchar(max) NULL,
    [SourceType] int NULL,
    [SourceId] int NULL,
    [CreatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_StockTransaction] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTransaction_CashierSession_CashierSessionId] FOREIGN KEY ([CashierSessionId]) REFERENCES [CashierSession] ([Id]),
    CONSTRAINT [FK_StockTransaction_Invoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoice] ([Id]),
    CONSTRAINT [FK_StockTransaction_ProductUnit_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnit] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockTransaction_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockTransaction_StockAdjustment_StockAdjustmentId] FOREIGN KEY ([StockAdjustmentId]) REFERENCES [StockAdjustment] ([Id]),
    CONSTRAINT [FK_StockTransaction_StockLot_StockLotId] FOREIGN KEY ([StockLotId]) REFERENCES [StockLot] ([Id]),
    CONSTRAINT [FK_StockTransaction_Stock_StockId] FOREIGN KEY ([StockId]) REFERENCES [Stock] ([Id]),
    CONSTRAINT [FK_StockTransaction_User_CasherId] FOREIGN KEY ([CasherId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_StockTransaction_User_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_StockTransaction_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id])
);
GO


CREATE UNIQUE INDEX [IX_AccountingOperations_ReferenceType_ReferenceId_OperationType] ON [AccountingOperations] ([ReferenceType], [ReferenceId], [OperationType]);
GO


CREATE INDEX [IX_AccountingOperations_Status_NextAttemptDate] ON [AccountingOperations] ([Status], [NextAttemptDate]);
GO


CREATE UNIQUE INDEX [IX_AccountingPeriods_FiscalYearId_PeriodNumber] ON [AccountingPeriods] ([FiscalYearId], [PeriodNumber]);
GO


CREATE INDEX [IX_AccountOpeningBalances_AccountId] ON [AccountOpeningBalances] ([AccountId]);
GO


CREATE INDEX [IX_AccountOpeningBalances_FiscalYearId_AccountId_BranchId_CostCenterId_WarehouseId_PartyUserId] ON [AccountOpeningBalances] ([FiscalYearId], [AccountId], [BranchId], [CostCenterId], [WarehouseId], [PartyUserId]);
GO


CREATE UNIQUE INDEX [IX_Accounts_Code] ON [Accounts] ([Code]);
GO


CREATE INDEX [IX_Accounts_ParentAccountId] ON [Accounts] ([ParentAccountId]);
GO


IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_AppSettings_Key' AND [object_id] = OBJECT_ID(N'[AppSettings]'))
CREATE UNIQUE INDEX [IX_AppSettings_Key] ON [AppSettings] ([Key]);
GO


CREATE UNIQUE INDEX [IX_AttendanceRecords_EmployeeId_WorkDate] ON [AttendanceRecords] ([EmployeeId], [WorkDate]);
GO


CREATE INDEX [IX_AttendanceRecords_ShiftAssignmentId] ON [AttendanceRecords] ([ShiftAssignmentId]);
GO


CREATE INDEX [IX_AuditLogs_CreatedDate_UserId] ON [AuditLogs] ([CreatedDate], [UserId]);
GO


CREATE UNIQUE INDEX [IX_BankAccounts_AccountNumber] ON [BankAccounts] ([AccountNumber]);
GO


CREATE INDEX [IX_BankAccounts_CurrencyId] ON [BankAccounts] ([CurrencyId]);
GO


CREATE INDEX [IX_BankAccounts_GlAccountId] ON [BankAccounts] ([GlAccountId]);
GO


CREATE INDEX [IX_BankStatementLines_BankStatementId_TransactionDate] ON [BankStatementLines] ([BankStatementId], [TransactionDate]);
GO


CREATE INDEX [IX_BankStatementLines_MatchedJournalEntryLineId] ON [BankStatementLines] ([MatchedJournalEntryLineId]);
GO


CREATE INDEX [IX_BankStatements_BankAccountId_StatementDate] ON [BankStatements] ([BankAccountId], [StatementDate]);
GO


CREATE UNIQUE INDEX [IX_Branches_Code] ON [Branches] ([Code]);
GO


CREATE INDEX [IX_CashierSession_CashierId] ON [CashierSession] ([CashierId]);
GO


CREATE INDEX [IX_Check_InvoiceId] ON [Check] ([InvoiceId]);
GO


CREATE INDEX [IX_Check_StockDocumentId] ON [Check] ([StockDocumentId]);
GO


CREATE INDEX [IX_Check_VoucherId] ON [Check] ([VoucherId]);
GO


CREATE UNIQUE INDEX [IX_CostCenters_Code] ON [CostCenters] ([Code]);
GO


CREATE INDEX [IX_CostCenters_ParentCostCenterId] ON [CostCenters] ([ParentCostCenterId]);
GO


CREATE UNIQUE INDEX [IX_Currencies_Code] ON [Currencies] ([Code]);
GO


CREATE INDEX [IX_CustomerRequestLines_CustomerRequestId] ON [CustomerRequestLines] ([CustomerRequestId]);
GO


CREATE UNIQUE INDEX [IX_Delegate_Code] ON [Delegate] ([Code]);
GO


CREATE UNIQUE INDEX [IX_Delegate_UserId] ON [Delegate] ([UserId]) WHERE [UserId] IS NOT NULL;
GO


CREATE INDEX [IX_EmployeeBranchAssignments_BranchId] ON [EmployeeBranchAssignments] ([BranchId]);
GO


CREATE INDEX [IX_EmployeeBranchAssignments_EmployeeId_BranchId_StartDate] ON [EmployeeBranchAssignments] ([EmployeeId], [BranchId], [StartDate]);
GO


CREATE INDEX [IX_EmployeePayrollLines_EmployeePayrollId] ON [EmployeePayrollLines] ([EmployeePayrollId]);
GO


CREATE INDEX [IX_EmployeePayrolls_EmployeeId] ON [EmployeePayrolls] ([EmployeeId]);
GO


CREATE UNIQUE INDEX [IX_EmployeePayrolls_PayrollPeriodId_EmployeeId] ON [EmployeePayrolls] ([PayrollPeriodId], [EmployeeId]);
GO


CREATE INDEX [IX_Employees_FullName] ON [Employees] ([FullName]);
GO


CREATE UNIQUE INDEX [IX_Employees_NationalId] ON [Employees] ([NationalId]) WHERE [NationalId] IS NOT NULL;
GO


CREATE INDEX [IX_Employees_PhoneNumber] ON [Employees] ([PhoneNumber]);
GO


CREATE INDEX [IX_Employees_PrimaryBranchId] ON [Employees] ([PrimaryBranchId]);
GO


CREATE UNIQUE INDEX [IX_Employees_UserId] ON [Employees] ([UserId]) WHERE [UserId] IS NOT NULL;
GO


CREATE INDEX [IX_EmployeeShiftAssignments_EmployeeId_DayOfWeek_StartDate] ON [EmployeeShiftAssignments] ([EmployeeId], [DayOfWeek], [StartDate]);
GO


CREATE INDEX [IX_EmployeeShiftAssignments_ShiftTemplateId] ON [EmployeeShiftAssignments] ([ShiftTemplateId]);
GO


CREATE INDEX [IX_ExchangeRates_FromCurrencyId_ToCurrencyId_EffectiveDate] ON [ExchangeRates] ([FromCurrencyId], [ToCurrencyId], [EffectiveDate]);
GO


CREATE INDEX [IX_ExchangeRates_ToCurrencyId] ON [ExchangeRates] ([ToCurrencyId]);
GO


CREATE INDEX [IX_FinancialTransaction_CasherId] ON [FinancialTransaction] ([CasherId]);
GO


CREATE INDEX [IX_FinancialTransaction_CashierSessionId] ON [FinancialTransaction] ([CashierSessionId]);
GO


CREATE INDEX [IX_FinancialTransaction_SourceType_SourceId] ON [FinancialTransaction] ([SourceType], [SourceId]);
GO


CREATE UNIQUE INDEX [IX_FiscalYears_Code] ON [FiscalYears] ([Code]);
GO


CREATE INDEX [IX_GoodsReceiptLines_GoodsReceiptId_PurchaseOrderLineId] ON [GoodsReceiptLines] ([GoodsReceiptId], [PurchaseOrderLineId]);
GO


CREATE UNIQUE INDEX [IX_GoodsReceipts_ReceiptNumber] ON [GoodsReceipts] ([ReceiptNumber]);
GO


CREATE INDEX [IX_HrAdjustments_EmployeeId_PeriodMonth_Type] ON [HrAdjustments] ([EmployeeId], [PeriodMonth], [Type]);
GO


CREATE INDEX [IX_InsuranceAttachments_OwnerType_OwnerId] ON [InsuranceAttachments] ([OwnerType], [OwnerId]);
GO


CREATE UNIQUE INDEX [IX_InsuranceBeneficiaryCategories_Code] ON [InsuranceBeneficiaryCategories] ([Code]);
GO


CREATE UNIQUE INDEX [IX_InsuranceClaimLines_InsuranceClaimId_InvoiceLineId] ON [InsuranceClaimLines] ([InsuranceClaimId], [InvoiceLineId]);
GO


CREATE UNIQUE INDEX [IX_InsuranceClaimPaymentAllocations_InsuranceClaimPaymentId_InsuranceClaimId] ON [InsuranceClaimPaymentAllocations] ([InsuranceClaimPaymentId], [InsuranceClaimId]);
GO


CREATE UNIQUE INDEX [IX_InsuranceClaims_ClaimNumber] ON [InsuranceClaims] ([ClaimNumber]);
GO


CREATE INDEX [IX_InsuranceClaims_InsuranceCompanyId_Status_SubmissionDate] ON [InsuranceClaims] ([InsuranceCompanyId], [Status], [SubmissionDate]);
GO


CREATE UNIQUE INDEX [IX_InsuranceCompanies_Code] ON [InsuranceCompanies] ([Code]);
GO


CREATE UNIQUE INDEX [IX_InsuranceContracts_ContractNumber] ON [InsuranceContracts] ([ContractNumber]);
GO


CREATE UNIQUE INDEX [IX_InsuranceContractVersions_InsuranceContractId_VersionNumber] ON [InsuranceContractVersions] ([InsuranceContractId], [VersionNumber]);
GO


CREATE INDEX [IX_InsuranceEligibilityChecks_InsuranceMemberPolicyId_CheckedAt] ON [InsuranceEligibilityChecks] ([InsuranceMemberPolicyId], [CheckedAt]);
GO


CREATE UNIQUE INDEX [IX_InsuranceInvoiceCoverageLines_InsuranceInvoiceCoverageId_InvoiceLineId] ON [InsuranceInvoiceCoverageLines] ([InsuranceInvoiceCoverageId], [InvoiceLineId]);
GO


CREATE UNIQUE INDEX [IX_InsuranceInvoiceCoverages_InvoiceId] ON [InsuranceInvoiceCoverages] ([InvoiceId]);
GO


CREATE INDEX [IX_InsuranceMemberPolicies_InsuranceCompanyId_MemberNumber] ON [InsuranceMemberPolicies] ([InsuranceCompanyId], [MemberNumber]);
GO


CREATE UNIQUE INDEX [IX_InsuranceNetworks_Code] ON [InsuranceNetworks] ([Code]);
GO


CREATE UNIQUE INDEX [IX_InsurancePlans_InsuranceContractId_Code] ON [InsurancePlans] ([InsuranceContractId], [Code]);
GO


CREATE UNIQUE INDEX [IX_InsurancePreAuthorizationLines_InsurancePreAuthorizationId_ProductId] ON [InsurancePreAuthorizationLines] ([InsurancePreAuthorizationId], [ProductId]);
GO


CREATE UNIQUE INDEX [IX_InsurancePreAuthorizations_AuthorizationNumber] ON [InsurancePreAuthorizations] ([AuthorizationNumber]);
GO


CREATE UNIQUE INDEX [IX_IntegrationInbox_EventId] ON [IntegrationInbox] ([EventId]);
GO


CREATE UNIQUE INDEX [IX_IntegrationInbox_SourceSystem_ExternalOrderId] ON [IntegrationInbox] ([SourceSystem], [ExternalOrderId]);
GO


CREATE INDEX [IX_Invoice_CashierSessionId] ON [Invoice] ([CashierSessionId]);
GO


CREATE INDEX [IX_Invoice_DelegateId] ON [Invoice] ([DelegateId]);
GO


CREATE INDEX [IX_Invoice_DueDate] ON [Invoice] ([DueDate]);
GO


CREATE INDEX [IX_Invoice_ReferenceNumber] ON [Invoice] ([ReferenceNumber]);
GO


CREATE INDEX [IX_Invoice_UserId] ON [Invoice] ([UserId]);
GO


CREATE INDEX [IX_Invoice_VoucherId] ON [Invoice] ([VoucherId]);
GO


CREATE INDEX [IX_InvoiceLine_InvoiceId] ON [InvoiceLine] ([InvoiceId]);
GO


CREATE INDEX [IX_InvoiceLine_ProductId] ON [InvoiceLine] ([ProductId]);
GO


CREATE INDEX [IX_InvoiceLine_ProductUnitId] ON [InvoiceLine] ([ProductUnitId]);
GO


CREATE INDEX [IX_InvoiceLine_SourcePurchaseLineId] ON [InvoiceLine] ([SourcePurchaseLineId]);
GO


CREATE INDEX [IX_InvoicePayments_InvoiceId] ON [InvoicePayments] ([InvoiceId]);
GO


CREATE INDEX [IX_JournalEntries_AccountingPeriodId] ON [JournalEntries] ([AccountingPeriodId]);
GO


CREATE UNIQUE INDEX [IX_JournalEntries_EntryNumber] ON [JournalEntries] ([EntryNumber]);
GO


CREATE INDEX [IX_JournalEntries_FiscalYearId] ON [JournalEntries] ([FiscalYearId]);
GO


CREATE INDEX [IX_JournalEntryLines_AccountId] ON [JournalEntryLines] ([AccountId]);
GO


CREATE UNIQUE INDEX [IX_JournalEntryLines_JournalEntryId_LineNumber] ON [JournalEntryLines] ([JournalEntryId], [LineNumber]);
GO


CREATE INDEX [IX_JournalEntryLines_TaxRateId] ON [JournalEntryLines] ([TaxRateId]);
GO


CREATE UNIQUE INDEX [IX_LeaveBalances_EmployeeId_LeaveTypeId_Year] ON [LeaveBalances] ([EmployeeId], [LeaveTypeId], [Year]);
GO


CREATE INDEX [IX_LeaveBalances_LeaveTypeId] ON [LeaveBalances] ([LeaveTypeId]);
GO


CREATE INDEX [IX_LeaveRequests_EmployeeId_StartDate_EndDate] ON [LeaveRequests] ([EmployeeId], [StartDate], [EndDate]);
GO


CREATE INDEX [IX_LeaveRequests_LeaveTypeId] ON [LeaveRequests] ([LeaveTypeId]);
GO


CREATE UNIQUE INDEX [IX_PayrollPeriods_PeriodMonth] ON [PayrollPeriods] ([PeriodMonth]);
GO


CREATE UNIQUE INDEX [IX_PermissionGroupPermissions_PermissionGroupId_AreaKey] ON [PermissionGroupPermissions] ([PermissionGroupId], [AreaKey]);
GO


CREATE UNIQUE INDEX [IX_PermissionGroups_Name] ON [PermissionGroups] ([Name]);
GO


CREATE INDEX [IX_PosProductCatalogEntry_AlternateBarcode] ON [PosProductCatalogEntry] ([AlternateBarcode]);
GO


CREATE INDEX [IX_PosProductCatalogEntry_AvailableToSale] ON [PosProductCatalogEntry] ([AvailableToSale]);
GO


CREATE UNIQUE INDEX [IX_PosProductCatalogEntry_ProductId_ProductUnitId] ON [PosProductCatalogEntry] ([ProductId], [ProductUnitId]);
GO


CREATE INDEX [IX_PosProductCatalogEntry_ProductName] ON [PosProductCatalogEntry] ([ProductName]);
GO


CREATE INDEX [IX_Product_BrandId] ON [Product] ([BrandId]);
GO


CREATE INDEX [IX_Product_ITEMCODE] ON [Product] ([ITEMCODE]);
GO


CREATE INDEX [IX_Product_Name] ON [Product] ([Name]);
GO


CREATE INDEX [IX_Product_SubCategoryId] ON [Product] ([SubCategoryId]);
GO


CREATE INDEX [IX_Product_SupplierId] ON [Product] ([SupplierId]);
GO


CREATE INDEX [IX_ProductUnit_AlternateBarcode] ON [ProductUnit] ([AlternateBarcode]);
GO


CREATE INDEX [IX_ProductUnit_ProductId_AlternateBarcode] ON [ProductUnit] ([ProductId], [AlternateBarcode]);
GO


CREATE INDEX [IX_ProductUnit_UnitId] ON [ProductUnit] ([UnitId]);
GO


CREATE INDEX [IX_ProductUnit_UnitId1] ON [ProductUnit] ([UnitId1]);
GO


CREATE UNIQUE INDEX [IX_PurchaseInvoiceMatches_InvoiceId] ON [PurchaseInvoiceMatches] ([InvoiceId]);
GO


CREATE UNIQUE INDEX [IX_PurchaseInvoiceMatchLines_PurchaseInvoiceMatchId_InvoiceLineId] ON [PurchaseInvoiceMatchLines] ([PurchaseInvoiceMatchId], [InvoiceLineId]);
GO


CREATE INDEX [IX_PurchaseOrderLines_PurchaseOrderId] ON [PurchaseOrderLines] ([PurchaseOrderId]);
GO


CREATE INDEX [IX_PurchaseOrderLines_PurchaseRequestLineId] ON [PurchaseOrderLines] ([PurchaseRequestLineId]);
GO


CREATE UNIQUE INDEX [IX_PurchaseOrders_OrderNumber] ON [PurchaseOrders] ([OrderNumber]);
GO


CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_PurchaseRequestLines_PurchaseRequestId_ProductId_ProductUnitId] ON [PurchaseRequestLines] ([PurchaseRequestId], [ProductId], [ProductUnitId]) WHERE [ProductId] IS NOT NULL AND [ProductUnitId] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_PurchaseRequests_RequestNumber] ON [PurchaseRequests] ([RequestNumber]);
GO


CREATE INDEX [IX_PurchaseReturnLines_PurchaseReturnId] ON [PurchaseReturnLines] ([PurchaseReturnId]);
GO


CREATE UNIQUE INDEX [IX_PurchaseReturns_ReturnNumber] ON [PurchaseReturns] ([ReturnNumber]);
GO


CREATE INDEX [IX_RecurringJournalLines_AccountId] ON [RecurringJournalLines] ([AccountId]);
GO


CREATE INDEX [IX_RecurringJournalLines_CostCenterId] ON [RecurringJournalLines] ([CostCenterId]);
GO


CREATE INDEX [IX_RecurringJournalLines_RecurringJournalId] ON [RecurringJournalLines] ([RecurringJournalId]);
GO


CREATE INDEX [IX_RecurringJournals_NextRunDate] ON [RecurringJournals] ([NextRunDate]);
GO


CREATE UNIQUE INDEX [IX_ShiftTemplates_Name] ON [ShiftTemplates] ([Name]);
GO


CREATE UNIQUE INDEX [IX_ShiftTemplateWorkingDays_ShiftTemplateId_DayOfWeek] ON [ShiftTemplateWorkingDays] ([ShiftTemplateId], [DayOfWeek]);
GO


CREATE INDEX [IX_SocialSecurityRules_EffectiveFrom_EffectiveTo] ON [SocialSecurityRules] ([EffectiveFrom], [EffectiveTo]);
GO


CREATE INDEX [IX_Stock_ProductId] ON [Stock] ([ProductId]);
GO


CREATE INDEX [IX_Stock_ProductUnitId] ON [Stock] ([ProductUnitId]);
GO


CREATE INDEX [IX_Stock_WarehouseId_ProductId_ProductUnitId] ON [Stock] ([WarehouseId], [ProductId], [ProductUnitId]);
GO


CREATE INDEX [IX_StockAdjustment_NewStockLotId] ON [StockAdjustment] ([NewStockLotId]);
GO


CREATE INDEX [IX_StockAdjustment_ProductId] ON [StockAdjustment] ([ProductId]);
GO


CREATE INDEX [IX_StockAdjustment_ProductUnitId] ON [StockAdjustment] ([ProductUnitId]);
GO


CREATE INDEX [IX_StockAdjustment_StockLotId] ON [StockAdjustment] ([StockLotId]);
GO


CREATE INDEX [IX_StockAdjustment_UserId] ON [StockAdjustment] ([UserId]);
GO


CREATE INDEX [IX_StockDocument_CustomerId] ON [StockDocument] ([CustomerId]);
GO


CREATE UNIQUE INDEX [IX_StockDocument_DocumentNumber] ON [StockDocument] ([DocumentNumber]);
GO


CREATE INDEX [IX_StockDocument_SupplierId] ON [StockDocument] ([SupplierId]);
GO


CREATE INDEX [IX_StockItem_ProductId] ON [StockItem] ([ProductId]);
GO


CREATE INDEX [IX_StockItem_ProductUnitId] ON [StockItem] ([ProductUnitId]);
GO


CREATE INDEX [IX_StockItem_StockId_LineNumber] ON [StockItem] ([StockId], [LineNumber]);
GO


CREATE INDEX [IX_StockLot_ExpiryDate_RemainingQuantity] ON [StockLot] ([ExpiryDate], [RemainingQuantity]);
GO


CREATE INDEX [IX_StockLot_ProductId_Status_RemainingQuantity] ON [StockLot] ([ProductId], [Status], [RemainingQuantity]);
GO


CREATE INDEX [IX_StockLot_ProductUnitId] ON [StockLot] ([ProductUnitId]);
GO


CREATE UNIQUE INDEX [IX_StockLot_ReplacesStockLotId] ON [StockLot] ([ReplacesStockLotId]) WHERE [ReplacesStockLotId] IS NOT NULL;
GO


CREATE INDEX [IX_StockLot_SourcePurchaseLineId] ON [StockLot] ([SourcePurchaseLineId]);
GO


CREATE INDEX [IX_StockTransaction_CasherId] ON [StockTransaction] ([CasherId]);
GO


CREATE INDEX [IX_StockTransaction_CashierSessionId] ON [StockTransaction] ([CashierSessionId]);
GO


CREATE INDEX [IX_StockTransaction_CustomerId] ON [StockTransaction] ([CustomerId]);
GO


CREATE INDEX [IX_StockTransaction_InvoiceId] ON [StockTransaction] ([InvoiceId]);
GO


CREATE INDEX [IX_StockTransaction_ProductId] ON [StockTransaction] ([ProductId]);
GO


CREATE INDEX [IX_StockTransaction_ProductUnitId] ON [StockTransaction] ([ProductUnitId]);
GO


CREATE INDEX [IX_StockTransaction_SourceType_SourceId] ON [StockTransaction] ([SourceType], [SourceId]);
GO


CREATE INDEX [IX_StockTransaction_StockAdjustmentId] ON [StockTransaction] ([StockAdjustmentId]);
GO


CREATE INDEX [IX_StockTransaction_StockDocumentId] ON [StockTransaction] ([StockDocumentId]);
GO


CREATE INDEX [IX_StockTransaction_StockId] ON [StockTransaction] ([StockId]);
GO


CREATE INDEX [IX_StockTransaction_StockLotId] ON [StockTransaction] ([StockLotId]);
GO


CREATE INDEX [IX_StockTransaction_VoucherId] ON [StockTransaction] ([VoucherId]);
GO


CREATE INDEX [IX_SubCategory_ParentCategoryId] ON [SubCategory] ([ParentCategoryId]);
GO


CREATE INDEX [IX_SubCategoryBrand_BrandId] ON [SubCategoryBrand] ([BrandId]);
GO


CREATE INDEX [IX_SubCategoryBrand_SubCategoryId] ON [SubCategoryBrand] ([SubCategoryId]);
GO


CREATE UNIQUE INDEX [IX_SupplierAdvances_VoucherId] ON [SupplierAdvances] ([VoucherId]);
GO


CREATE INDEX [IX_SupplierPaymentAllocations_InvoiceId] ON [SupplierPaymentAllocations] ([InvoiceId]);
GO


CREATE UNIQUE INDEX [IX_SupplierPaymentAllocations_VoucherId_InvoiceId] ON [SupplierPaymentAllocations] ([VoucherId], [InvoiceId]);
GO


CREATE INDEX [IX_TaxRates_Name_TaxType] ON [TaxRates] ([Name], [TaxType]);
GO


CREATE INDEX [IX_TaxRates_TaxAccountId] ON [TaxRates] ([TaxAccountId]);
GO


CREATE UNIQUE INDEX [IX_User_UserNumber] ON [User] ([UserNumber]);
GO


CREATE INDEX [IX_UserPermissionGroups_PermissionGroupId] ON [UserPermissionGroups] ([PermissionGroupId]);
GO


CREATE UNIQUE INDEX [IX_UserPermissionGroups_UserId_PermissionGroupId] ON [UserPermissionGroups] ([UserId], [PermissionGroupId]);
GO


CREATE INDEX [IX_Voucher_CashierSessionId] ON [Voucher] ([CashierSessionId]);
GO


CREATE INDEX [IX_Voucher_ReferenceNumber] ON [Voucher] ([ReferenceNumber]);
GO


