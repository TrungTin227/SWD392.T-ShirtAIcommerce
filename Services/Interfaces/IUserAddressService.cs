using DTOs.UserAddressDTOs.Request;
using DTOs.UserAddressDTOs.Response;
using Repositories.Commons;

namespace Services.Interfaces
{
    public interface IUserAddressService
    {
        Task<ApiResult<UserAddressResponse>> CreateUserAddressAsync(CreateUserAddressRequest request);
        Task<ApiResult<UserAddressResponse>> UpdateUserAddressAsync(Guid addressId, UpdateUserAddressRequest request);
        Task<ApiResult<bool>> DeleteUserAddressAsync(Guid addressId);
        Task<ApiResult<IEnumerable<UserAddressResponse>>> GetUserAddressesAsync();
        Task<ApiResult<UserAddressResponse>> GetUserAddressByIdAsync(Guid addressId);
        Task<ApiResult<UserAddressResponse>> GetDefaultAddressAsync();
        Task<ApiResult<bool>> SetDefaultAddressAsync(Guid addressId);
        Task<ApiResult<UserAddressResponse>> CreateDefaultAddressForNewUserAsync(Guid userId, CreateUserAddressRequest? request = null);
    }
}