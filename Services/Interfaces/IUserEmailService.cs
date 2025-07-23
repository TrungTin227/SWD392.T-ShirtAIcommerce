using BusinessObjects.Common;

namespace Services.Interfaces
{
    namespace Services.Commons.User
    {
        public interface IUserEmailService
        {
            Task SendWelcomeEmailAsync(string email);
            Task SendEmailConfirmationAsync(string email, Guid userId, string token, string confirmEmailUri);
            Task ResendEmailConfirmationAsync(string email, Guid userId, string token, string confirmEmailUri);
            Task SendPasswordResetEmailAsync(string email, string token, string resetPasswordUri);
            Task SendPasswordChangedNotificationAsync(string email);
            Task Send2FACodeAsync(string email, string code);
            //DesignCustoms
            Task SendCustomDesignStatusEmailAsync(
       string email,
       string designName,
       CustomDesignStatus status,
       DateTime? orderCreatedAt = null,
       DateTime? shippingStartAt = null,
       DateTime? deliveredAt = null,
       DateTime? doneAt = null,
       string? customerName = null,
       string? customerPhone = null,
       string? customerAddress = null);
        }
    }
}
