namespace RaccoonWarehouse.Domain.Insurance;

public sealed class PosInsuranceBillingDto
{
    public int InsuranceCompanyId { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public string? PolicyNumber { get; set; }
    public decimal CustomerDiscountPercent { get; set; }
    public decimal ContractualDiscountPercent { get; set; }
    public decimal OriginalTotal { get; set; }
    public decimal CustomerDiscountAmount { get; set; }
    public decimal ContractualDiscountAmount { get; set; }
    public decimal PatientAmount { get; set; }
    public decimal InsurerAmount { get; set; }
    public decimal InvoiceTotal { get; set; }
}
