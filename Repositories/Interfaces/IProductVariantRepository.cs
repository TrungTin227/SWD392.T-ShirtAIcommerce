using BusinessObjects.Products;
using Repositories.Helpers;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface IProductVariantRepository : IGenericRepository<ProductVariant, Guid>
    {
        Task<IReadOnlyList<ProductVariant>> GetVariantsByProductIdAsync(Guid productId);
        Task<PagedList<ProductVariant>> GetPagedVariantsByProductIdAsync(Guid productId, int pageNumber, int pageSize);
        Task<ProductVariant?> GetByIdWithProductAsync(Guid variantId);
    }
}