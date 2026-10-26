using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Domain.Accounting.RecurringJournals;
using RaccoonWarehouse.Domain.Accounting.RecurringJournals.DTOs;
using RaccoonWarehouse.Domain.Accounting.RecurringJournals.Enums;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface IRecurringJournalService
{
    Task<RecurringJournal> CreateAsync(RecurringJournalUpsertDto dto);
    Task<RecurringJournal?> GetByIdAsync(int id);
    Task<List<RecurringJournal>> GetAllAsync();
    Task UpdateAsync(int id, RecurringJournalUpsertDto dto);
    Task DeleteAsync(int id);
    Task ExecuteDueAsync(DateTime asOfDate);
    Task<List<RecurringJournal>> GetUpcomingAsync(int days);
}
