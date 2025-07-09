using BusinessObjects.Common;
using BusinessObjects.CustomDesigns;
using DTOs.CustomDesigns;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface ICustomDesignRepository : IGenericRepository<CustomDesign, Guid>
    {
        Task<IEnumerable<CustomDesign>> GetDesignsByUserIdAsync(Guid userId);
        Task<IEnumerable<CustomDesign>> GetDesignsByStaffIdAsync(Guid staffId);
        Task<IEnumerable<CustomDesign>> GetPendingDesignsAsync();
        Task<CustomDesignStatsDto> GetDesignStatsAsync();
        Task<CustomDesignStatsDto> GetUserDesignStatsAsync(Guid userId);
        Task<CustomDesignStatsDto> GetStaffDesignStatsAsync(Guid staffId);
        Task<IEnumerable<CustomDesign>> GetDesignsByStatusAsync(DesignStatus status);
        Task<decimal> CalculateDesignPriceAsync(CreateCustomDesignDto designDto);
        Task<bool> AssignStaffToDesignAsync(Guid designId, Guid staffId);
    }
}