using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.StockDocuments;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Vouchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		
		public UOW(ApplicationDbContext context, IMapper mapper) 
		{
			_context = context;
			_mapper = mapper;

		
		}
		public IGenericRepository<T> GetRepository<T>() where T : BaseEntity
		{
			if (!_repositories.ContainsKey(typeof(T)))
			{
				var repositoryInstance = new GenericService<T>(_context, _mapper);
				_repositories[typeof(T)] = repositoryInstance;
			}
			return (IGenericRepository<T>)_repositories[typeof(T)];
		}


		public IGenericRepository<User> Users
		{
			get
			{
				return _users ??= new GenericService <User>(_context, _mapper);
				
			}
		}
	
		public IGenericRepository<Voucher> Vouchers
        {
			get
			{
				return _vouchers ??= new GenericService <Voucher>(_context, _mapper);
				
			}
		
		
		}
		public IGenericRepository<StockDocument> StockDocuments
        {
			get
			{
				return _stockDocuments ??= new GenericService <StockDocument>(_context, _mapper);
				
			}
		
		
		}
	
		public IGenericRepository<Invoice> Invoices
        {
			get
			{
				return _invoices ??= new GenericService<Invoice>(_context, _mapper);
				
			}
		
		
		}


		public IGenericRepository<CashierSession> CashierSessions
        {
			get
			{
				return _cashierSessions ??= new GenericService<CashierSession>(_context, _mapper);
				
			}
		
		
		}
	

        public async Task<int> CommitAsync()
		{
				return await _context.SaveChangesAsync();
		}
		

		public void Dispose()
		{
			
		}

	}
}
