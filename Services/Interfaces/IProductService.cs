using DTOs.Product;
using DTOs.Common;
using Repositories.Commons;
using Repositories.Helpers;

namespace Services.Interfaces
{
    public interface IProductService
    {
        Task<ApiResult<PagedResponse<ProductDto>>> GetPagedAsync(ProductFilterDto filter);
        Task<ApiResult<ProductDto>> GetByIdAsync(Guid id);
        Task<ApiResult<ProductDto>> GetBySkuAsync(string sku);
        Task<ApiResult<ProductDto>> CreateAsync(CreateProductDto dto);
        Task<ApiResult<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto);
        Task<ApiResult<BatchOperationResultDTO>> BulkDeleteAsync(BatchIdsRequest request);
        Task<ApiResult<BatchOperationResultDTO>> BulkRestoreAsync(BatchIdsRequest request);
        Task<ApiResult<List<ProductDto>>> GetBestSellersAsync(int count = 10);
        Task<ApiResult<List<ProductDto>>> GetFeaturedAsync(int count = 10);
        Task<ApiResult<bool>> UpdateViewCountAsync(Guid id);
    }
}