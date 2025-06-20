namespace DTOs.EmailDTOs
{
    public class EmailTemplate
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public EmailTemplateType Type { get; set; }
    }

    public enum EmailTemplateType
    {
        Welcome,
        EmailConfirmation,
        PasswordReset,
        OrderConfirmation,
        OrderShipped,
        OrderDelivered,
        CustomDesignApproved,
        CustomDesignRejected,
        AccountLocked,
        PromotionalOffer
    }
}