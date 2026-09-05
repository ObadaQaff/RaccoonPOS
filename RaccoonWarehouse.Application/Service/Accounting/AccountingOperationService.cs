using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Accounting.Operations;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;

namespace RaccoonWarehouse.Application.Service.Accounting;

public interface IAccountingOperationService
{
    Task EnqueueInvoiceAsync(InvoiceWriteDto invoice);
    Task EnqueueFinancialAsync(FinancialPostDto transaction, string referenceNumber);
    Task<List<AccountingOperation>> GetAllAsync();
    Task<List<AccountingOperation>> GetFailedAsync();
    Task<bool> RetryAsync(int operationId);
    Task<int> RetryFailedAsync();
    Task<bool> DeleteFailedAsync(int operationId);
}

public sealed class AccountingOperationService : IAccountingOperationService
{
    private readonly IUOW _uow;

    public AccountingOperationService(IUOW uow)
    {
        _uow = uow;
    }

    public async Task EnqueueInvoiceAsync(InvoiceWriteDto invoice)
    {
        var repository = _uow.GetRepository<AccountingOperation>();
        var existing = await repository.GetAllAsQueryable()
            .FirstOrDefaultAsync(operation =>
                operation.ReferenceType == "Invoice" &&
                operation.ReferenceId == invoice.Id &&
                operation.OperationType == "PostInvoiceJournal");

        if (existing != null)
            return;

        var now = DateTime.Now;
        await repository.AddAsync(new AccountingOperation
        {
            ReferenceType = "Invoice",
            ReferenceId = invoice.Id,
            ReferenceNumber = invoice.InvoiceNumber ?? invoice.Id.ToString(),
            OperationType = "PostInvoiceJournal",
            PayloadJson = JsonSerializer.Serialize(invoice),
            Status = AccountingOperationStatus.Pending,
            CreatedDate = now,
            UpdatedDate = now,
            NextAttemptDate = now
        });
    }

    public async Task EnqueueFinancialAsync(FinancialPostDto transaction, string referenceNumber)
    {
        var repository = _uow.GetRepository<AccountingOperation>();
        var referenceId = transaction.SourceId ?? 0;
        // A mixed invoice can have several financial allocations. Keep one
        // idempotency key per invoice and payment method, not one key for the
        // whole invoice, otherwise cash + Visa collide on the unique index.
        var operationType = $"PostFinancialTransaction:{(int)transaction.Method}";
        var existing = await repository.GetAllAsQueryable()
            .FirstOrDefaultAsync(operation =>
                operation.ReferenceType == "InvoiceFinancial" &&
                operation.ReferenceId == referenceId &&
                operation.OperationType == operationType);

        if (existing != null)
            return;

        var now = DateTime.Now;
        await repository.AddAsync(new AccountingOperation
        {
            ReferenceType = "InvoiceFinancial",
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber,
            OperationType = operationType,
            PayloadJson = JsonSerializer.Serialize(transaction),
            Status = AccountingOperationStatus.Pending,
            CreatedDate = now,
            UpdatedDate = now,
            NextAttemptDate = now
        });
    }

    public async Task<List<AccountingOperation>> GetFailedAsync()
    {
        return await GetAllAsync(status: AccountingOperationStatus.Failed);
    }

    public async Task<List<AccountingOperation>> GetAllAsync()
    {
        return await GetAllAsync(status: null);
    }

    private async Task<List<AccountingOperation>> GetAllAsync(AccountingOperationStatus? status)
    {
        var query = _uow.GetRepository<AccountingOperation>()
            .GetAllAsQueryable()
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(operation => operation.Status == status.Value);

        return await query
            .OrderBy(operation => operation.Status == AccountingOperationStatus.Failed ? 0 : 1)
            .ThenByDescending(operation => operation.UpdatedDate)
            .ToListAsync();
    }

    public async Task<bool> RetryAsync(int operationId)
    {
        var operation = await _uow.GetRepository<AccountingOperation>()
            .GetAllAsQueryable()
            .FirstOrDefaultAsync(item => item.Id == operationId);

        if (operation == null || operation.Status == AccountingOperationStatus.Processing)
            return false;

        operation.Status = AccountingOperationStatus.Pending;
        operation.LastError = null;
        operation.NextAttemptDate = DateTime.Now;
        operation.UpdatedDate = DateTime.Now;
        await _uow.CommitAsync();
        return true;
    }

    public async Task<int> RetryFailedAsync()
    {
        var operations = await _uow.GetRepository<AccountingOperation>()
            .GetAllAsQueryable()
            .Where(operation => operation.Status == AccountingOperationStatus.Failed)
            .ToListAsync();

        if (operations.Count == 0)
            return 0;

        var now = DateTime.Now;
        foreach (var operation in operations)
        {
            operation.Status = AccountingOperationStatus.Pending;
            operation.LastError = null;
            operation.NextAttemptDate = now;
            operation.UpdatedDate = now;
        }

        await _uow.CommitAsync();
        return operations.Count;
    }

