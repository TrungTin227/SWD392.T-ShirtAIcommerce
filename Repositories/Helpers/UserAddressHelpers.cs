using BusinessObjects.Identity;

namespace Repositories.Helpers
{
    public static class UserAddressHelpers
    {
        public static string FormatFullAddress(this UserAddress address)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(address.DetailAddress))
                parts.Add(address.DetailAddress);
            if (!string.IsNullOrEmpty(address.Ward))
                parts.Add(address.Ward);
            if (!string.IsNullOrEmpty(address.District))
                parts.Add(address.District);
            if (!string.IsNullOrEmpty(address.Province))
                parts.Add(address.Province);

            return string.Join(", ", parts);
        }

        public static bool IsCompleteAddress(this UserAddress address)
        {
            return !string.IsNullOrEmpty(address.ReceiverName) &&
                   !string.IsNullOrEmpty(address.Phone) &&
                   !string.IsNullOrEmpty(address.DetailAddress) &&
                   !string.IsNullOrEmpty(address.Ward) &&
                   !string.IsNullOrEmpty(address.District) &&
                   !string.IsNullOrEmpty(address.Province);
        }

        public static UserAddress CreateDefaultAddress(Guid userId, string receiverName, string phone)
        {
            return new UserAddress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReceiverName = receiverName,
                Phone = phone,
                DetailAddress = "Chưa cập nhật",
                Ward = "Chưa cập nhật",
                District = "Chưa cập nhật",
                Province = "Chưa cập nhật",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}