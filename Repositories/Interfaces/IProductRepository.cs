using BusinessObjects.Products;
using Repositories.Helpers;
using DTOs.Product;

namespace Repositories.WorkSeeds.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product, Guid>
    {
        Task<PagedList<Product>> GetPagedAsync(ProductFilterDto filter);
        Task<Product?> GetBySkuAsync(string sku);
        Task<bool> IsSkuExistsAsync(string sku, Guid? excludeId = null);
        Task<bool> IsSlugExistsAsync(string slug, Guid? excludeId = null);
        Task<List<Product>> GetBestSellersAsync(int count = 10);
        Task<List<Product>> GetFeaturedAsync(int count = 10);
        Task UpdateViewCountAsync(Guid id);
        Task UpdateSoldCountAsync(Guid id, int quantity);
    }
}