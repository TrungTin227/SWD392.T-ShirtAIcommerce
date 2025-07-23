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

            // 2. Tạo mã giao dịch (TxnRef)
            var txnRef = $"CD{DateTime.Now:yyyyMMddHHmmss}{cd.Id:N}".Substring(0, 22);

            string? payUrl = null;
            string? respCode = null;

            // 3. Xử lý VNPAY: Gọi VNPAY để lấy URL TRƯỚC KHI lưu vào DB
            if (req.PaymentMethod == PaymentMethod.VNPAY)
            {
                var vnReq = new VnPayCreatePaymentRequest
                {
                    vnp_TxnRef = txnRef,
                    vnp_Amount = (long)cd.TotalPrice, // Đảm bảo số tiền đúng format của VNPAY (vd: VNĐ không dấu phẩy)
                    vnp_OrderInfo = $"Thanh toán CustomDesign #{cd.Id}",
                    vnp_Locale = "vn",
                    vnp_IpAddr = ipAddress,
                    vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss")
                };

                var vnResp = await _vnPayService.CreatePaymentUrlAsync(vnReq);

                // KIỂM TRA KẾT QUẢ TỪ VNPAY
                if (!vnResp.Success || string.IsNullOrEmpty(vnResp.PaymentUrl))
                {
                    // Nếu VNPAY không trả về URL hoặc có lỗi, ném ngoại lệ
                    // Không lưu bản ghi payment vào DB trong trường hợp này
                    throw new InvalidOperationException($"Không thể tạo URL thanh toán VNPAY. Lỗi: {vnResp.Message ?? "Lỗi không xác định từ VNPAY."}");
                }

                payUrl = vnResp.PaymentUrl;
                respCode = vnResp.VnPayResponseCode;
            }
            // ELSE: Nếu không phải VNPAY, payUrl và respCode sẽ vẫn là null, tiếp tục tạo payment như bình thường

            // 4. Lưu record CustomDesignPayment vào DB CHỈ KHI URL VNPAY đã được tạo thành công
            // (hoặc nếu phương thức thanh toán không phải VNPAY)
            var payment = new CustomDesignPayment
            {
                CustomDesignId = cd.Id,
                Amount = cd.TotalPrice,
                PaymentMethod = req.PaymentMethod,
                Status = PaymentStatus.Unpaid, // Ban đầu luôn là Unpaid
                CreatedAt = DateTime.UtcNow,
                TransactionId = txnRef,
                Notes = req.Description,
                PaidAt = null // Đảm bảo PaidAt là null khi khởi tạo payment
            };
            _db.CustomDesignPayments.Add(payment);
            await _db.SaveChangesAsync(); // <-- SaveChangesAsync được gọi SAU KHI lấy được URL VNPAY

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