using BusinessObjects.Common;
using BusinessObjects.CustomDesignPayments;
using DTOs.CustomOrder;
using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Repositories;
using Services.Configuration;
using Services.Helpers;
using Services.Implementations;
using Services.Interfaces;
using Services.Interfaces.Services.Commons.User;
using System.Net;

public class CustomDesignPaymentService : ICustomDesignPaymentService
{
    private readonly T_ShirtAIcommerceContext _db;
    private readonly IVnPayService _vnPayService;
    private readonly VnPayConfig _cfg;
    private readonly IUserEmailService _userEmailService;

    public CustomDesignPaymentService(T_ShirtAIcommerceContext db, IVnPayService vnPayService, IOptions<VnPayConfig> cfg, IUserEmailService userEmailService)
    {
        _db = db;
        _vnPayService = vnPayService;
        _cfg = cfg.Value;
        _userEmailService = userEmailService;
    }

    public async Task<CustomDesignPaymentResponse> CreateCustomDesignPaymentAsync(
        CustomDesignPaymentCreateRequest req, string ipAddress)
    {
        var cd = await _db.CustomDesigns.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.CustomDesignId)
                 ?? throw new Exception("CustomDesign không tồn tại!");

        var txnRef = $"CD{DateTime.Now:yyyyMMddHHmmss}{cd.Id:N}".Substring(0, 22);

        string? payUrl = null;
        string? respCode = null;

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

            var vnResp = await _vnPayService.CreatePaymentUrlAsync(
    vnReq,
    _cfg.ReturnUrlCustomDesign ?? _cfg.ReturnUrl
);

            if (!vnResp.Success || string.IsNullOrWhiteSpace(vnResp.PaymentUrl))
                throw new InvalidOperationException($"Không tạo được URL VNPAY: {vnResp.Message}");

            payUrl = vnResp.PaymentUrl;
            respCode = vnResp.VnPayResponseCode;
        }

        var payment = new CustomDesignPayment
        {
            CustomDesignId = cd.Id,
            Amount = cd.TotalPrice,
            PaymentMethod = req.PaymentMethod,
            Status = PaymentStatus.Unpaid,
            CreatedAt = DateTime.UtcNow,
            TransactionId = txnRef,
            Notes = req.Description,
            PayerName = req.PayerName,
            PayerPhone = req.PayerPhone,
            PayerAddress = req.PayerAddress
        };

        _db.CustomDesignPayments.Add(payment);
        await _db.SaveChangesAsync();


        return Map(payment, payUrl, respCode);
    }

    public async Task<bool> HandleVnPayCallbackAsync(VnPayCallbackRequest cb, HttpRequest request)
    {
        // 1. Validate chữ ký
        var list = request.Query
            .Where(kv => kv.Key.StartsWith("vnp_"))
            .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value!));

        var rawData = string.Join("&", list);
        var computed = Utils.HmacSHA512(_cfg.HashSecret, rawData);
        var received = request.Query["vnp_SecureHash"].ToString();
        if (!string.Equals(computed, received, StringComparison.OrdinalIgnoreCase)) return false;

        // 2. Lấy payment + CustomDesign + User
        var txnRef = cb.vnp_TxnRef;
        var payment = await _db.CustomDesignPayments
                               .Include(p => p.CustomDesign)
                                   .ThenInclude(cd => cd.User)
                               .FirstOrDefaultAsync(p => p.TransactionId == txnRef);
        if (payment == null) return false;

        // 3. Xác định kết quả
        var ok = cb.vnp_ResponseCode == "00" && cb.vnp_TransactionStatus == "00";

        if (ok)
        {
            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;

            var cd = payment.CustomDesign;
            if (cd.Status != CustomDesignStatus.Order)
            {
                cd.Status = CustomDesignStatus.Order;
                cd.OrderCreatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
        }
        payment.Notes = $"VNPAY RespCode={cb.vnp_ResponseCode}, TransStatus={cb.vnp_TransactionStatus}";
        await _db.SaveChangesAsync();

        // 4. Gửi mail sau khi đã lưu DB
        if (ok && payment.CustomDesign?.User?.Email is string email && !string.IsNullOrWhiteSpace(email))
        {
            try
            {
                await _userEmailService.SendCustomDesignStatusEmailAsync(
                    email: email,
                    designName: payment.CustomDesign.DesignName,
                    status: payment.CustomDesign.Status,                 // Order
                    orderCreatedAt: payment.CustomDesign.OrderCreatedAt,
                    customerName: payment.PayerName,
                    customerPhone: payment.PayerPhone,
                    customerAddress: payment.PayerAddress
                );
            }
            catch (Exception ex)
            {
                // Log để biết nếu email fail nhưng vẫn trả ok cho VNPAY
                // _logger.LogError(ex, "Send mail failed for CustomDesignPayment {Id}", payment.Id);
            }
        }

        return ok;
    }


    public async Task<CustomDesignPaymentResponse?> GetByIdAsync(Guid paymentId)
    {
        var p = await _db.CustomDesignPayments.FindAsync(paymentId);
        return p == null ? null : Map(p, null, null);
    }

    public async Task<IEnumerable<CustomDesignPaymentResponse>> GetByCustomDesignIdAsync(Guid customDesignId)
    {
        var list = await _db.CustomDesignPayments
                            .Where(x => x.CustomDesignId == customDesignId)
                            .OrderByDescending(x => x.CreatedAt)
                            .ToListAsync();
        return list.Select(x => Map(x, null, null));
    }

    private static CustomDesignPaymentResponse Map(
        CustomDesignPayment p, string? url, string? code)
        => new()
        {
            PaymentId = p.Id,
            CustomDesignId = p.CustomDesignId,
            PaymentMethod = p.PaymentMethod.ToString(),
            Amount = p.Amount,
            TransactionId = p.TransactionId,
            Status = p.Status.ToString(),
            CreatedAt = p.CreatedAt,
            PaidAt = p.PaidAt,
            Notes = p.Notes,
            PaymentUrl = url,
            VnPayResponseCode = code
        };
}
