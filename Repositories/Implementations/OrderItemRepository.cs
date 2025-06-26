using BusinessObjects.CustomDesigns;
using BusinessObjects.Orders;
using BusinessObjects.Products;
using DTOs.OrderItem;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;
using System;

namespace Repositories.Implements
{
    public class OrderItemRepository : GenericRepository<OrderItem, Guid>, IOrderItemRepository
    {
        public OrderItemRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<PagedList<OrderItem>> GetOrderItemsAsync(OrderItemQueryDto query)
        {
            var queryable = _dbSet.AsQueryable()
                .Include(oi => oi.Product)
                .Include(oi => oi.CustomDesign)
                .Include(oi => oi.ProductVariant)
                .Include(oi => oi.Order);

            // Apply filters
            if (query.OrderId.HasValue)
                queryable = queryable.Where(oi => oi.OrderId == query.OrderId.Value);

            if (query.ProductId.HasValue)
                queryable = queryable.Where(oi => oi.ProductId == query.ProductId.Value);

            if (query.CustomDesignId.HasValue)
                queryable = queryable.Where(oi => oi.CustomDesignId == query.CustomDesignId.Value);

            if (query.ProductVariantId.HasValue)
                queryable = queryable.Where(oi => oi.ProductVariantId == query.ProductVariantId.Value);

            if (!string.IsNullOrEmpty(query.SelectedColor))
                queryable = queryable.Where(oi => oi.SelectedColor == query.SelectedColor);

            if (!string.IsNullOrEmpty(query.SelectedSize))
                queryable = queryable.Where(oi => oi.SelectedSize == query.SelectedSize);

            if (query.MinUnitPrice.HasValue)
                queryable = queryable.Where(oi => oi.UnitPrice >= query.MinUnitPrice.Value);

            if (query.MaxUnitPrice.HasValue)
                queryable = queryable.Where(oi => oi.UnitPrice <= query.MaxUnitPrice.Value);

            if (query.MinQuantity.HasValue)
                queryable = queryable.Where(oi => oi.Quantity >= query.MinQuantity.Value);

            if (query.MaxQuantity.HasValue)
                queryable = queryable.Where(oi => oi.Quantity <= query.MaxQuantity.Value);

            if (!string.IsNullOrEmpty(query.Search))
            {
                queryable = queryable.Where(oi =>
                    oi.ItemName.Contains(query.Search) ||
                    (oi.Product != null && oi.Product.Name.Contains(query.Search)) ||
                    (oi.CustomDesign != null && oi.CustomDesign.Name.Contains(query.Search)));
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(query.SortBy))
            {
                queryable = query.SortBy.ToLower() switch
                {
                    "itemname" => query.IsDescending ? queryable.OrderByDescending(oi => oi.ItemName) : queryable.OrderBy(oi => oi.ItemName),
                    "unitprice" => query.IsDescending ? queryable.OrderByDescending(oi => oi.UnitPrice) : queryable.OrderBy(oi => oi.UnitPrice),
                    "quantity" => query.IsDescending ? queryable.OrderByDescending(oi => oi.Quantity) : queryable.OrderBy(oi => oi.Quantity),
                    "totalprice" => query.IsDescending ? queryable.OrderByDescending(oi => oi.TotalPrice) : queryable.OrderBy(oi => oi.TotalPrice),
                    "selectedcolor" => query.IsDescending ? queryable.OrderByDescending(oi => oi.SelectedColor) : queryable.OrderBy(oi => oi.SelectedColor),
                    "selectedsize" => query.IsDescending ? queryable.OrderByDescending(oi => oi.SelectedSize) : queryable.OrderBy(oi => oi.SelectedSize),
                    _ => query.IsDescending ? queryable.OrderByDescending(oi => oi.Id) : queryable.OrderBy(oi => oi.Id)
                };
            }

            return await PagedList<OrderItem>.ToPagedListAsync(queryable, query.Page, query.Size);
        }

        public async Task<IEnumerable<OrderItem>> GetByOrderIdAsync(Guid orderId)
        {
            return await _dbSet
                .Include(oi => oi.Product)
                .Include(oi => oi.CustomDesign)
                .Include(oi => oi.ProductVariant)
                .Where(oi => oi.OrderId == orderId)
                .OrderBy(oi => oi.ItemName)
                .ToListAsync();
        }

        public async Task<OrderItem?> GetWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(oi => oi.Product)
                .Include(oi => oi.CustomDesign)
                .Include(oi => oi.ProductVariant)
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.Id == id);
        }

        public async Task<bool> ValidateOrderExistsAsync(Guid orderId)
        {
            return await _context.Set<Order>().AnyAsync(o => o.Id == orderId);
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
            return await _dbSet
                .Where(oi => oi.OrderId == orderId)
                .SumAsync(oi => oi.TotalPrice);
        }

        public async Task<int> GetOrderItemCountAsync(Guid orderId)
        {
            return await _dbSet
                .Where(oi => oi.OrderId == orderId)
                .SumAsync(oi => oi.Quantity);
        }
    }
}