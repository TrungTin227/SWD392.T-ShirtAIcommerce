namespace DTOs.Cart
{
    public class CartSummaryDto
    {
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
        public decimal SubTotal { get; set; }
        public decimal EstimatedShipping { get; set; }
        public decimal EstimatedTax { get; set; }
        public decimal EstimatedTotal { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }
}