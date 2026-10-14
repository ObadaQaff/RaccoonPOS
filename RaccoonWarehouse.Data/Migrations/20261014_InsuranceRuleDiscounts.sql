/* Insurance rule discounts and per-line audit snapshot.
   Safe to run more than once. No data is deleted or rewritten. */
IF OBJECT_ID(N'dbo.InsuranceCoverageRules', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.InsuranceCoverageRules', N'DiscountPercent') IS NULL
        ALTER TABLE dbo.InsuranceCoverageRules ADD DiscountPercent decimal(18,6) NOT NULL CONSTRAINT DF_InsuranceCoverageRules_DiscountPercent DEFAULT (0);
    IF COL_LENGTH(N'dbo.InsuranceCoverageRules', N'FixedDiscountAmount') IS NULL
        ALTER TABLE dbo.InsuranceCoverageRules ADD FixedDiscountAmount decimal(18,6) NOT NULL CONSTRAINT DF_InsuranceCoverageRules_FixedDiscountAmount DEFAULT (0);
    IF COL_LENGTH(N'dbo.InsuranceCoverageRules', N'ValidFrom') IS NULL
        ALTER TABLE dbo.InsuranceCoverageRules ADD ValidFrom datetime2 NOT NULL CONSTRAINT DF_InsuranceCoverageRules_ValidFrom DEFAULT ('2000-01-01');
    IF COL_LENGTH(N'dbo.InsuranceCoverageRules', N'ValidTo') IS NULL
        ALTER TABLE dbo.InsuranceCoverageRules ADD ValidTo datetime2 NOT NULL CONSTRAINT DF_InsuranceCoverageRules_ValidTo DEFAULT ('2099-12-31');
END;

IF OBJECT_ID(N'dbo.InsuranceInvoiceCoverageLines', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.InsuranceInvoiceCoverageLines', N'InsuranceDiscountPercent') IS NULL
        ALTER TABLE dbo.InsuranceInvoiceCoverageLines ADD InsuranceDiscountPercent decimal(18,6) NOT NULL CONSTRAINT DF_InsuranceCoverageLines_DiscountPercent DEFAULT (0);
    IF COL_LENGTH(N'dbo.InsuranceInvoiceCoverageLines', N'InsuranceDiscountAmount') IS NULL
        ALTER TABLE dbo.InsuranceInvoiceCoverageLines ADD InsuranceDiscountAmount decimal(18,6) NOT NULL CONSTRAINT DF_InsuranceCoverageLines_DiscountAmount DEFAULT (0);
END;
