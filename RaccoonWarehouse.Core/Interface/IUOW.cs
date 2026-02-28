
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.StockDocuments;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Vouchers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Core.Interface
{
	public interface  IUOW : IDisposable
	{
		IGenericRepository<T> GetRepository<T>() where T : BaseEntity;
        IGenericRepository<User> Users { get;}
        IGenericRepository<Voucher> Vouchers { get;}	
        IGenericRepository<Invoice> Invoices { get;}	
        IGenericRepository<StockDocument> StockDocuments { get;}
        IGenericRepository<CashierSession> CashierSessions { get;}
        Task<int> CommitAsync();

	}
}
