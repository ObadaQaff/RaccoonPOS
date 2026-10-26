using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Banks;
using RaccoonWarehouse.Domain.Accounting.Banks.DTOs;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface IBankReconciliationService
{
    Task ImportStatementAsync(int bankAccountId, List<StatementLineDto> lines);
    Task AutoMatchAsync(int bankAccountId, DateTime statementDate);
    Task ManualMatchAsync(int statementLineId, int journalEntryLineId);
    Task UnmatchAsync(int statementLineId);
    Task<BankReconciliationSummaryDto> GetSummaryAsync(int bankAccountId, DateTime statementDate);
}
