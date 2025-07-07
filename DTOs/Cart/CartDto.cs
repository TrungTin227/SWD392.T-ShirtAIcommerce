namespace DTOs.Cart
{
    public class CartDto
    {
        public Guid? UserId { get; set; }
        public string? SessionId { get; set; }
        public string? UserName { get; set; }

        public List<CartItemDto> Items { get; set; } = new();

        public decimal Subtotal => Items?.Sum(i => i.TotalPrice) ?? 0;
        public int TotalItems => Items?.Sum(i => i.Quantity) ?? 0;
    }

}
