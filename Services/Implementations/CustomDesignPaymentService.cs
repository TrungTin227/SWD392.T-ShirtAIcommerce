using BusinessObjects.Common;
using BusinessObjects.CustomDesignPayments;
using DTOs.CustomOrder;
using DTOs.Payments.VnPay;
using Repositories;
using Services.Interfaces;

namespace Services.Implementations
{
    public class CustomDesignPaymentService : ICustomDesignPaymentService
    {
        private readonly T_ShirtAIcommerceContext _db;
        private readonly IVnPayService _vnPayService;

        public CustomDesignPaymentService(T_ShirtAIcommerceContext db, IVnPayService vnPayService)
        {
            _db = db;
            _vnPayService = vnPayService;
        }

        public async Task<CustomDesignPaymentResponse> CreateCustomDesignPaymentAsync(
            CustomDesignPaymentCreateRequest req, string ipAddress)
        {
            // 1. Validate
            var cd = await _db.CustomDesigns.FindAsync(req.CustomDesignId)
                     ?? throw new Exception("CustomDesign không tồn tại!");

            // 2. Tạo mã giao dịch
            var txnRef = $"CD{DateTime.Now:yyyyMMddHHmmss}{cd.Id:N}".Substring(0, 22);

            // 3. Lưu record
            var payment = new CustomDesignPayment
            {
                CustomDesignId = cd.Id,
                Amount = cd.TotalPrice,
                PaymentMethod = req.PaymentMethod,
                Status = PaymentStatus.Unpaid,
                CreatedAt = DateTime.UtcNow,
                TransactionId = txnRef,
                Notes = req.Description
            };
            _db.CustomDesignPayments.Add(payment);
            await _db.SaveChangesAsync();

            string? payUrl = null;
            string? respCode = null;

            // 4. Nếu là VNPAY → tạo URL
            if (req.PaymentMethod == PaymentMethod.VNPAY)
            {
                var vnReq = new VnPayCreatePaymentRequest
                {
                    vnp_TxnRef = txnRef,
                    vnp_Amount = (long)cd.TotalPrice,
                    vnp_OrderInfo = $"Thanh toán CustomDesign #{cd.Id}",
                    vnp_OrderType = "other",
                    vnp_Locale = "vn",
                    vnp_IpAddr = ipAddress,
                    vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss")
                };

                var vnResp = await _vnPayService.CreatePaymentUrlAsync(vnReq);
                payUrl = vnResp.PaymentUrl;
                respCode = vnResp.VnPayResponseCode; // nếu bạn có property này
            }

            // 5. Map ra DTO
            return new CustomDesignPaymentResponse
            {
                PaymentId = payment.Id,
                CustomDesignId = payment.CustomDesignId,
                PaymentMethod = payment.PaymentMethod.ToString(),
                Amount = payment.Amount,
                TransactionId = payment.TransactionId,
                Status = payment.Status.ToString(),
                CreatedAt = payment.CreatedAt,
                PaidAt = payment.PaidAt,
                Notes = payment.Notes,
                PaymentUrl = payUrl,
                VnPayResponseCode = respCode
            };
        }
    }
}
