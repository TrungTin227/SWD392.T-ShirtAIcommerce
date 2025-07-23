using DTOs.CustomOrder;
using DTOs.Payments.VnPay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ICustomDesignPaymentService
    {
        Task<CustomDesignPaymentResponse> CreateCustomDesignPaymentAsync(
            CustomDesignPaymentCreateRequest req, string ipAddress);
    }

}
