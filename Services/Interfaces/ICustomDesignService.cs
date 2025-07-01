using DTOs.CustomDesigns;
using Repositories.Commons;
using Repositories.Helpers;

namespace Services.Interfaces
{
    public interface ICustomDesignService
    {
        Task<ApiResult<CustomDesignDto>> GetByIdAsync(Guid id);
        Task<ApiResult<PagedList<CustomDesignDto>>> GetCustomDesignsAsync(CustomDesignFilterDto filter);
        Task<ApiResult<List<CustomDesignDto>>> GetUserDesignsAsync(Guid userId);
        Task<ApiResult<List<CustomDesignDto>>> GetStaffDesignsAsync(Guid staffId);
        Task<ApiResult<List<CustomDesignDto>>> GetPendingDesignsAsync();
        Task<ApiResult<CustomDesignDto>> CreateCustomDesignAsync(CreateCustomDesignDto createDto);
        Task<ApiResult<CustomDesignDto>> UpdateCustomDesignAsync(Guid id, UpdateCustomDesignDto updateDto);
        Task<ApiResult<CustomDesignDto>> AdminUpdateCustomDesignAsync(Guid id, AdminUpdateCustomDesignDto updateDto);
        Task<ApiResult<bool>> DeleteCustomDesignAsync(Guid id);
        Task<ApiResult<CustomDesignDto>> SubmitDesignAsync(Guid id);
        Task<ApiResult<CustomDesignDto>> ApproveDesignAsync(Guid id);
        Task<ApiResult<CustomDesignDto>> RejectDesignAsync(Guid id, string reason);
        Task<ApiResult<CustomDesignDto>> StartProductionAsync(Guid id);
        Task<ApiResult<CustomDesignDto>> CompleteDesignAsync(Guid id);
        Task<ApiResult<CustomDesignStatsDto>> GetDesignStatsAsync();
        Task<ApiResult<CustomDesignStatsDto>> GetUserDesignStatsAsync(Guid userId);
        Task<ApiResult<CustomDesignStatsDto>> GetStaffDesignStatsAsync(Guid staffId);
        Task<ApiResult<DesignPricingDto>> CalculateDesignPricingAsync(CreateCustomDesignDto designDto);
        Task<ApiResult<bool>> AssignStaffToDesignAsync(Guid designId, Guid staffId);
    }
}