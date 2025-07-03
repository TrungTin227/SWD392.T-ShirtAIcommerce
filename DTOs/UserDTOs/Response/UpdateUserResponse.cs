using DTOs.UserAddressDTOs.Response;

namespace DTOs.UserDTOs.Response
{
    public class UpdateUserResponse
    {
        /// <summary>
        /// Thông tin cơ bản của user (Id, tên, email, roles, v.v…)
        /// </summary>
        public UserDetailsDTO User { get; set; } = new UserDetailsDTO();

        /// <summary>
        /// Danh sách địa chỉ cập nhật (có thể rỗng)
        /// </summary>
        public List<UserAddressResponse> Addresses { get; set; }
            = new List<UserAddressResponse>();
    }
}
