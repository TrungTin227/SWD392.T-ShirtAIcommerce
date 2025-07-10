using BusinessObjects.Common;
using BusinessObjects.Products;
using DTOs.Product;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class ProductRepository : GenericRepository<Product, Guid>, IProductRepository
    {
        public ProductRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<PagedList<Product>> GetPagedAsync(ProductFilterDto filter)
        {
            IQueryable<Product> query = GetQueryable();

            query = query.Include(p => p.Category)
                         .Include(p => p.Images)
                         .Include(p => p.Variants);   

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

            if (filter.CreatedFrom.HasValue)
                query = query.Where(x => x.CreatedAt >= filter.CreatedFrom.Value);

            if (filter.CreatedTo.HasValue)
                query = query.Where(x => x.CreatedAt <= filter.CreatedTo.Value);

            query = query.Where(x => !x.IsDeleted);

            query = ApplySorting(query, filter.SortBy, filter.SortDirection);

            return await PagedList<Product>.ToPagedListAsync(query, filter.PageNumber, filter.PageSize);
        }

        public async Task<Product?> GetBySkuAsync(string sku)
        {
            return await _dbSet
                .Include(p => p.Images)
                .FirstOrDefaultAsync(x => x.Sku == sku && !x.IsDeleted);
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

        // Trả về sản phẩm có nhiều variant bán chạy nhất (có thể cần custom thêm logic)
        public async Task<List<Product>> GetBestSellersAsync(int count = 10)
        {
            // Nếu muốn sort theo tổng sold count từ các variant, cần điều chỉnh lại model và select cho phù hợp
            return await GetQueryable()
                .Where(x => !x.IsDeleted && x.Status == ProductStatus.Active)
                .Include(x => x.Variants)
                // .OrderByDescending(x => x.Variants.Sum(v => v.SoldCount)) // nếu ProductVariant có SoldCount
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        // Trả về sản phẩm nổi bật (nếu cần trường IsFeatured thì thêm vào Product)
        public async Task<List<Product>> GetFeaturedAsync(int count = 10)
        {
            return await GetQueryable()
                .Where(x => !x.IsDeleted && x.Status == ProductStatus.Active)
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy, string? sortDirection)
        {
            var isDescending = sortDirection?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "name" => isDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                "price" => isDescending ? query.OrderByDescending(x => x.Price) : query.OrderBy(x => x.Price),
                _ => isDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
            };
        }
    }
}