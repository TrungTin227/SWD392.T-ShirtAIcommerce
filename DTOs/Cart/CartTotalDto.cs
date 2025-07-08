namespace DTOs.Cart
{
    public class CartTotalDto
    {
        public List<CartItemDto> Items { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
