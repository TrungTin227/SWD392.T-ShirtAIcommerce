using Microsoft.EntityFrameworkCore.Storage;
using Repositories.Interfaces;
using System.Data;

namespace Repositories.WorkSeeds.Interfaces
{
    public interface IUnitOfWork : IGenericUnitOfWork, IDisposable, IAsyncDisposable
    {
        bool HasActiveTransaction { get; }
        T_ShirtAIcommerceContext Context { get; }
        IUserRepository UserRepository { get; }

        Task<IDbContextTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default);

        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}