using BusinessObjects.CustomDesigns;
using BusinessObjects.Orders;
using BusinessObjects.Products;
using DTOs.OrderItem;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;
using System;
using System.Linq.Expressions;

namespace Repositories.Implementations
{
    public class OrderItemRepository : GenericRepository<OrderItem, Guid>, IOrderItemRepository
    {
        public OrderItemRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<PagedList<OrderItem>> GetOrderItemsAsync(OrderItemQueryDto query)
        {
            // Sử dụng các include expressions từ common pattern
            var includes = new Expression<Func<OrderItem, object>>[]
            {
                oi => oi.Product,
                oi => oi.CustomDesign,
                oi => oi.ProductVariant,
                oi => oi.Order
            };

            // Predicate để filter dữ liệu
            Expression<Func<OrderItem, bool>>? predicate = BuildFilterPredicate(query);

            // Ordering function
            Func<IQueryable<OrderItem>, IOrderedQueryable<OrderItem>>? orderBy = BuildOrderingFunction(query);

            // Sử dụng method GetPagedAsync từ GenericRepository thay vì custom implementation
            return await GetPagedAsync(
                query.Page,
                query.Size,
                predicate,
                orderBy,
                includes);
        }

        public async Task<IEnumerable<OrderItem>> GetByOrderIdAsync(Guid orderId)
        {
            return await GetAllAsync(
                oi => oi.OrderId == orderId,
                q => q.OrderBy(oi => oi.ItemName),
                oi => oi.Product,
                oi => oi.CustomDesign,
                oi => oi.ProductVariant);
        }

        public async Task<OrderItem?> GetWithDetailsAsync(Guid id)
        {
            return await GetByIdAsync(id,
                oi => oi.Product,
                oi => oi.CustomDesign,
                oi => oi.ProductVariant,
                oi => oi.Order);
        }

        public async Task<bool> ValidateOrderExistsAsync(Guid orderId)
        {
            return await AnyAsync(oi => oi.OrderId == orderId);
        }

        public async Task<bool> ValidateProductExistsAsync(Guid productId)
        {
            return await _context.Set<Product>().AnyAsync(p => p.Id == productId);
        }

        public async Task<bool> ValidateCustomDesignExistsAsync(Guid customDesignId)
        {
            return await _context.Set<CustomDesign>().AnyAsync(cd => cd.Id == customDesignId);
        }

        public async Task<bool> ValidateProductVariantExistsAsync(Guid productVariantId)
        {
            return await _context.Set<ProductVariant>().AnyAsync(pv => pv.Id == productVariantId);
        }

        public async Task<decimal> GetOrderTotalAsync(Guid orderId)
        {
            var orderItems = await GetAllAsync(oi => oi.OrderId == orderId);
            return orderItems.Sum(oi => oi.TotalPrice);
        }

        public async Task<int> GetOrderItemCountAsync(Guid orderId)
        {
            var orderItems = await GetAllAsync(oi => oi.OrderId == orderId);
            return orderItems.Sum(oi => oi.Quantity);
        }

        #region Private Helper Methods

        private static Expression<Func<OrderItem, bool>>? BuildFilterPredicate(OrderItemQueryDto query)
        {
            Expression<Func<OrderItem, bool>>? predicate = null;

            if (query.OrderId.HasValue)
                predicate = CombinePredicates(predicate, oi => oi.OrderId == query.OrderId.Value);

            if (query.ProductId.HasValue)
                predicate = CombinePredicates(predicate, oi => oi.ProductId == query.ProductId.Value);

            if (query.CustomDesignId.HasValue)
                predicate = CombinePredicates(predicate, oi => oi.CustomDesignId == query.CustomDesignId.Value);

            if (query.ProductVariantId.HasValue)
                predicate = CombinePredicates(predicate, oi => oi.ProductVariantId == query.ProductVariantId.Value);

            if (!string.IsNullOrEmpty(query.SelectedColor))
                predicate = CombinePredicates(predicate, oi => oi.SelectedColor == query.SelectedColor);

            if (!string.IsNullOrEmpty(query.SelectedSize))
                predicate = CombinePredicates(predicate, oi => oi.SelectedSize == query.SelectedSize);

            if (query.MinUnitPrice.HasValue)
                predicate = CombinePredicates(predicate, oi => oi.UnitPrice >= query.MinUnitPrice.Value);

            if (query.MaxUnitPrice.HasValue)
                predicate = CombinePredicates(predicate, oi => oi.UnitPrice <= query.MaxUnitPrice.Value);

            if (query.MinQuantity.HasValue)
                predicate = CombinePredicates(predicate, oi => oi.Quantity >= query.MinQuantity.Value);

            if (query.MaxQuantity.HasValue)
                predicate = CombinePredicates(predicate, oi => oi.Quantity <= query.MaxQuantity.Value);

            if (!string.IsNullOrEmpty(query.Search))
            {
                predicate = CombinePredicates(predicate, oi =>
                    oi.ItemName.Contains(query.Search) ||
                    (oi.Product != null && oi.Product.Name.Contains(query.Search)));
            }

            return predicate;
        }

        private static Func<IQueryable<OrderItem>, IOrderedQueryable<OrderItem>>? BuildOrderingFunction(OrderItemQueryDto query)
        {
            if (string.IsNullOrEmpty(query.SortBy))
                return q => query.IsDescending ? q.OrderByDescending(oi => oi.Id) : q.OrderBy(oi => oi.Id);

            return query.SortBy.ToLower() switch
            {
                "itemname" => q => query.IsDescending ? q.OrderByDescending(oi => oi.ItemName) : q.OrderBy(oi => oi.ItemName),
                "unitprice" => q => query.IsDescending ? q.OrderByDescending(oi => oi.UnitPrice) : q.OrderBy(oi => oi.UnitPrice),
                "quantity" => q => query.IsDescending ? q.OrderByDescending(oi => oi.Quantity) : q.OrderBy(oi => oi.Quantity),
                "totalprice" => q => query.IsDescending ? q.OrderByDescending(oi => oi.TotalPrice) : q.OrderBy(oi => oi.TotalPrice),
                "selectedcolor" => q => query.IsDescending ? q.OrderByDescending(oi => oi.SelectedColor) : q.OrderBy(oi => oi.SelectedColor),
                "selectedsize" => q => query.IsDescending ? q.OrderByDescending(oi => oi.SelectedSize) : q.OrderBy(oi => oi.SelectedSize),
                _ => q => query.IsDescending ? q.OrderByDescending(oi => oi.Id) : q.OrderBy(oi => oi.Id)
            };
        }

        private static Expression<Func<T, bool>>? CombinePredicates<T>(
            Expression<Func<T, bool>>? first,
            Expression<Func<T, bool>> second)
        {
            if (first == null)
                return second;

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.AndAlso(
                Expression.Invoke(first, parameter),
                Expression.Invoke(second, parameter));

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        #endregion
    }
}