using BusinessObjects.Identity;
using DTOs.UserDTOs.Identities;
using Repositories.Commons;

namespace Services.Interfaces
{
    public interface ITokenService
    {
        Task<ApiResult<string>> GenerateToken(ApplicationUser user);
        RefreshTokenInfo GenerateRefreshToken();
    }
}
