using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Insurance;

public enum InsuranceRecordStatus
{
    Draft = 1,
    Active = 2,
    Inactive = 3,
    Expired = 4
}

public enum InsuranceOrganizationType
{
    Insurer = 1,
    Administrator = 2,
    ContractualPayer = 3,
    InsurerAndPayer = 4,
    Other = 5
}

public enum InsuranceEligibilityStatus
{
    Pending = 1,
    Eligible = 2,
    Ineligible = 3,
    Expired = 4,
    Unverified = 5,
    Failed = 6
}

public enum InsuranceVerificationMethod
{
    Manual = 1,
    Portal = 2,
    Api = 3,
    FileImport = 4,
    CardReader = 5
}

public enum InsuranceClaimStatus
{
    Draft = 1,
    Prepared = 2,
    Submitted = 3,
    PartiallyApproved = 4,
    Approved = 5,
    Rejected = 6,
    PartiallyPaid = 7,
    Paid = 8,
    Closed = 9
    ,ReadyForSubmission = 10
    ,UnderReview = 11
    ,PartiallySettled = 12
    ,Cancelled = 13
}

public enum InsuranceBatchStatus
{
    Draft = 1,
    ReadyForSubmission = 2,
    Submitted = 3,
    Closed = 4
}

public enum InsuranceAuthorizationStatus
{
    Requested = 1,
    Approved = 2,
    PartiallyApproved = 3,
    Rejected = 4,
    Expired = 5
}

public enum InsuranceAdjustmentType
{
    Deduction = 1,
    Rejection = 2,
    Discount = 3,
    Other = 4
}

public enum InsuranceAttachmentOwnerType
{
    MemberPolicy = 1,
    PreAuthorization = 2,
    Claim = 3
}

public class InsuranceCompany : BaseEntity
{
    public decimal ContractualDiscountPercent { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEnglish { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? ReceivableAccountId { get; set; }
    public int? DeductionAccountId { get; set; }
    public int? PaymentAccountId { get; set; }
    public InsuranceRecordStatus Status { get; set; } = InsuranceRecordStatus.Active;
    public string? Notes { get; set; }
    public InsuranceOrganizationType OrganizationType { get; set; } = InsuranceOrganizationType.Insurer;
}

public class InsuranceNetwork : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEnglish { get; set; }
    public InsuranceRecordStatus Status { get; set; } = InsuranceRecordStatus.Active;
    public string? Notes { get; set; }
}

public class InsuranceBeneficiaryCategory : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEnglish { get; set; }
    public InsuranceRecordStatus Status { get; set; } = InsuranceRecordStatus.Active;
}

