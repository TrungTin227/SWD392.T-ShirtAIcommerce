using DTOs.UserDTOs.Request;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Commons.Gmail.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Services.Commons.Gmail.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(EmailRequest emailRequest)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var mailMessage = CreateMailMessage(emailRequest);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {string.Join(", ", emailRequest.To)}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {string.Join(", ", emailRequest.To)}");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string firstName, string lastName)
        {
            var template = EmailTemplateService.GetWelcomeTemplate(firstName, lastName);
            var emailRequest = new EmailRequest
            {
                To = new List<string> { email },
                Subject = template.Subject,
                Body = template.Body,
                JobType = "Welcome"
            };

            return await SendEmailAsync(emailRequest);
        }

        public async Task<bool> SendEmailConfirmationAsync(string email, string confirmationLink, string firstName)
        {
            var template = EmailTemplateService.GetEmailConfirmationTemplate(firstName, confirmationLink);
            var emailRequest = new EmailRequest
            {
                To = new List<string> { email },
                Subject = template.Subject,
                Body = template.Body,
                JobType = "EmailConfirmation"
            };

            return await SendEmailAsync(emailRequest);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetLink, string firstName)
        {
            var template = EmailTemplateService.GetPasswordResetTemplate(firstName, resetLink);
            var emailRequest = new EmailRequest
            {
                To = new List<string> { email },
                Subject = template.Subject,
                Body = template.Body,
                JobType = "PasswordReset"
            };

            return await SendEmailAsync(emailRequest);
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(string email, string orderNumber, decimal amount, string firstName)
        {
            var template = EmailTemplateService.GetOrderConfirmationTemplate(firstName, orderNumber, amount);
            var emailRequest = new EmailRequest
            {
                To = new List<string> { email },
                Subject = template.Subject,
                Body = template.Body,
                JobType = "OrderConfirmation"
            };

            return await SendEmailAsync(emailRequest);
        }

        public async Task<bool> SendOrderShippedEmailAsync(string email, string orderNumber, string trackingNumber, string firstName)
        {
            var emailRequest = new EmailRequest
            {
                To = new List<string> { email },
                Subject = $"Đơn hàng #{orderNumber} đã được giao cho đơn vị vận chuyển",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Xin chào {firstName}!</h2>
                        <p>Đơn hàng #{orderNumber} của bạn đã được giao cho đơn vị vận chuyển.</p>
                        <p><strong>Mã vận đơn:</strong> {trackingNumber}</p>
                        <p>Bạn có thể theo dõi đơn hàng bằng mã vận đơn này.</p>
                    </body>
                    </html>",
                JobType = "OrderShipped"
            };

            return await SendEmailAsync(emailRequest);
        }

        public async Task<bool> SendCustomDesignNotificationAsync(string email, string designName, bool isApproved, string firstName)
        {
            var status = isApproved ? "được duyệt" : "bị từ chối";
            var emailRequest = new EmailRequest
            {
                To = new List<string> { email },
                Subject = $"Thiết kế '{designName}' {status}",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Xin chào {firstName}!</h2>
                        <p>Thiết kế '{designName}' của bạn đã {status}.</p>
                        {(isApproved ? "<p>Bạn có thể tiến hành đặt hàng ngay bây giờ!</p>" : "<p>Vui lòng chỉnh sửa và gửi lại thiết kế.</p>")}
                    </body>
                    </html>",
                JobType = "DesignNotification"
            };

            return await SendEmailAsync(emailRequest);
        }

        public async Task<bool> SendPromotionalEmailAsync(string email, string promoCode, decimal discountPercent, string firstName)
        {
            var emailRequest = new EmailRequest
            {
                To = new List<string> { email },
                Subject = $"🎉 Ưu đãi đặc biệt {discountPercent}% cho bạn!",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Xin chào {firstName}!</h2>
                        <p>Chúng tôi có ưu đãi đặc biệt dành cho bạn!</p>
                        <p><strong>Mã giảm giá:</strong> {promoCode}</p>
                        <p><strong>Giảm giá:</strong> {discountPercent}%</p>
                        <p>Sử dụng ngay để được giảm giá!</p>
                    </body>
                    </html>",
                JobType = "Promotional"
            };

            return await SendEmailAsync(emailRequest);
        }

        public async Task<List<EmailRequest>> GetPendingEmailsAsync()
        {
            // TODO: Implement logic to get pending emails from database
            // This is a placeholder implementation
            _logger.LogWarning("GetPendingEmailsAsync not implemented - returning empty list");
            return new List<EmailRequest>();
        }

        public async Task<bool> MarkEmailAsSentAsync(Guid emailId)
        {
            // TODO: Implement logic to mark email as sent in database
            _logger.LogWarning($"MarkEmailAsSentAsync not implemented for email ID: {emailId}");
            return true;
        }

        public async Task<bool> RetryFailedEmailAsync(Guid emailId)
        {
            // TODO: Implement logic to retry failed email
            _logger.LogWarning($"RetryFailedEmailAsync not implemented for email ID: {emailId}");
            return true;
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                EnableSsl = _emailSettings.EnableSsl
            };
        }

        private MailMessage CreateMailMessage(EmailRequest emailRequest)
        {
            var mailMessage = new MailMessage();

            // Validate and set From address - sử dụng đúng property name
            var fromEmail = _emailSettings.FromEmail?.Trim();
            var fromDisplayName = _emailSettings.FromName?.Trim();

            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException("FromEmail is not configured or empty in EmailSettings");

            // Create From address - only use display name if it's not empty
            if (!string.IsNullOrWhiteSpace(fromDisplayName))
                mailMessage.From = new MailAddress(fromEmail, fromDisplayName);
            else
                mailMessage.From = new MailAddress(fromEmail);

            // Validate and add To addresses
            if (emailRequest.To == null || !emailRequest.To.Any())
                throw new ArgumentException("No recipients specified");

            foreach (var toEmail in emailRequest.To)
            {
                var cleanEmail = toEmail?.Trim();
                if (!string.IsNullOrWhiteSpace(cleanEmail) && IsValidEmail(cleanEmail))
                {
                    mailMessage.To.Add(new MailAddress(cleanEmail));
                }
                else
                {
                    _logger.LogWarning("Invalid or empty email address skipped: '{Email}'", toEmail);
                }
            }

            if (!mailMessage.To.Any())
                throw new ArgumentException("No valid recipients found");

            // Set subject and body
            mailMessage.Subject = emailRequest.Subject ?? string.Empty;
            mailMessage.Body = emailRequest.Body ?? string.Empty;
            mailMessage.IsBodyHtml = true;

            // Set Reply-To using SupportEmail
            var replyToEmail = _emailSettings.SupportEmail?.Trim();
            if (!string.IsNullOrWhiteSpace(replyToEmail) && IsValidEmail(replyToEmail))
            {
                mailMessage.ReplyToList.Add(new MailAddress(replyToEmail));
            }

            return mailMessage;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        // Thêm method debug để kiểm tra EmailSettings
        public void LogEmailSettings()
        {
            _logger.LogInformation("EmailSettings Debug - FromEmail: '{FromEmail}', FromName: '{FromName}', SupportEmail: '{SupportEmail}'",
                _emailSettings.FromEmail,
                _emailSettings.FromName,
                _emailSettings.SupportEmail);
        }
    }
}