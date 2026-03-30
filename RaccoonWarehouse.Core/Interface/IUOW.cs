using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.StockDocuments;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Vouchers;

namespace RaccoonWarehouse.Core.Interface
{
	public interface IUOW : IDisposable
	{
		IGenericRepository<T> GetRepository<T>() where T : BaseEntity;
        IGenericRepository<User> Users { get; }
        IGenericRepository<Voucher> Vouchers { get; }
        IGenericRepository<Invoice> Invoices { get; }
        IGenericRepository<StockDocument> StockDocuments { get; }
        IGenericRepository<CashierSession> CashierSessions { get; }
        IGenericRepository<Account> Accounts { get; }
        IGenericRepository<JournalEntry> JournalEntries { get; }
        Task<int> CommitAsync();
	}
}
