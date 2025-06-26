namespace DTOs.UserAddressDTOs.Response
{
    public class UserAddressResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string DetailAddress { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string FullAddress => $"{DetailAddress}, {Ward}, {District}, {Province}";
    }
}