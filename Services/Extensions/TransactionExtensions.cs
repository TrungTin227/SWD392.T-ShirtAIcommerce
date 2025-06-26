using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Repositories.Commons;
using Repositories.WorkSeeds.Interfaces;
using System.Data;

namespace Services.Extensions
{
    public static class TransactionExtensions
    {
        public static async Task<ApiResult<T>> ExecuteTransactionAsync<T>(
            this IUnitOfWork unitOfWork,
            Func<Task<ApiResult<T>>> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            if (unitOfWork is IUnitOfWorkWithContext contextProvider)
            {
                var strategy = contextProvider.Context.Database.CreateExecutionStrategy();

                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await contextProvider.Context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                    try
                    {
                        var result = await operation();

                        if (result.IsSuccess)
                            await transaction.CommitAsync(cancellationToken);
                        else
                            await transaction.RollbackAsync(cancellationToken);

                        return result;
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                });
            }
            else
            {
                return await ExecuteWithoutRetryAsync(unitOfWork, operation, isolationLevel, cancellationToken);
            }
        }

        public static async Task<ApiResult<bool>> ExecuteTransactionAsync(
            this IUnitOfWork unitOfWork,
            Func<Task<ApiResult<bool>>> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            return await unitOfWork.ExecuteTransactionAsync(async () => await operation(), isolationLevel, cancellationToken);
        }

        public static async Task<ApiResult<bool>> ExecuteTransactionAsync(
            this IUnitOfWork unitOfWork,
            Func<Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            return await unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await operation();
                return ApiResult<bool>.Success(true, "Operation completed successfully");
            }, isolationLevel, cancellationToken);
        }

        private static async Task<ApiResult<T>> ExecuteWithoutRetryAsync<T>(
            IUnitOfWork unitOfWork,
            Func<Task<ApiResult<T>>> operation,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            if (unitOfWork.HasActiveTransaction)
            {
                return await operation();
            }

            IDbContextTransaction? transaction = null;
            try
            {
                transaction = await unitOfWork.BeginTransactionAsync(isolationLevel, cancellationToken);

                var result = await operation();

                if (result.IsSuccess)
                    await transaction.CommitAsync(cancellationToken);
                else
                    await transaction.RollbackAsync(cancellationToken);

                return result;
            }
            catch
            {
                if (transaction != null)
                    await SafeRollbackAsync(transaction, cancellationToken);
                throw;
            }
            finally
            {
                transaction?.Dispose();
            }
        }

        private static async Task SafeRollbackAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch
            {
                // Log rollback error if needed
            }
        }
    }

    public interface IUnitOfWorkWithContext : IUnitOfWork
    {
        DbContext Context { get; }
    }
}