public class InsuranceContract : BaseEntity
{
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

public class InsurancePlan : BaseEntity
{
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

public class InsuranceContractVersion : BaseEntity
{
    public int InsuranceContractId { get; set; }
    public int VersionNumber { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class InsuranceCoverageRule : BaseEntity
{
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

public class InsuranceExclusion : BaseEntity
{
    public int InsuranceContractVersionId { get; set; }
    public int? InsurancePlanId { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public string? ServiceCode { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class InsuranceMemberPolicy : BaseEntity
{
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
    public InsuranceEligibilityStatus EligibilityStatus { get; set; } = InsuranceEligibilityStatus.Unverified;
    public DateTime? LastVerifiedDate { get; set; }
    public InsuranceVerificationMethod? LastVerificationMethod { get; set; }
    public string? ExternalVerificationReference { get; set; }
    public string? Notes { get; set; }
}

public class InsuranceEligibilityCheck : BaseEntity
{
    public int InsuranceMemberPolicyId { get; set; }
    public InsuranceEligibilityStatus Status { get; set; }
    public InsuranceVerificationMethod Method { get; set; }
    public int? PerformedByUserId { get; set; }
    public DateTime CheckedAt { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? ExternalReference { get; set; }
    public string? ResponseSummary { get; set; }
    public string? Notes { get; set; }
}

public class InsuranceInvoiceCoverage : BaseEntity
{
    public int InvoiceId { get; set; }
    public int InsuranceCompanyId { get; set; }
    public int? InsuranceContractId { get; set; }
    public int? InsuranceContractVersionId { get; set; }
    public int? InsuranceMemberPolicyId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DeductibleAmount { get; set; }
    public decimal CopayAmount { get; set; }
    public decimal InsurerAmount { get; set; }
    public decimal PatientAmount { get; set; }
    public string? SnapshotJson { get; set; }
}

public class InsuranceInvoiceCoverageLine : BaseEntity
{
    public int InsuranceInvoiceCoverageId { get; set; }
    public int InvoiceLineId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ContractedUnitPrice { get; set; }
    public decimal InsuranceDiscountPercent { get; set; }
    public decimal InsuranceDiscountAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CoveredAmount { get; set; }
    public decimal PatientAmount { get; set; }
    public decimal ExcludedAmount { get; set; }
    public decimal DeductibleAmount { get; set; }
    public decimal CopayAmount { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public bool RequiresAuthorization { get; set; }
    public bool IsExcluded { get; set; }
    public string? DecisionReason { get; set; }
    public int? InsurancePreAuthorizationId { get; set; }
}

public class InsuranceClaimBatch : BaseEntity
{
    public string BatchNumber { get; set; } = string.Empty;
    public int InsuranceCompanyId { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public InsuranceBatchStatus Status { get; set; } = InsuranceBatchStatus.Draft;
    public DateTime? SubmittedDate { get; set; }
    public string? ExportFileName { get; set; }
    public decimal SubmittedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public string? Notes { get; set; }
}

public class InsuranceClaim : BaseEntity
{
    public string ClaimNumber { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public int InsuranceInvoiceCoverageId { get; set; }
    public int InsuranceCompanyId { get; set; }
    public int? InsuranceMemberPolicyId { get; set; }
    public int? InsuranceClaimBatchId { get; set; }
    public InsuranceClaimStatus Status { get; set; } = InsuranceClaimStatus.Draft;
    public DateTime? SubmissionDate { get; set; }
    public DateTime? DecisionDate { get; set; }
    public decimal SubmittedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal RejectedAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
}

public class InsuranceClaimLine : BaseEntity
{
    public int InsuranceClaimId { get; set; }
    public int InvoiceLineId { get; set; }
    public decimal SubmittedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal RejectedAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public string? DeductionReason { get; set; }
    public string? Notes { get; set; }
}

public class InsuranceClaimAdjustment : BaseEntity
{
    public int InsuranceClaimId { get; set; }
    public InsuranceAdjustmentType Type { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsAccountingPosted { get; set; }
}

public class InsuranceClaimPayment : BaseEntity
{
    public int InsuranceCompanyId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public int? FinancialTransactionId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

public class InsuranceClaimPaymentAllocation : BaseEntity
{
    public int InsuranceClaimPaymentId { get; set; }
    public int InsuranceClaimId { get; set; }
    public decimal Amount { get; set; }
}

public class InsurancePreAuthorization : BaseEntity
{
    public string AuthorizationNumber { get; set; } = string.Empty;
    public int InsuranceCompanyId { get; set; }
    public int InsuranceMemberPolicyId { get; set; }
    public int? InvoiceId { get; set; }
    public InsuranceAuthorizationStatus Status { get; set; } = InsuranceAuthorizationStatus.Requested;
    public DateTime RequestedDate { get; set; }
    public DateTime? DecisionDate { get; set; }
    public DateTime? ValidTo { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public string? Notes { get; set; }
}

public class InsurancePreAuthorizationLine : BaseEntity
{
    public int InsurancePreAuthorizationId { get; set; }
    public int ProductId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public InsuranceAuthorizationStatus Status { get; set; } = InsuranceAuthorizationStatus.Requested;
    public string? RejectionReason { get; set; }
}

public class InsuranceAttachment : BaseEntity
{
    public InsuranceAttachmentOwnerType OwnerType { get; set; }
    public int OwnerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public string? Notes { get; set; }
}
