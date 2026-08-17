using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Core.Interface.Accounting;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.StockDocuments;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Vouchers;

namespace RaccoonWarehouse.Data.Repository
{
	public class UOW : IUOW
	{
		private readonly ApplicationDbContext _context;
		private readonly IMapper _mapper;
		private readonly Dictionary<Type, object> _repositories = new();

		private IGenericRepository<User> _users;
		private IGenericRepository<Voucher> _vouchers;
		private IGenericRepository<StockDocument> _stockDocuments;
		private IGenericRepository<Invoice> _invoices;
		private IGenericRepository<CashierSession> _cashierSessions;
		private IAccountRepository _accounts;
		private IGenericRepository<JournalEntry> _journalEntries;

		public UOW(ApplicationDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		public IGenericRepository<T> GetRepository<T>() where T : BaseEntity
		{
			if (typeof(T) == typeof(Account))
			{
				return (IGenericRepository<T>)Accounts;
			}

			if (!_repositories.ContainsKey(typeof(T)))
			{
				var repositoryInstance = new GenericService<T>(_context, _mapper);
				_repositories[typeof(T)] = repositoryInstance;
			}

			return (IGenericRepository<T>)_repositories[typeof(T)];
		}

		public IGenericRepository<User> Users => _users ??= new GenericService<User>(_context, _mapper);

		public IGenericRepository<Voucher> Vouchers => _vouchers ??= new GenericService<Voucher>(_context, _mapper);

		public IGenericRepository<StockDocument> StockDocuments => _stockDocuments ??= new GenericService<StockDocument>(_context, _mapper);

		public IGenericRepository<Invoice> Invoices => _invoices ??= new GenericService<Invoice>(_context, _mapper);

		public IGenericRepository<CashierSession> CashierSessions => _cashierSessions ??= new GenericService<CashierSession>(_context, _mapper);

		public IAccountRepository Accounts => _accounts ??= new AccountRepository(_context, _mapper);

		public IGenericRepository<JournalEntry> JournalEntries => _journalEntries ??= new GenericService<JournalEntry>(_context, _mapper);

		public async Task<int> CommitAsync()
		{
			return await _context.SaveChangesAsync();
		}

        public async Task<IUnitOfWorkTransaction> BeginTransactionAsync()
        {
            return new UnitOfWorkTransaction(await _context.Database.BeginTransactionAsync());
        }

		public void Dispose()
		{
		}
	}

    internal sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public UnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync() => _transaction.CommitAsync();

        public Task RollbackAsync() => _transaction.RollbackAsync();

        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
