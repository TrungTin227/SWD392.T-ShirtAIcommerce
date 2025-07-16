using BusinessObjects.Common;
using BusinessObjects.CustomDesigns;
using DTOs.CustomDesigns;
using Repositories.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Data.Repositories.CustomDesigns
{
    public interface IAICustomDesignRepository
    {
        Task<PagedList<CustomDesign>> GetCustomDesignsAsync(CustomDesignFilterRequest filter);
        Task<bool> UpdateStatusAsync(Guid id, CustomDesignStatus status);
        Task<CustomDesign> CreateAsync(CustomDesign entity);

        Task<PagedList<CustomDesign>> GetCustomDesignsByIDAsync(CustomDesignFilterRequest filter);
        Task<CustomDesign?> GetByIdAsync(Guid id);
        Task<IEnumerable<CustomDesign>> GetByUserIdAsync(Guid userId);
        Task UpdateAsync(CustomDesign entity);
        Task DeleteAsync(Guid id);      // Xóa mềm (IsDeleted)
        Task ShowAsync(Guid id);        // Đổi trạng thái IsDeleted = false
        Task HideAsync(Guid id);        // Đổi trạng thái IsDeleted = true
    }
}
