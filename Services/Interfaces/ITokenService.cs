using BusinessObjects.Common;
using BusinessObjects.Identity;
using DTOs.UserDTOs.Identities;

namespace Services.Interfaces
{
    public interface ITokenService
    {
        Task<ApiResult<string>> GenerateToken(ApplicationUser user);
        RefreshTokenInfo GenerateRefreshToken();
    }
}
