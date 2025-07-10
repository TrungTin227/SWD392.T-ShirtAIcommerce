using System.Net;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Services.Commons.Gmail.Interfaces;
using Services.Interfaces.Services.Commons.User;

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
