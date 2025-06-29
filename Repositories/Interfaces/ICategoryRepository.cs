using BusinessObjects.Products;
using DTOs.Category;
using Repositories.Helpers;

namespace Repositories.WorkSeeds.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category, Guid>
    {
        Task<PagedList<Category>> GetPagedAsync(CategoryFilterDto filter);
        Task<Category?> GetByNameAsync(string name);
        Task<bool> IsNameExistsAsync(string name, Guid? excludeId = null);
    }
}