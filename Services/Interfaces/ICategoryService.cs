using DTOs.Category;
using DTOs.Common;
using Repositories.Commons;

namespace Services.Interfaces
{
    public interface ICategoryService
    {
        Task<ApiResult<PagedResponse<CategoryDto>>> GetPagedAsync(CategoryFilterDto filter);
        Task<ApiResult<CategoryDto>> GetByIdAsync(Guid id);
        Task<ApiResult<CategoryDto>> CreateAsync(CreateCategoryDto dto);
        Task<ApiResult<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto);
        Task<ApiResult<bool>> DeleteAsync(Guid id);
        Task<ApiResult<BatchOperationResultDTO>> BulkDeleteAsync(BatchIdsRequest request);
        Task<ApiResult<BatchOperationResultDTO>> BulkRestoreAsync(BatchIdsRequest request);
    }
}