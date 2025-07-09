using BusinessObjects.Common;
using BusinessObjects.Shipping;
using DTOs.Shipping;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class ShippingMethodRepository : GenericRepository<ShippingMethod, Guid>, IShippingMethodRepository
    {
        public ShippingMethodRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<PagedList<ShippingMethod>> GetShippingMethodsAsync(ShippingMethodFilterRequest filter)
        {
            var query = _dbSet.AsQueryable();

            // Apply filters
            if (Enum.TryParse<ShippingCategory>(filter.Name, true, out var category))
            {
                query = query.Where(sm => sm.Name == category);
            }

            if (filter.IsActive.HasValue)
                query = query.Where(sm => sm.IsActive == filter.IsActive.Value);

            if (filter.MinFee.HasValue)
                query = query.Where(sm => sm.Fee >= filter.MinFee.Value);

            if (filter.MaxFee.HasValue)
                query = query.Where(sm => sm.Fee <= filter.MaxFee.Value);

            if (filter.MinEstimatedDays.HasValue)
                query = query.Where(sm => sm.EstimatedDays >= filter.MinEstimatedDays.Value);

            if (filter.MaxEstimatedDays.HasValue)
                query = query.Where(sm => sm.EstimatedDays <= filter.MaxEstimatedDays.Value);

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDescending
                    ? query.OrderByDescending(sm => sm.Name)
                    : query.OrderBy(sm => sm.Name),
                "fee" => filter.SortDescending
                    ? query.OrderByDescending(sm => sm.Fee)
                    : query.OrderBy(sm => sm.Fee),
                "estimateddays" => filter.SortDescending
                    ? query.OrderByDescending(sm => sm.EstimatedDays)
                    : query.OrderBy(sm => sm.EstimatedDays),
                "isactive" => filter.SortDescending
                    ? query.OrderByDescending(sm => sm.IsActive)
                    : query.OrderBy(sm => sm.IsActive),
                "createdat" => filter.SortDescending
                    ? query.OrderByDescending(sm => sm.CreatedAt)
                    : query.OrderBy(sm => sm.CreatedAt),
                _ => filter.SortDescending
                    ? query.OrderByDescending(sm => sm.SortOrder).ThenByDescending(sm => sm.CreatedAt)
                    : query.OrderBy(sm => sm.SortOrder).ThenBy(sm => sm.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((filter.PageNumber - 1) * filter.PageSize)
                                  .Take(filter.PageSize)
                                  .ToListAsync();

            return new PagedList<ShippingMethod>(items, totalCount, filter.PageNumber, filter.PageSize);
        }

        public async Task<IEnumerable<ShippingMethod>> GetActiveShippingMethodsAsync()
        {
            return await _dbSet
                .Where(sm => sm.IsActive)
                .OrderBy(sm => sm.SortOrder)
                .ThenBy(sm => sm.Name)
                .ToListAsync();
        }

        public async Task<ShippingMethod?> GetShippingMethodWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(sm => sm.Orders)
                .FirstOrDefaultAsync(sm => sm.Id == id);
        }

        public async Task<bool> IsNameExistsAsync(string name, Guid? excludeId = null)
        {
            if (!Enum.TryParse<ShippingCategory>(name, true, out var category))
                return false;

            var query = _dbSet.Where(sm => sm.Name == category);
            if (excludeId.HasValue)
                query = query.Where(sm => sm.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<IEnumerable<ShippingMethod>> GetShippingMethodsBySortOrderAsync()
        {
            return await _dbSet
                .OrderBy(sm => sm.SortOrder)
                .ThenBy(sm => sm.Name)
                .ToListAsync();
        }

        public async Task<bool> UpdateSortOrderAsync(Guid id, int newSortOrder)
        {
            var shippingMethod = await _dbSet.FindAsync(id);
            if (shippingMethod == null) return false;

            shippingMethod.SortOrder = newSortOrder;
            shippingMethod.UpdatedAt = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> ToggleActiveStatusAsync(Guid id, bool isActive, Guid? updatedBy = null)
        {
            var shippingMethod = await _dbSet.FindAsync(id);
            if (shippingMethod == null) return false;

            shippingMethod.IsActive = isActive;
            shippingMethod.UpdatedAt = DateTime.UtcNow;
            if (updatedBy.HasValue)
                shippingMethod.UpdatedBy = updatedBy.Value;

            return true;
        }

        public async Task<decimal> CalculateShippingFeeAsync(Guid shippingMethodId, decimal orderAmount)
        {
            var shippingMethod = await _dbSet.FindAsync(shippingMethodId);
            if (shippingMethod == null || !shippingMethod.IsActive)
                return 0;

            // Check if order qualifies for free shipping
            if (shippingMethod.FreeShippingThreshold.HasValue &&
                orderAmount >= shippingMethod.FreeShippingThreshold.Value)
                return 0;

            return shippingMethod.Fee;
        }

        public async Task<bool> IsShippingMethodUsedInOrdersAsync(Guid id)
        {
            return await _context.Orders.AnyAsync(o => o.ShippingMethodId == id);
        }
    }
}