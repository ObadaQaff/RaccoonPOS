using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.Operations;

public enum AccountingOperationStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public sealed class AccountingOperation : BaseEntity
{
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public AccountingOperationStatus Status { get; set; } = AccountingOperationStatus.Pending;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastAttemptDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? NextAttemptDate { get; set; }
}
