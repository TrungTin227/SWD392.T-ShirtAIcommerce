using DTOs.UserDTOs.Identities;
using DTOs.UserDTOs.Request;
using DTOs.UserDTOs.Response;
using Repositories.Commons;
using Repositories.Helpers;
using UserDTOs.DTOs.Response;

namespace Services.Interfaces
{
    public interface IUserService
    {
        // Existing methods
        Task<ApiResult<string>> ConfirmEmailAsync(Guid userId, string encodedToken);
        Task<ApiResult<string>> ResendConfirmationEmailAsync(string email);
        Task<ApiResult<string>> InitiatePasswordResetAsync(ForgotPasswordRequestDTO request);
        Task<ApiResult<string>> ResetPasswordAsync(ResetPasswordRequestDTO request);
        Task<ApiResult<string>> Send2FACodeAsync();
        Task<ApiResult<UserResponse>> AdminRegisterAsync(AdminCreateUserRequest req);
        Task<ApiResult<UserResponse>> LoginAsync(UserLoginRequest req);
        Task<ApiResult<UserResponse>> GetByIdAsync(Guid id);
        Task<ApiResult<CurrentUserResponse>> GetCurrentUserAsync();
        Task<ApiResult<CurrentUserResponse>> RefreshTokenAsync(RefreshTokenRequest req);
        Task<ApiResult<RevokeRefreshTokenResponse>> RevokeRefreshTokenAsync(RefreshTokenRequest req);
        Task<ApiResult<string>> ChangePasswordAsync(ChangePasswordRequest req);
        Task<ApiResult<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest req);
        Task<ApiResult<UserResponse>> UpdateCurrentUserAsync(UpdateUserRequest req);
        Task<ApiResult<UserResponse>> LockUserAsync(Guid id);
        Task<ApiResult<UserResponse>> UnlockUserAsync(Guid id);
        Task<ApiResult<object>> DeleteUsersAsync(List<Guid> ids);
        Task<UserResponse> CreateOrUpdateGoogleUserAsync(GoogleUserInfo info);
        Task<ApiResult<PagedList<UserDetailsDTO>>> GetUsersAsync(int page, int size);

        // New methods for AuthController
        Task<ApiResult<string>> LogoutAsync(LogoutRequest request);
        Task<ApiResult<ValidateTokenResponse>> ValidateTokenAsync(string token);
        Task<ApiResult<string>> Verify2FAAsync(Verify2FARequest request);
        Task<ApiResult<UserResponse>> RegisterAsync(UserRegisterRequest req);

    }
}