namespace DTOs.UserDTOs.Response
{
    public class ValidateTokenResponse
    {
        public bool IsValid { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
    }
}