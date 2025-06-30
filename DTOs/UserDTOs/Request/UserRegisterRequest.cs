using BusinessObjects.Common;
using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;

namespace DTOs.UserDTOs.Request
{
    public class UserRegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; } = string.Empty;
        [MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        public string LastName { get; set; }

        public Gender Gender { get; set; }
    }
}
