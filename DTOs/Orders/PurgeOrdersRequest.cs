using System.ComponentModel.DataAnnotations;

namespace DTOs.Orders
{
    public class PurgeOrdersRequest
    {
        /// <summary>
        /// Xóa các đơn hàng đã hoàn thành cũ hơn số ngày này.
        /// </summary>
        [Range(1, 3650, ErrorMessage = "Số ngày phải từ 1 đến 3650.")]
        public int DaysOld { get; set; }
    }
}
