using System.ComponentModel.DataAnnotations;

namespace DTOs.UserDTOs.Request
{
    public class Verify2FARequest
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }
}