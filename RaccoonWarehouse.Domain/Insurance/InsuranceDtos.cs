namespace RaccoonWarehouse.Domain.Insurance;

public sealed class InsuranceCompanyDto
{
    public decimal ContractualDiscountPercent { get; set; }
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEnglish { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public InsuranceRecordStatus Status { get; set; }
    public InsuranceOrganizationType OrganizationType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public sealed class InsuranceCompanyWriteDto
{
    public decimal ContractualDiscountPercent { get; set; }
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEnglish { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public InsuranceRecordStatus Status { get; set; } = InsuranceRecordStatus.Active;
    public InsuranceOrganizationType OrganizationType { get; set; } = InsuranceOrganizationType.Insurer;
    public int? ReceivableAccountId { get; set; }
    public int? DeductionAccountId { get; set; }
    public int? PaymentAccountId { get; set; }
    public string? Notes { get; set; }
}

public sealed class InsurancePlanDto
{
    public int Id { get; set; }
    public int InsuranceContractId { get; set; }
    public int? InsuranceContractVersionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEnglish { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public InsuranceRecordStatus Status { get; set; }
}

public sealed class InsurancePlanWriteDto
{
    public int Id { get; set; }
    public int InsuranceContractId { get; set; }
    public int? InsuranceContractVersionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEnglish { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public InsuranceRecordStatus Status { get; set; } = InsuranceRecordStatus.Draft;
    public string? Notes { get; set; }
}

public sealed class InsuranceContractVersionDto
{
    public int Id { get; set; }
    public int InsuranceContractId { get; set; }
    public int VersionNumber { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
}

public sealed class InsuranceContractVersionWriteDto
{
    public int Id { get; set; }
    public int InsuranceContractId { get; set; }
    public int VersionNumber { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public sealed class InsuranceCoverageRuleDto
{
    public int Id { get; set; }
    public int InsuranceContractVersionId { get; set; }
    public int? InsurancePlanId { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public string? ServiceCode { get; set; }
    public decimal CoveragePercent { get; set; }
    public decimal CopayPercent { get; set; }
    public decimal DeductibleAmount { get; set; }
    public decimal? PerClaimLimit { get; set; }
    public decimal? AnnualLimit { get; set; }
    public bool RequiresAuthorization { get; set; }
    public bool IsExcluded { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal FixedDiscountAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string? Notes { get; set; }
}

public sealed class InsuranceCoverageRuleWriteDto
{
    public int Id { get; set; }
    public int InsuranceContractVersionId { get; set; }
    public int? InsurancePlanId { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public string? ServiceCode { get; set; }
    public decimal CoveragePercent { get; set; }
    public decimal CopayPercent { get; set; }
    public decimal DeductibleAmount { get; set; }
    public decimal? PerClaimLimit { get; set; }
    public decimal? AnnualLimit { get; set; }
    public bool RequiresAuthorization { get; set; }
    public bool IsExcluded { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal FixedDiscountAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string? Notes { get; set; }
}

public sealed class InsuranceEligibilityCheckWriteDto
{
    public int InsuranceMemberPolicyId { get; set; }
    public InsuranceEligibilityStatus Status { get; set; }
    public InsuranceVerificationMethod Method { get; set; } = InsuranceVerificationMethod.Manual;
    public int? PerformedByUserId { get; set; }
    public DateTime? CheckedAt { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? ExternalReference { get; set; }
    public string? ResponseSummary { get; set; }
    public string? Notes { get; set; }
}

public sealed class InsuranceContractWriteDto
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public int InsuranceCompanyId { get; set; }
    public int? NetworkId { get; set; }
    public int? AdministratorId { get; set; }
    public int? ContractualPayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public InsuranceRecordStatus Status { get; set; } = InsuranceRecordStatus.Draft;
    public string? SettlementTerms { get; set; }
    public int? PaymentDueDays { get; set; }
    public string? Notes { get; set; }
}

public sealed class InsuranceContractDto
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public int InsuranceCompanyId { get; set; }
    public int? AdministratorId { get; set; }
    public int? ContractualPayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public InsuranceRecordStatus Status { get; set; }
    public int? PaymentDueDays { get; set; }
}

public sealed class InsuranceMemberPolicyDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int InsuranceCompanyId { get; set; }
    public int InsuranceContractId { get; set; }
    public int? InsurancePlanId { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public string? PolicyNumber { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public InsuranceEligibilityStatus EligibilityStatus { get; set; }
    public InsuranceRecordStatus Status { get; set; }
}

public sealed class InsuranceMemberPolicyWriteDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int InsuranceCompanyId { get; set; }
    public int InsuranceContractId { get; set; }
    public int? InsurancePlanId { get; set; }
    public int? BeneficiaryCategoryId { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public string? PolicyNumber { get; set; }
    public string? CardNumber { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsPrimary { get; set; }
    public InsuranceRecordStatus Status { get; set; } = InsuranceRecordStatus.Active;
    public string? Notes { get; set; }
}

public sealed class InsuranceClaimSummaryDto
{
    public int Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public int InsuranceCompanyId { get; set; }
    public int? InsuranceClaimBatchId { get; set; }
    public InsuranceClaimStatus Status { get; set; }
    public DateTime? SubmissionDate { get; set; }
    public decimal SubmittedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal RejectedAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
}

public sealed class InsuranceClaimPaymentWriteDto
{
    public int InsuranceCompanyId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public int? FinancialTransactionId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public List<InsuranceClaimPaymentAllocationDto> Allocations { get; set; } = new();
}

public sealed class InsuranceClaimPaymentAllocationDto
{
    public int InsuranceClaimId { get; set; }
    public decimal Amount { get; set; }
}

public sealed class InsuranceClaimReviewDto
{
    public int ClaimId { get; set; }
    public InsuranceClaimStatus Status { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal RejectedAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
}

public sealed class InsuranceClaimBatchDto
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int InsuranceCompanyId { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public InsuranceBatchStatus Status { get; set; }
    public int ClaimCount { get; set; }
    public decimal SubmittedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal DeductionAmount { get; set; }
}

public sealed class InsuranceReceivableAgingDto
{
    public int InsuranceCompanyId { get; set; }
    public string Bucket { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int ClaimCount { get; set; }
}
