using Repositories.Commons;
using Repositories.WorkSeeds.Interfaces;
using System.Data;
using Microsoft.EntityFrameworkCore;

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
            // Kiểm tra xem IUnitOfWork có property Context không
            // Nếu không có, bạn cần thêm vào interface IUnitOfWork
            if (unitOfWork is not IUnitOfWorkWithContext contextProvider)
            {
                // Fallback: Execute without retry strategy nếu không có Context
                return await ExecuteWithoutRetryAsync(unitOfWork, operation, isolationLevel, cancellationToken);
            }

            // Sử dụng execution strategy để handle retry logic
            var strategy = contextProvider.Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await contextProvider.Context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                try
                {
                    var result = await operation();

                    if (result.IsSuccess)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }
                    else
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        private static async Task<ApiResult<T>> ExecuteWithoutRetryAsync<T>(
            IUnitOfWork unitOfWork,
            Func<Task<ApiResult<T>>> operation,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            // Existing logic từ code cũ của bạn
            if (unitOfWork.HasActiveTransaction)
            {
                return await operation();
            }

            using var transaction = await unitOfWork.BeginTransactionAsync(isolationLevel, cancellationToken);
            try
            {
                var result = await operation();

                if (result.IsSuccess)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    // Interface helper để access DbContext
    public interface IUnitOfWorkWithContext : IUnitOfWork
    {
        DbContext Context { get; }
    }
}