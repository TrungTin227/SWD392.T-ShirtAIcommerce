using DTOs.Products;
using Repositories.Helpers;
using Repositories.Commons;

namespace Services.Interfaces
{
    public interface IProductVariantService
    {
        Task<ApiResult<ProductVariantDto>> CreateAsync(ProductVariantCreateDto dto);
        Task<ApiResult<ProductVariantDto>> UpdateAsync(ProductVariantUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(Guid id);
        Task<ApiResult<ProductVariantDto?>> GetByIdAsync(Guid id);
        Task<ApiResult<IReadOnlyList<ProductVariantDto>>> GetVariantsByProductIdAsync(Guid productId);
        Task<ApiResult<PagedList<ProductVariantDto>>> GetPagedVariantsByProductIdAsync(Guid productId, int pageNumber, int pageSize);
        Task<ApiResult<PagedList<ProductVariantDto>>> GetPagedAsync(int pageNumber, int pageSize);
        Task<ApiResult<bool>> BulkCreateAsync(IEnumerable<ProductVariantCreateDto> dtos);
        Task<ApiResult<bool>> BulkUpdateAsync(IEnumerable<ProductVariantUpdateDto> dtos);
        Task<ApiResult<bool>> BulkDeleteAsync(IEnumerable<Guid> ids);
    }
}