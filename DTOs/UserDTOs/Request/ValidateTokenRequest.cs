using System.ComponentModel.DataAnnotations;

namespace DTOs.UserDTOs.Request
{
    public class ValidateTokenRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}