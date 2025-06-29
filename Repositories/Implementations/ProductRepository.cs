using BusinessObjects.Products;
using DTOs.Product;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.WorkSeeds.Implements;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Implementations
{
    public class ProductRepository : GenericRepository<Product, Guid>, IProductRepository
    {
        public ProductRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<PagedList<Product>> GetPagedAsync(ProductFilterDto filter)
        {
            // 1. Khởi tạo query với kiểu IQueryable<Product>
            IQueryable<Product> query = GetQueryable();

            // 2. Include quan hệ Category (được gắn nhãn ở đây, vẫn trả về IQueryable<Product>)
            query = query.Include(p => p.Category);

            // 3. Áp dụng các bộ lọc (filter)
            if (!string.IsNullOrWhiteSpace(filter.Name))
                query = query.Where(x => x.Name.Contains(filter.Name));

            if (!string.IsNullOrWhiteSpace(filter.Sku))
                query = query.Where(x => x.Sku != null && x.Sku.Contains(filter.Sku));

            if (filter.CategoryId.HasValue)
                query = query.Where(x => x.CategoryId == filter.CategoryId.Value);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            if (filter.MinPrice.HasValue)
                query = query.Where(x => x.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(x => x.Price <= filter.MaxPrice.Value);

            if (filter.IsFeatured.HasValue)
                query = query.Where(x => x.IsFeatured == filter.IsFeatured.Value);

            if (filter.IsBestseller.HasValue)
                query = query.Where(x => x.IsBestseller == filter.IsBestseller.Value);

            if (filter.InStock.HasValue)
                query = filter.InStock.Value
                    ? query.Where(x => x.Quantity > 0)
                    : query.Where(x => x.Quantity == 0);

            if (!string.IsNullOrWhiteSpace(filter.Material))
                query = query.Where(x => x.Material != null && x.Material.Contains(filter.Material));

            if (!string.IsNullOrWhiteSpace(filter.Season))
                query = query.Where(x => x.Season != null && x.Season.Contains(filter.Season));

            if (filter.CreatedFrom.HasValue)
                query = query.Where(x => x.CreatedAt >= filter.CreatedFrom.Value);

            if (filter.CreatedTo.HasValue)
                query = query.Where(x => x.CreatedAt <= filter.CreatedTo.Value);

            // 4. Luôn loại bỏ bản ghi đã đánh dấu xoá
            query = query.Where(x => !x.IsDeleted);

            // 5. Áp dụng phân trang và sắp xếp
            query = ApplySorting(query, filter.SortBy, filter.SortDirection);

            // 6. Trả về kết quả phân trang
            return await PagedList<Product>.ToPagedListAsync(query, filter.PageNumber, filter.PageSize);
        }
        public async Task<Product?> GetBySkuAsync(string sku)
        {
            return await FirstOrDefaultAsync(x => x.Sku == sku && !x.IsDeleted);
        }

        public async Task<bool> IsSkuExistsAsync(string sku, Guid? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await AnyAsync(x => x.Sku == sku && !x.IsDeleted && x.Id != excludeId.Value);
            }

            return await AnyAsync(x => x.Sku == sku && !x.IsDeleted);
        }

        public async Task<bool> IsSlugExistsAsync(string slug, Guid? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await AnyAsync(x => x.Slug == slug && !x.IsDeleted && x.Id != excludeId.Value);
            }

            return await AnyAsync(x => x.Slug == slug && !x.IsDeleted);
        }

        public async Task<List<Product>> GetBestSellersAsync(int count = 10)
        {
            return await GetQueryable()
                .Where(x => !x.IsDeleted && x.Status == ProductStatus.Active)
                .OrderByDescending(x => x.SoldCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Product>> GetFeaturedAsync(int count = 10)
        {
            return await GetQueryable()
                .Where(x => !x.IsDeleted && x.Status == ProductStatus.Active && x.IsFeatured)
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task UpdateViewCountAsync(Guid id)
        {
            var product = await GetByIdAsync(id);
            if (product != null && !product.IsDeleted)
            {
                product.ViewCount++;
                await UpdateAsync(product);
            }
        }

        public async Task UpdateSoldCountAsync(Guid id, int quantity)
        {
            var product = await GetByIdAsync(id);
            if (product != null && !product.IsDeleted)
            {
                product.SoldCount += quantity;
                if (product.Quantity >= quantity)
                {
                    product.Quantity -= quantity;
                }
                await UpdateAsync(product);
            }
        }

        private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy, string? sortDirection)
        {
            var isDescending = sortDirection?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "name" => isDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                "price" => isDescending ? query.OrderByDescending(x => x.Price) : query.OrderBy(x => x.Price),
                "soldcount" => isDescending ? query.OrderByDescending(x => x.SoldCount) : query.OrderBy(x => x.SoldCount),
                "viewcount" => isDescending ? query.OrderByDescending(x => x.ViewCount) : query.OrderBy(x => x.ViewCount),
                _ => isDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
            };
        }
    }
}