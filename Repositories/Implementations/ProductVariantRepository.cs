using BusinessObjects.Products;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class ProductVariantRepository : GenericRepository<ProductVariant, Guid>, IProductVariantRepository
    {
        public ProductVariantRepository(T_ShirtAIcommerceContext context)
            : base(context)
        {
        }

        public async Task<IReadOnlyList<ProductVariant>> GetVariantsByProductIdAsync(Guid productId)
        {
            return await _dbSet
                .Where(x => x.ProductId == productId && !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<PagedList<ProductVariant>> GetPagedVariantsByProductIdAsync(Guid productId, int pageNumber, int pageSize)
        {
            var query = _dbSet
                .Where(x => x.ProductId == productId && !x.IsDeleted);
            return await PagedList<ProductVariant>.ToPagedListAsync(query, pageNumber, pageSize);
        }
        public async Task<ProductVariant?> GetByIdWithProductAsync(Guid variantId)
        {
            return await _dbSet
                .Include(x => x.Product)
                .Where(x => x.Id == variantId && !x.IsDeleted && !x.Product.IsDeleted)
                .FirstOrDefaultAsync();
        }
    }
}