    public async Task<bool> DeleteFailedAsync(int operationId)
    {
        var repository = _uow.GetRepository<AccountingOperation>();
        var operation = await repository.GetAllAsQueryable()
            .FirstOrDefaultAsync(item => item.Id == operationId);

        if (operation == null || operation.Status != AccountingOperationStatus.Failed)
            return false;

        await repository.DeleteAsync(operationId);
        await _uow.CommitAsync();
        return true;
    }
}

public sealed class AccountingOperationProcessor
{
    private sealed record OperationProcessResult(int OperationId, bool Completed);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly SemaphoreSlim _runGate = new(1, 1);
    private Task? _loop;

    public AccountingOperationProcessor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Start()
    {
        _loop ??= Task.Run(ProcessLoopAsync);
    }

    private async Task ProcessLoopAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(_cancellation.Token))
        {
            try
            {
                await ProcessDueAsync(ignoreSchedule: false, excludedOperationIds: null);
            }
            catch
            {
                // A later cycle retries after transient database or accounting failures.
            }
        }
    }

    public async Task<bool> ProcessPendingAsync()
    {
        await _runGate.WaitAsync();
        try
        {
            return await ProcessDueAsync(ignoreSchedule: true, excludedOperationIds: null) != null;
        }
        finally
        {
            _runGate.Release();
        }
    }

    public async Task<(int Processed, int Failed)> ProcessPendingBatchAsync(int maxOperations)
    {
        if (maxOperations <= 0)
            return (0, 0);

        await _runGate.WaitAsync();
        try
        {
            var excludedOperationIds = new HashSet<int>();
            var processed = 0;
            var failed = 0;

            while (processed + failed < maxOperations)
            {
                var result = await ProcessDueAsync(ignoreSchedule: true, excludedOperationIds);
                if (result == null)
                    break;

                excludedOperationIds.Add(result.OperationId);
                if (result.Completed)
                    processed++;
                else
                    failed++;
            }

            return (processed, failed);
        }
        finally
        {
            _runGate.Release();
        }
    }

    private async Task<OperationProcessResult?> ProcessDueAsync(
        bool ignoreSchedule,
        HashSet<int>? excludedOperationIds)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUOW>();
        var operation = await uow.GetRepository<AccountingOperation>()
            .GetAllAsQueryable()
            .Where(item => (item.Status == AccountingOperationStatus.Pending || item.Status == AccountingOperationStatus.Failed) &&
                           (ignoreSchedule || !item.NextAttemptDate.HasValue || item.NextAttemptDate <= DateTime.Now) &&
                           (excludedOperationIds == null || !excludedOperationIds.Contains(item.Id)))
            .OrderBy(item => item.CreatedDate)
            .FirstOrDefaultAsync();

        if (operation == null)
            return null;
       
        operation.Status = AccountingOperationStatus.Processing;
        operation.LastAttemptDate = DateTime.Now;
        operation.UpdatedDate = DateTime.Now;
        await uow.CommitAsync();

        var completed = false;
        string? errorMessage = null;
        try
        {
            string? failureMessage = null;
            if (operation.OperationType == "PostFinancialTransaction"
                || operation.OperationType.StartsWith("PostFinancialTransaction:", StringComparison.Ordinal))
            {
                var transaction = JsonSerializer.Deserialize<FinancialPostDto>(operation.PayloadJson)
                    ?? throw new InvalidOperationException("Financial operation payload is empty.");
                var financialResult = await scope.ServiceProvider
                    .GetRequiredService<IFinancialTransactionService>()
                    .PostAsync(transaction);
                if (!financialResult.Success)
                    failureMessage = financialResult.Message;
            }
            else
            {
                var invoice = JsonSerializer.Deserialize<InvoiceWriteDto>(operation.PayloadJson)
                    ?? throw new InvalidOperationException("Accounting operation payload is empty.");
                var journalResult = await scope.ServiceProvider
                    .GetRequiredService<IAccountingService>()
                    .PostInvoiceEntryAsync(invoice);
                if (!journalResult.Success)
                    failureMessage = journalResult.Message;
            }

            if (failureMessage != null)
                throw new InvalidOperationException(failureMessage);

            completed = true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        await UpdateOperationStatusAsync(operation.Id, completed, errorMessage);
        return new OperationProcessResult(operation.Id, completed);
    }

    private async Task UpdateOperationStatusAsync(int operationId, bool completed, string? errorMessage)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUOW>();
        var operation = await uow.GetRepository<AccountingOperation>()
            .GetAllAsQueryable()
            .FirstOrDefaultAsync(item => item.Id == operationId);

        if (operation == null)
            return;

        operation.Status = completed
            ? AccountingOperationStatus.Completed
            : AccountingOperationStatus.Failed;
        operation.UpdatedDate = DateTime.Now;
        operation.LastError = completed ? null : errorMessage;
        if (completed)
        {
            operation.CompletedDate = DateTime.Now;
            operation.NextAttemptDate = null;
        }
        else
        {
            operation.RetryCount++;
            operation.NextAttemptDate = DateTime.Now.AddMinutes(Math.Min(30, Math.Max(1, operation.RetryCount * 2)));
        }

        await uow.CommitAsync();
    }
}
