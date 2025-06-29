using BusinessObjects.Products;
using DTOs.Category;
using Repositories.Helpers;
using Repositories.WorkSeeds.Implements;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Implementations
{
    public class CategoryRepository : GenericRepository<Category, Guid>, ICategoryRepository
    {
        public CategoryRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<PagedList<Category>> GetPagedAsync(CategoryFilterDto filter)
        {
            var query = GetQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                query = query.Where(x => x.Name.Contains(filter.Name));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == filter.IsActive.Value);
            }

            if (filter.CreatedFrom.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= filter.CreatedTo.Value);
            }

            // Exclude deleted items
            query = query.Where(x => !x.IsDeleted);

            // Apply sorting
            query = query.OrderByDescending(x => x.CreatedAt);

            return await PagedList<Category>.ToPagedListAsync(query, filter.PageNumber, filter.PageSize);
        }

        public async Task<Category?> GetByNameAsync(string name)
        {
            return await FirstOrDefaultAsync(x => x.Name == name && !x.IsDeleted);
        }

        public async Task<bool> IsNameExistsAsync(string name, Guid? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await AnyAsync(x => x.Name == name && !x.IsDeleted && x.Id != excludeId.Value);
            }

            return await AnyAsync(x => x.Name == name && !x.IsDeleted);
        }
    }
}