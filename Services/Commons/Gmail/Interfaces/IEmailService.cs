using DTOs.UserDTOs.Request;

namespace Services.Commons.Gmail.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailRequest emailRequest);
        Task<bool> SendWelcomeEmailAsync(string email, string firstName, string lastName);
        Task<bool> SendEmailConfirmationAsync(string email, string confirmationLink, string firstName);
        Task<bool> SendPasswordResetEmailAsync(string email, string resetLink, string firstName);
        Task<bool> SendOrderConfirmationEmailAsync(string email, string orderNumber, decimal amount, string firstName);
        Task<bool> SendOrderShippedEmailAsync(string email, string orderNumber, string trackingNumber, string firstName);
        Task<bool> SendCustomDesignNotificationAsync(string email, string designName, bool isApproved, string firstName);
        Task<bool> SendPromotionalEmailAsync(string email, string promoCode, decimal discountPercent, string firstName);
        Task<List<EmailRequest>> GetPendingEmailsAsync();
        Task<bool> MarkEmailAsSentAsync(Guid emailId);
        Task<bool> RetryFailedEmailAsync(Guid emailId);
    }
}