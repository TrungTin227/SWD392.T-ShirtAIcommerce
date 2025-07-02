using DTOs.Common;
using DTOs.Shipping;
using Repositories.Commons;
using Repositories.Helpers;

namespace Services.Interfaces
{
    public interface IShippingMethodService
    {
        Task<PagedResponse<ShippingMethodDTO>> GetShippingMethodsAsync(ShippingMethodFilterRequest filter);
        Task<ShippingMethodDTO?> GetShippingMethodByIdAsync(Guid id);
        Task<ShippingMethodDTO?> CreateShippingMethodAsync(CreateShippingMethodRequest request);
        Task<ShippingMethodDTO?> UpdateShippingMethodAsync(Guid id, UpdateShippingMethodRequest request);
        Task<bool> DeleteShippingMethodAsync(Guid id);
        Task<IEnumerable<ShippingMethodDTO>> GetActiveShippingMethodsAsync();
        Task<bool> ToggleActiveStatusAsync(Guid id, bool isActive);
        Task<bool> UpdateSortOrderAsync(Guid id, int newSortOrder);
        Task<decimal> CalculateShippingFeeAsync(Guid shippingMethodId, decimal orderAmount);
        Task<ApiResult<string>> ValidateShippingMethodAsync(Guid id);
        Task<ApiResult<ShippingMethodDTO>> GetByIdAsync(Guid id);
    }
}