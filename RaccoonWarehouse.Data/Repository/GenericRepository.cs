using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

using AutoMapper;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Protocols;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.EntityAndDtoStructure;




namespace RaccoonWarehouse.Data.Repository
{
	public class GenericService<T> :  IGenericRepository<T> 
		where T : BaseEntity

    {
        protected readonly ApplicationDbContext _context; 
		private readonly DbSet<T> _entities;	
		private readonly IMapper _mapper;
		public GenericService(ApplicationDbContext context,IMapper mapper) 
		{
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _entities = _context.Set<T>() ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper;

        }

		public async Task<T> AddAsync(T entity) => (await _entities.AddAsync(entity)).Entity;

		public async Task<IEnumerable<T>> GetAllAsync() 
		{
			return await _entities.ToListAsync();
		}

        public virtual async Task SoftDeleteAsync_repo(T entity)
        {
            if (entity is ISoftDelete softDeleteEntity)
            {
                softDeleteEntity.IsDeleted = true;
                _context.Update(entity);
                await _context.SaveChangesAsync();
            }
            else
            {
                // fallback for entities without ISoftDelete
                _context.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public IQueryable<T> AsQueryable()
        {
            return _context.Set<T>();  
        }

        public T GetById(int id)
		{
			var entity = _entities.Find(id);
			if (entity == null) 
			{
				return null;		
			}
			return entity;
		}




        public async Task<IEnumerable<T>> GetAllWithIncludeAsync(
		 params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
		{
			try
			{
				var entity = await _entities.FindAsync(id);
				if (entity == null)
				{

					return default(T);

				}
				else
				{
					return entity;
					
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"An error occurred: {ex.Message}", ex); // Proper error handling
			}

		}
		public async Task<T> GetByIdAsyncForUpdate(int id)
		{
			try
			{
				var entity = await _entities.FindAsync(id);
				if (entity == null)
				{

					return default(T); // or throw an exception

				}
				else
				{
					return entity;

				}
			}
			catch (Exception ex)
			{
				throw new Exception($"An error occurred: {ex.Message}", ex); // Proper error handling
			}

		}
		
		public async Task<T> UpdateAsync(T entity)
		{
			entity.CreatedDate =  _entities.Where(e => e.Id == entity.Id).Select(e=> 
				e.CreatedDate).FirstOrDefault();
			_entities.Update(entity);
			_context.SaveChanges();
            return entity;

		}
		public bool Any(Expression<Func<T, bool>> predicate)
		{
			return _entities.Any(predicate);
		}
		public Task<bool> AnyAsync(Expression<Func<T, bool>> condition)
		{
			return _entities.AnyAsync(condition);
		}
		public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
		{
			return await _entities.FirstOrDefaultAsync(predicate);
		}

		public async Task<bool> DeleteAsync(int id)
		{
            var entity = await _entities.FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null)
            {
                Console.WriteLine($"Entity with ID {id} not found.");
                return false;
            }

            Console.WriteLine($"Entity found: {entity.Id}, removing...");
            _entities.Remove(entity);

            Console.WriteLine("Saving changes...");

            var changes = await _context.SaveChangesAsync();
            if (changes == 0)
            {
                Console.WriteLine("No changes were saved!");
            }
			return true;
		}
		public IQueryable<T> GetAllAsQueryable() => _entities;


        public virtual async Task<T> GetByIdWithIncludeAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes)
                {
            IQueryable<T> query = _context.Set<T>();

            foreach (var include in includes)
            {
                query = query.Include(include);

            }
            var entity = await query.FirstOrDefaultAsync(predicate);

            if (entity == null)
            {
				return (default(T));
            }
           
            return entity;
        }
        public async Task<bool> DeleteWithIncludeAsync(
                Expression<Func<T, bool>> predicate,
                params Expression<Func<T, object>>[] includes)
		{
			var entity  = await GetByIdWithIncludeAsync(predicate, includes);
			_entities.Remove(entity);
			return true;

		}


        public async Task<IEnumerable<T>> GetAllWithFilteringAndInclude(
            Expression<Func<T,bool>> filter= null,
            params Expression<Func<T, object>>[] includes)
        {



            IQueryable<T> query = _entities.AsNoTracking();


            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.ToListAsync();


        }
        public async Task<IPagedResult<T>> GetPagedListAsync(
           int pageNumber,
           int pageSize,
           Expression<Func<T, bool>> filter = null,
           Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
           params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _entities.AsNoTracking();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            int totalCount = await query.CountAsync();

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
        }
/*

        public async Task<List<T>> GetAllAsyncWithFiltering<T>(
            FilterOptions<T>? filterOptions = null,
            IncludeOptions<T>? includeOptions = null,
            PaginationParams? paginationParams = null
        )
        {
            paginationParams ??= new PaginationParams();

            var query = _entities.AsQueryable(); // ✅ Fix applied here

            // Apply Includes
            if (includeOptions?.Includes != null)
            {
                foreach (var include in includeOptions.Includes)
                {
                    query = query.Include(include);
                }
            }

            // Apply Filters
            if (filterOptions?.Filters != null)
            {
                foreach (var filter in filterOptions.Filters)
                {
                    // Ensure filters are in Expression<Func<T, bool>> format
                    if (filter is Expression<Func<T, bool>> expressionFilter)
                    {
                        query = query.Where(expressionFilter); // Apply filter if it's an expression
                    }
                    else if (filter is Func<T, bool> funcFilter)
                    {
                        // Convert Func<T, bool> to Expression<Func<T, bool>>
                        var param = Expression.Parameter(typeof(T), "x");
                        var body = Expression.Invoke(Expression.Constant(funcFilter), param);
                        var expression = Expression.Lambda<Func<T, bool>>(body, param);
                        query = query.Where(expression);
                    }
                    else
                    {
                        // If filter is not an Expression<Func<T, bool>> or Func<T, bool>, handle accordingly
                        throw new InvalidOperationException("Invalid filter type.");
                    }
                }
            }

            // Get Total Count after filtering
            int totalCount = await query.CountAsync();

            // Apply Pagination
            var entities = await query
                .OrderBy(e => EF.Property<object>(e, "Id")) // Ensure consistent ordering
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PagedResult<T>(entities, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
        }
*/
    }
}
       
    



