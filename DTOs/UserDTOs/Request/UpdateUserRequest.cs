using BusinessObjects.Common;
using DTOs.UserAddressDTOs.Request;

namespace DTOs.UserDTOs.Request
{
    public class UpdateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<UpdateUserAddressRequest> Addresses { get; set; }  = new List<UpdateUserAddressRequest>();
    }
}
