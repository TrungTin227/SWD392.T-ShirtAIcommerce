using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CustomOrder
{
    public class CustomOrderCreateDto
    {
        public string? ShippingAddress { get; set; }   // Null thì lấy từ user
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public int Quantity { get; set; } = 1;
        public PaymentMethod PaymentMethod { get; set; }

    }
}
