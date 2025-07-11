using BusinessObjects.Common;
using BusinessObjects.Payments;

namespace Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(Guid id);
        Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId);
        Task AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task DeleteAsync(Payment payment);
        Task SaveChangesAsync();
        Task<Payment?> GetActiveVnPayPaymentByOrderIdAsync(Guid orderId);
        Task<bool> HasActivePaymentForOrderAsync(Guid orderId, PaymentMethod paymentMethod);
    }
}