using BusinessObjects.Common;
using BusinessObjects.Payments;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly T_ShirtAIcommerceContext _context;
        private readonly DbSet<Payment> _payments;

        public PaymentRepository(T_ShirtAIcommerceContext context)
        {
            _context = context;
            _payments = context.Set<Payment>();
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await _payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId)
        {
            return await _payments
                .Where(p => p.OrderId == orderId)
                .Include(p => p.Order)
                .ToListAsync();
        }

        public async Task AddAsync(Payment payment)
        {
            await _payments.AddAsync(payment);
        }

        public async Task UpdateAsync(Payment payment)
        {
            _payments.Update(payment);
        }

        public async Task DeleteAsync(Payment payment)
        {
            _payments.Remove(payment);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<Payment?> GetActiveVnPayPaymentByOrderIdAsync(Guid orderId)
        {
            return await _payments
                .Where(p => p.OrderId == orderId
                         && p.PaymentMethod == PaymentMethod.VNPAY
                         && p.Status == PaymentStatus.Processing)
                .Include(p => p.Order)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasActivePaymentForOrderAsync(Guid orderId, PaymentMethod paymentMethod)
        {
            return await _payments
                .AnyAsync(p => p.OrderId == orderId
                            && p.PaymentMethod == paymentMethod
                            && (p.Status == PaymentStatus.Processing || p.Status == PaymentStatus.Completed));
        }
    }
}