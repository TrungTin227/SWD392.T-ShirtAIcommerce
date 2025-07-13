using DTOs.Payments;

namespace DTOs.Orders
{
    public class CreateOrderResult
    {
        public OrderDTO Order { get; set; } = null!;
        public PaymentResponse Payment { get; set; } = null!;
        public string? PaymentUrl { get; set; }    // <— thêm trường này

    }
}
