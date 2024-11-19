using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace JazFinanzasApp.API.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;
        private IDbContextTransaction _transaction;
        private bool _isTransactionActive = false;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }


        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        
        public async Task<T> AddAsyncReturnObject(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task AddAsyncTransaction(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        //public async Task SaveChangesAsyncTransaction()
        //{
        //    await _context.SaveChangesAsync();
        //}
        

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                if (!_isTransactionActive) // Solo guarda cambios si no hay una transacción activa
                {
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task<IEnumerable<T>> GetByUserIdAsync(int userId)
        {
            return await _context.Set<T>()
                .Where(entity => EF.Property<int>(entity, "UserId") == userId)
                .ToListAsync();
        }


        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
            _isTransactionActive = true;
        }

        public async Task CommitTransactionAsync()
        {
            if(_transaction != null)
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
                _isTransactionActive = false;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if(_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
                _isTransactionActive = false;
            }
        }
    }
}
