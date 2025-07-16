using BusinessObjects.Common;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Services.Commons.Gmail.Interfaces;
using Services.Interfaces.Services.Commons.User;
using System.Net;
using System.Text;

namespace Services.Implementations
{
    public class UserEmailService : IUserEmailService
    {
        private readonly IEmailQueueService _emailQueueService;
        private readonly ILogger<UserEmailService> _logger;

        public UserEmailService(IEmailQueueService emailQueueService, ILogger<UserEmailService> logger)
        {
            _emailQueueService = emailQueueService ?? throw new ArgumentNullException(nameof(emailQueueService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendWelcomeEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống hoặc null.", nameof(email));

            var subject = "🎉 Chào mừng bạn đến với TShirtAI!";
            var content = @"
                <h2 style='color:#0d6efd;'>Chào mừng bạn!</h2>
                <p>Cảm ơn bạn đã đăng ký tài khoản tại <b>TShirtAICertify</b>.</p>
                <p>Chúng tôi rất vui được đồng hành cùng bạn trên hành trình phát triển sự nghiệp.</p>
                <p style='margin-top:32px;'>
                    <a href='https://TShirtAI.vn' style='background:#0d6efd;color:#fff;text-decoration:none;padding:12px 28px;border-radius:6px;font-weight:bold;display:inline-block;'>Khám phá ngay</a>
                </p>";
            var message = BuildLayout(subject, content);

            await QueueEmailAsync(email, subject, message);
        }

        public async Task SendEmailConfirmationAsync(string email, Guid userId, string token, string confirmEmailUri)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống hoặc null.", nameof(email));
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token không được để trống hoặc null.", nameof(token));
            if (string.IsNullOrWhiteSpace(confirmEmailUri))
                throw new ArgumentException("ConfirmEmailUri không được để trống hoặc null.", nameof(confirmEmailUri));

            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var encodedUserId = WebUtility.UrlEncode(userId.ToString());
            var confirmLink = $"{confirmEmailUri}?userId={encodedUserId}&token={encodedToken}";

            var subject = "Xác nhận tài khoản của bạn";
            var content = $@"
                <h2 style='color:#0d6efd;'>Xác nhận tài khoản</h2>
                <p>Vui lòng nhấn vào nút bên dưới để xác nhận tài khoản email của bạn.</p>
                <p style='margin:30px 0;'>
                    <a href='{confirmLink}' style='background:#0d6efd;color:#fff;text-decoration:none;padding:12px 28px;border-radius:6px;font-weight:bold;'>Xác nhận Email</a>
                </p>
                <p style='color:#999;font-size:13px;'>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.</p>";
            var message = BuildLayout(subject, content);

            await QueueEmailAsync(email, subject, message);
        }

        public async Task ResendEmailConfirmationAsync(string email, Guid userId, string token, string confirmEmailUri)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống hoặc null.", nameof(email));
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token không được để trống hoặc null.", nameof(token));
            if (string.IsNullOrWhiteSpace(confirmEmailUri))
                throw new ArgumentException("ConfirmEmailUri không được để trống hoặc null.", nameof(confirmEmailUri));

            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var confirmLink = $"{confirmEmailUri}?userId={userId}&token={encodedToken}";
            var subject = "Xác nhận Email - Gửi lại";
            var content = $@"
                <h2 style='color:#0d6efd;'>Xác nhận tài khoản</h2>
                <p>Vui lòng nhấn vào nút bên dưới để xác nhận tài khoản email của bạn.</p>
                <p style='margin:30px 0;'>
                    <a href='{confirmLink}' style='background:#0d6efd;color:#fff;text-decoration:none;padding:12px 28px;border-radius:6px;font-weight:bold;'>Xác nhận Email</a>
                </p>";
            var message = BuildLayout(subject, content);

            await QueueEmailAsync(email, subject, message);
        }

