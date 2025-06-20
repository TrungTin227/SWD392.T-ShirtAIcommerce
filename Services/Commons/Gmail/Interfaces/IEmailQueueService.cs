namespace Services.Commons.Gmail.Interfaces
{
    public interface IEmailQueueService
    {
        Task QueueEmailAsync(List<string> emails, string subject, string message);
        Task QueueEmailAsync(string email, string subject, string message);
    }
}