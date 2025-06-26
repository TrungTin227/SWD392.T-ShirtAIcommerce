using BusinessObjects.Shipping;
using DTOs.Shipping;
using Repositories.Helpers;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface IShippingMethodRepository : IGenericRepository<ShippingMethod, Guid>
    {
        Task<PagedList<ShippingMethod>> GetShippingMethodsAsync(ShippingMethodFilterRequest filter);
        Task<IEnumerable<ShippingMethod>> GetActiveShippingMethodsAsync();
        Task<ShippingMethod?> GetShippingMethodWithDetailsAsync(Guid id);
        Task<bool> IsNameExistsAsync(string name, Guid? excludeId = null);
        Task<IEnumerable<ShippingMethod>> GetShippingMethodsBySortOrderAsync();
        Task<bool> UpdateSortOrderAsync(Guid id, int newSortOrder);
        Task<bool> ToggleActiveStatusAsync(Guid id, bool isActive, Guid? updatedBy = null);
        Task<decimal> CalculateShippingFeeAsync(Guid shippingMethodId, decimal orderAmount);
        Task<bool> IsShippingMethodUsedInOrdersAsync(Guid id);
    }
}