        public async Task SendPasswordResetEmailAsync(string email, string token, string resetPasswordUri)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống hoặc null.", nameof(email));
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token không được để trống hoặc null.", nameof(token));
            if (string.IsNullOrWhiteSpace(resetPasswordUri))
                throw new ArgumentException("ResetPasswordUri không được để trống hoặc null.", nameof(resetPasswordUri));

            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetLink = $"{resetPasswordUri}?email={Uri.EscapeDataString(email)}&token={encodedToken}";

            var subject = "Đặt lại mật khẩu TShirtAICertify";
            var content = $@"
                <h2 style='color:#0d6efd;'>Đặt lại mật khẩu</h2>
                <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản TShirtAI của mình.</p>
                <p style='margin:30px 0;'>
                    <a href='{resetLink}' style='background:#dc3545;color:#fff;text-decoration:none;padding:12px 28px;border-radius:6px;font-weight:bold;'>Đặt lại mật khẩu</a>
                </p>
                <p style='color:#999;font-size:13px;'>Nếu bạn không yêu cầu hành động này, hãy bỏ qua email này.</p>";
            var message = BuildLayout(subject, content);

            await QueueEmailAsync(email, subject, message);
        }

        public async Task SendPasswordChangedNotificationAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống hoặc null.", nameof(email));

            var subject = "Mật khẩu của bạn đã được thay đổi";
            var content = @"
                <h2 style='color:#0d6efd;'>Thông báo thay đổi mật khẩu</h2>
                <p>Mật khẩu của bạn đã được thay đổi thành công.</p>
                <p>Nếu bạn không thực hiện hành động này, vui lòng liên hệ với chúng tôi ngay lập tức.</p>";
            var message = BuildLayout(subject, content);

