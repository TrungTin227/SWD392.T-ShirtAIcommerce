using BusinessObjects.Common;
using BusinessObjects.CustomDesigns;
using DTOs.CustomDesigns;
using Repositories.Commons;
using Repositories.Helpers;


namespace Services.Interfaces
{
    public interface ICustomDesignedService
    {
        Task<PagedList<CustomDesign>> GetCustomDesignsAsync(CustomDesignFilterRequest filter);
        Task<bool> UpdateStatusAsync(Guid id, CustomDesignStatus status);

        Task<PagedList<CustomDesign>> GetCustomDesignsByIDAsync(CustomDesignFilterRequest filter);
        Task<CustomDesign> CreateAsync(CustomDesign entity);
        Task<CustomDesign?> GetByIdAsync(Guid id);
        Task<IEnumerable<CustomDesign>> GetByUserIdAsync(Guid userId);
        Task UpdateAsync(CustomDesign entity);
        Task DeleteAsync(Guid id);      // Xóa mềm
        Task ShowAsync(Guid id);        // Hiện mẫu
        Task HideAsync(Guid id);        // Ẩn mẫu
    }
}