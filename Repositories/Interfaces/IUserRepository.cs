using BusinessObjects.Identity;
using DTOs.UserDTOs.Response;
using Repositories.Helpers;

namespace Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<PagedList<UserDetailsDTO>> GetUserDetailsAsync(int pageNumber, int pageSize);
        Task<bool> ExistsByEmailAsync(string email);
        Task<ApplicationUser> GetUserDetailsByIdAsync(Guid id);
        Task<bool> ExistsByUsernameAsync(string username);
        Task<bool> ExistsAsync(Guid userId);
    }
}