            await QueueEmailAsync(email, subject, message);
        }

        public async Task Send2FACodeAsync(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống hoặc null.", nameof(email));
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Mã 2FA không được để trống hoặc null.", nameof(code));

            var subject = "Mã xác thực hai yếu tố (2FA) TShirtAICertify";
            var content = $@"
                <h2 style='color:#0d6efd;'>Mã xác thực hai yếu tố</h2>
                <p>Mã xác thực của bạn là: <span style='font-size:24px;font-weight:bold;color:#0d6efd;'>{code}</span></p>
                <p style='color:#999;font-size:13px;'>Mã này có hiệu lực trong 5 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>";
            var message = BuildLayout(subject, content);

            await QueueEmailAsync(email, subject, message);
        }

        //SendCustomDesignStatusEmailAsync
        public async Task SendCustomDesignStatusEmailAsync(
     string email,
     string designName,
     CustomDesignStatus status,
     DateTime? orderCreatedAt = null,
     DateTime? shippingStartAt = null,
     DateTime? deliveredAt = null,
     DateTime? doneAt = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống.", nameof(email));

            string subject;
            string content;

            switch (status)
            {
                case CustomDesignStatus.Accepted:
                    subject = $"Thiết kế của bạn đã được chấp nhận!";
                    content = $@"
                <h2 style='color:#0d6efd;'>Thiết kế được duyệt</h2>
                <p>Chúc mừng! Mẫu thiết kế <b>{designName}</b> của bạn đã được chấp nhận. Chúng tôi sẽ liên hệ để tiếp tục quy trình đặt hàng.</p>
                <p>Cảm ơn bạn đã tin tưởng và sử dụng TShirtAI!</p>";
                    break;
                case CustomDesignStatus.Order:
                    var orderTime = orderCreatedAt?.ToString("HH:mm dd/MM/yyyy") ?? DateTime.Now.ToString("HH:mm dd/MM/yyyy");
                    subject = $"Đơn hàng mới từ thiết kế {designName}";
                    content = $@"
                <h2 style='color:#0d6efd;'>Đơn hàng đã được tạo</h2>
                <p>Mẫu thiết kế <b>{designName}</b> đã chuyển thành đơn hàng lúc <b>{orderTime}</b> và đang được xử lý.</p>
                <p>Chúng tôi cần khoảng <b>5-7 ngày</b> để hoàn thành sản phẩm cho bạn. Vui lòng chờ thông tin tiếp theo qua email.</p>";
                    break;
                case CustomDesignStatus.Shipping:
                    var shippingTime = shippingStartAt?.ToString("HH:mm dd/MM/yyyy") ?? DateTime.Now.ToString("HH:mm dd/MM/yyyy");
                    subject = $"Thiết kế {designName} đang được giao";
                    content = $@"
                <h2 style='color:#0d6efd;'>Sản phẩm đang trên đường đến bạn</h2>
                <p>Đơn hàng <b>{designName}</b> đã được xuất kho lúc <b>{shippingTime}</b> và đang được giao đến bạn.</p>
                <p>Dự kiến sẽ đến nơi trong <b>3-5 ngày</b>. Nếu cần hỗ trợ về đơn hàng, hãy liên hệ ngay với chúng tôi.</p>";
                    break;
                case CustomDesignStatus.Delivered:
                    var deliveredTime = deliveredAt?.ToString("HH:mm dd/MM/yyyy") ?? DateTime.Now.ToString("HH:mm dd/MM/yyyy");
                    subject = $"Thiết kế {designName} đã giao thành công!";
                    content = $@"
                <h2 style='color:#0d6efd;'>Giao hàng thành công</h2>
                <p>Đơn hàng <b>{designName}</b> đã được giao thành công đến bạn lúc <b>{deliveredTime}</b>.</p>
                <p>Cảm ơn bạn đã đồng hành cùng TShirtAI. Đừng quên đánh giá trải nghiệm để chúng tôi phục vụ bạn tốt hơn nhé!</p>";
                    break;
                case CustomDesignStatus.Done:
                    var doneTime = doneAt?.ToString("HH:mm dd/MM/yyyy") ?? DateTime.Now.ToString("HH:mm dd/MM/yyyy");
                    subject = $"Đơn hàng {designName} đã hoàn thành!";
                    content = $@"
                <h2 style='color:#0d6efd;'>Hoàn thành đơn hàng</h2>
                <p>Chúng tôi đã hoàn thành đơn hàng <b>{designName}</b> vào lúc <b>{doneTime}</b>.</p>
                <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>";
                    break;
                case CustomDesignStatus.Rejected:
                    subject = $"Tiếc quá! Thiết kế {designName} chưa được duyệt";
                    content = $@"
                <h2 style='color:#dc3545;'>Thiết kế chưa được chấp nhận</h2>
                <p>Chúng tôi rất tiếc phải thông báo rằng mẫu thiết kế <b>{designName}</b> của bạn chưa được duyệt. Vui lòng kiểm tra lại các yêu cầu và thử lại nhé.</p>";
                    break;
                default:
                    return;
            }

            var message = BuildLayout(subject, content);
            await QueueEmailAsync(email, subject, message);
        }

        private async Task QueueEmailAsync(string email, string subject, string message)
        {
            try
            {
                await _emailQueueService.QueueEmailAsync(email, subject, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xếp hàng email cho {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Template chuẩn hóa cho toàn bộ email (header, body, footer, màu sắc, font, padding…)
        /// </summary>
        private string BuildLayout(string title, string contentHtml)
        {
            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
    <title>{title}</title>
</head>
<body style='margin:0;padding:0;background:#f6f6f6;font-family:Arial,Helvetica,sans-serif;color:#333;'>
    <table width='100%' cellpadding='0' cellspacing='0'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background:white;border-radius:10px;margin-top:32px;box-shadow:0 2px 12px rgba(0,0,0,0.07);overflow:hidden;'>
                    <tr style='background:#0d6efd;'>
                        <td style='text-align:center;padding:28px 0 20px 0;'>
                            <span style='display:inline-block;font-size:26px;font-weight:bold;color:#fff;letter-spacing:1px;'>TShirtAICertify</span>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:32px 32px 28px 32px;font-size:16px;line-height:1.75;'>
                            {contentHtml}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:18px 0;text-align:center;font-size:13px;color:#8a8a8a;background:#f6f6f6;'>
                            © {DateTime.Now.Year} TShirtAI. All rights reserved.
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
