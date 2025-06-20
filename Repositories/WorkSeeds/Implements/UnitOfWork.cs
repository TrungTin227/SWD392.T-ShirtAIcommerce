using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using System.Data;

namespace Repositories.WorkSeeds.Implements
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly T_ShirtAIcommerceContext _context;
        private readonly IRepositoryFactory _repositoryFactory;
        private IDbContextTransaction? _transaction;

        // Specific repositories
        private IUserRepository? _userRepository;

        public UnitOfWork(T_ShirtAIcommerceContext context, IRepositoryFactory repositoryFactory)
        {
            _context = context;
            _repositoryFactory = repositoryFactory;
        }

        
        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : class
        {
            return _repositoryFactory.GetRepository<TEntity, TKey>();
        }

        public IUserRepository UserRepository =>
            _userRepository ??= _repositoryFactory.GetCustomRepository<IUserRepository>();

        public bool HasActiveTransaction => _transaction != null;

        public async Task<IDbContextTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
                throw new InvalidOperationException("A transaction is already active.");

            _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return _transaction;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("No active transaction to commit.");

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("No active transaction to rollback.");

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();

            if (_context != null)
                await _context.DisposeAsync();
        }
    }
}