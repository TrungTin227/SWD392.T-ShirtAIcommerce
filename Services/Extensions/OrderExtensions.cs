using BusinessObjects.Orders;

namespace Services.Extensions
{
    public static class OrderExtensions
    {
        public static bool CanBeUpdated(this Order order)
        {
            return order.Status != OrderStatus.Delivered &&
                   order.Status != OrderStatus.Cancelled;
        }

        public static bool CanBeCancelled(this Order order)
        {
            return order.Status != OrderStatus.Delivered &&
                   order.Status != OrderStatus.Cancelled;
        }

        public static bool IsRefundable(this Order order)
        {
            return order.PaymentStatus == PaymentStatus.Paid &&
                   (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Returned);
        }

        public static decimal GetFinalTotal(this Order order)
        {
            return order.TotalAmount + order.ShippingFee + order.TaxAmount - order.DiscountAmount;
        }

        public static bool RequiresShipping(this Order order)
        {
            return !string.IsNullOrEmpty(order.ShippingAddress);
        }

        public static int GetEstimatedDeliveryDays(this Order order)
        {
            return order.ShippingMethod?.EstimatedDays ?? 7; // Default 7 days
        }
    }
}