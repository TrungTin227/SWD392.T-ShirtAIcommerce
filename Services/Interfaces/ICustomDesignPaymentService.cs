using DTOs.CustomOrder;
using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Http;
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
        Task<bool> HandleVnPayCallbackAsync(VnPayCallbackRequest cb, HttpRequest httpRequest);

        Task<CustomDesignPaymentResponse?> GetByIdAsync(Guid paymentId);
        Task<IEnumerable<CustomDesignPaymentResponse>> GetByCustomDesignIdAsync(Guid customDesignId);
    }

}
