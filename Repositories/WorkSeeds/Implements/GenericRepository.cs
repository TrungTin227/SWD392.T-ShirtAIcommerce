using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.WorkSeeds.Interfaces;
using System.Linq.Expressions;

namespace Repositories.WorkSeeds.Implements
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
        where TEntity : class
    {
        protected readonly T_ShirtAIcommerceContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(T_ShirtAIcommerceContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }

        public virtual IQueryable<TEntity> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }

        // Repository chỉ làm việc với data, không set audit fields
        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);
            return entity;
        }

        public virtual async Task<bool> DeleteAsync(TKey id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return false;

            _dbSet.Remove(entity);
            return true;
        }

        public virtual async Task<TEntity?> GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(e => EF.Property<TKey>(e, "Id").Equals(id));
        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
        {
            _dbSet.UpdateRange(entities);
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<TKey> ids)
        {
            var entities = await _dbSet.Where(e => ids.Contains(EF.Property<TKey>(e, "Id"))).ToListAsync();
            _dbSet.RemoveRange(entities);
        }

        public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            if (predicate != null)
                query = query.Where(predicate);

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (orderBy != null)
                query = orderBy(query);

            return await query.ToListAsync();
        }

        public virtual async Task<PagedList<TEntity>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            if (predicate != null)
                query = query.Where(predicate);

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (orderBy != null)
                query = orderBy(query);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedList<TEntity>(items, totalCount, pageNumber, pageSize);
        }

        public virtual async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
        {
            return predicate == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);
        }

        public virtual async Task<bool> SoftDeleteAsync(TKey id, Guid? deletedBy = null)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return false;

            // Check if entity supports soft delete
            var isDeletedProperty = entity.GetType().GetProperty("IsDeleted");
            if (isDeletedProperty != null && isDeletedProperty.PropertyType == typeof(bool))
            {
                isDeletedProperty.SetValue(entity, true);

                var deletedAtProperty = entity.GetType().GetProperty("DeletedAt");
                if (deletedAtProperty != null && deletedAtProperty.PropertyType == typeof(DateTime?))
                {
                    deletedAtProperty.SetValue(entity, DateTime.UtcNow);
                }

                var deletedByProperty = entity.GetType().GetProperty("DeletedBy");
                if (deletedByProperty != null && deletedBy.HasValue)
                {
                    deletedByProperty.SetValue(entity, deletedBy.Value);
                }

                _dbSet.Update(entity);
                return true;
            }

            // Fallback to hard delete
            _dbSet.Remove(entity);
            return true;
        }

        public virtual async Task<bool> RestoreAsync(TKey id, Guid? restoredBy = null)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return false;

            var isDeletedProperty = entity.GetType().GetProperty("IsDeleted");
            if (isDeletedProperty != null && isDeletedProperty.PropertyType == typeof(bool))
            {
                isDeletedProperty.SetValue(entity, false);

                var deletedAtProperty = entity.GetType().GetProperty("DeletedAt");
                if (deletedAtProperty != null)
                {
                    deletedAtProperty.SetValue(entity, null);
                }

                var restoredAtProperty = entity.GetType().GetProperty("RestoredAt");
                if (restoredAtProperty != null && restoredAtProperty.PropertyType == typeof(DateTime?))
                {
                    restoredAtProperty.SetValue(entity, DateTime.UtcNow);
                }

                var restoredByProperty = entity.GetType().GetProperty("RestoredBy");
                if (restoredByProperty != null && restoredBy.HasValue)
                {
                    restoredByProperty.SetValue(entity, restoredBy.Value);
                }

                _dbSet.Update(entity);
                return true;
            }

            return false;
        }
    }
}