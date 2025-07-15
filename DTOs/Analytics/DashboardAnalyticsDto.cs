namespace DTOs.Analytics
{
    public class DashboardAnalyticsDto
    {
        /// <summary>
        /// Tổng doanh thu từ các đơn hàng đã hoàn thành (Status = Completed).
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Số lượng đơn hàng được tạo trong ngày hôm nay.
        /// </summary>
        public int OrdersToday { get; set; }

        /// <summary>
        /// Số lượng đơn hàng được tạo trong tuần này.
        /// </summary>
        public int OrdersThisWeek { get; set; }

        /// <summary>
        /// Thống kê chi tiết về trạng thái thanh toán của các đơn hàng.
        /// </summary>
        public PaymentStatusRatioDto PaymentStatusRatio { get; set; }
    }

    public class PaymentStatusRatioDto
    {
        /// <summary>
        /// Số lượng đơn đã thanh toán (Completed).
        /// </summary>
        public int PaidCount { get; set; }

        /// <summary>
        /// Số lượng đơn chưa thanh toán (Unpaid).
        /// </summary>
        public int UnpaidCount { get; set; }

        /// <summary>
        /// Số lượng đơn đang chờ xử lý thanh toán (Pending).
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// Số lượng đơn đã hoàn tiền (Refunded).
        /// </summary>
        public int RefundedCount { get; set; }

        /// <summary>
        /// Tổng số đơn hàng có trạng thái thanh toán.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Tỷ lệ phần trăm đơn hàng đã thanh toán so với tổng số.
        /// </summary>
        public double PaidPercentage { get; set; }
    }
}