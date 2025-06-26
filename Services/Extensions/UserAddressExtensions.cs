using BusinessObjects.Identity;
using DTOs.UserAddressDTOs.Request;
using DTOs.UserAddressDTOs.Response;

namespace Services.Extensions
{
    public static class UserAddressExtensions
    {
        // Request to Entity mappings
        public static UserAddress ToUserAddress(this CreateUserAddressRequest request, Guid userId)
        {
            return new UserAddress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReceiverName = request.ReceiverName,
                Phone = request.Phone,
                DetailAddress = request.DetailAddress,
                Ward = request.Ward,
                District = request.District,
                Province = request.Province,
                PostalCode = request.PostalCode,
                IsDefault = request.IsDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static void ApplyToUserAddress(this UpdateUserAddressRequest request, UserAddress userAddress)
        {
            userAddress.ReceiverName = request.ReceiverName;
            userAddress.Phone = request.Phone;
            userAddress.DetailAddress = request.DetailAddress;
            userAddress.Ward = request.Ward;
            userAddress.District = request.District;
            userAddress.Province = request.Province;
            userAddress.PostalCode = request.PostalCode;
            userAddress.IsDefault = request.IsDefault;
            userAddress.UpdatedAt = DateTime.UtcNow;
        }

        // Entity to Response mappings
        public static UserAddressResponse ToUserAddressResponse(this UserAddress userAddress)
        {
            return new UserAddressResponse
            {
                Id = userAddress.Id,
                UserId = userAddress.UserId,
                ReceiverName = userAddress.ReceiverName,
                Phone = userAddress.Phone,
                DetailAddress = userAddress.DetailAddress,
                Ward = userAddress.Ward,
                District = userAddress.District,
                Province = userAddress.Province,
                PostalCode = userAddress.PostalCode,
                IsDefault = userAddress.IsDefault,
                CreatedAt = userAddress.CreatedAt,
                UpdatedAt = userAddress.UpdatedAt
            };
        }
    }
}