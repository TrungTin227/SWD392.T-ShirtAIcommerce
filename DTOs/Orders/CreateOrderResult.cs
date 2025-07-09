using DTOs.Payments;

namespace DTOs.Orders
{
    public class CreateOrderResult
    {
        public OrderDTO Order { get; set; } = null!;
        public PaymentResponse? Payment { get; set; }
        public string? RedirectUrl { get; set; }  
    }
}
