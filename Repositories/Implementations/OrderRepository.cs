﻿using BusinessObjects.Common;
using BusinessObjects.Orders;
using DTOs.Orders;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class OrderRepository : GenericRepository<Order, Guid>, IOrderRepository
    {
        public OrderRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<PagedList<Order>> GetOrdersAsync(OrderFilterRequest filter)
        {
            var query = _dbSet.AsQueryable();

            // Apply filters
            if (filter.UserId.HasValue)
                query = query.Where(o => o.UserId == filter.UserId.Value);

            if (filter.Status.HasValue)
                query = query.Where(o => o.Status == filter.Status.Value);

            if (filter.PaymentStatus.HasValue)
                query = query.Where(o => o.PaymentStatus == filter.PaymentStatus.Value);

            if (filter.AssignedStaffId.HasValue)
                query = query.Where(o => o.AssignedStaffId == filter.AssignedStaffId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);

            if (!string.IsNullOrEmpty(filter.OrderNumber))
                query = query.Where(o => o.OrderNumber.Contains(filter.OrderNumber));

            if (!string.IsNullOrEmpty(filter.ReceiverName))
                query = query.Where(o => o.ReceiverName.Contains(filter.ReceiverName));

            if (!string.IsNullOrEmpty(filter.ReceiverPhone))
                query = query.Where(o => o.ReceiverPhone.Contains(filter.ReceiverPhone));

            if (filter.MinAmount.HasValue)
                query = query.Where(o => o.TotalAmount >= filter.MinAmount.Value);

            if (filter.MaxAmount.HasValue)
                query = query.Where(o => o.TotalAmount <= filter.MaxAmount.Value);

            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(o =>
                    o.OrderNumber.Contains(filter.Search) ||
                    o.ReceiverName.Contains(filter.Search) ||
                    o.ReceiverPhone.Contains(filter.Search) ||
                    o.ShippingAddress.Contains(filter.Search));
            }

            if (filter.HasTracking.HasValue)
            {
                if (filter.HasTracking.Value)
                    query = query.Where(o => !string.IsNullOrEmpty(o.TrackingNumber));
                else
                    query = query.Where(o => string.IsNullOrEmpty(o.TrackingNumber));
            }

            if (filter.CouponId.HasValue)
                query = query.Where(o => o.CouponId == filter.CouponId.Value);

            if (filter.ShippingMethodId.HasValue)
                query = query.Where(o => o.ShippingMethodId == filter.ShippingMethodId.Value);

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "ordernumber" => filter.SortDescending
                    ? query.OrderByDescending(o => o.OrderNumber)
                    : query.OrderBy(o => o.OrderNumber),
                "totalamount" => filter.SortDescending
                    ? query.OrderByDescending(o => o.TotalAmount)
                    : query.OrderBy(o => o.TotalAmount),
                "status" => filter.SortDescending
                    ? query.OrderByDescending(o => o.Status)
                    : query.OrderBy(o => o.Status),
                "paymentstatus" => filter.SortDescending
                    ? query.OrderByDescending(o => o.PaymentStatus)
                    : query.OrderBy(o => o.PaymentStatus),
                "receivername" => filter.SortDescending
                    ? query.OrderByDescending(o => o.ReceiverName)
                    : query.OrderBy(o => o.ReceiverName),
                "estimateddeliverydate" => filter.SortDescending
                    ? query.OrderByDescending(o => o.EstimatedDeliveryDate)
                    : query.OrderBy(o => o.EstimatedDeliveryDate),
                _ => filter.SortDescending
                    ? query.OrderByDescending(o => o.CreatedAt)
                    : query.OrderBy(o => o.CreatedAt)
            };

            // Include related data
            query = query.Include(o => o.User)
                         .Include(o => o.AssignedStaff)
                         .Include(o => o.Coupon)
                         .Include(o => o.ShippingMethod)
                         .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.ProductVariant)
                                .ThenInclude(pv => pv.Product) 

                         .Include(o => o.Payments);

            return await PagedList<Order>.ToPagedListAsync(query, filter.PageNumber, filter.PageSize);
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId)
        {
            return await _dbSet
                    .Where(o => o.UserId == userId && !o.IsDeleted)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.ProductVariant)
                            .ThenInclude(pv => pv.Product) 
                    .Include(o => o.Payments)
                    .Include(o => o.ShippingMethod)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
        }

        public async Task<Order?> GetOrderWithDetailsAsync(Guid orderId)
        {
            return await _dbSet
                .Include(o => o.User)
                .Include(o => o.AssignedStaff)
                .Include(o => o.Coupon)
                .Include(o => o.ShippingMethod)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                .Include(o => o.Payments)
                .Include(o => o.Reviews)
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
        }

        public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status)
        {
            return await _dbSet
                .Where(o => o.Status == status && !o.IsDeleted)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _dbSet
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && !o.IsDeleted)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, Guid? updatedBy = null)
        {
            var order = await _dbSet.FindAsync(orderId);
            if (order == null || order.IsDeleted) return false;

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            if (updatedBy.HasValue)
                order.UpdatedBy = updatedBy.Value;

            switch (status)
            {
                case OrderStatus.Delivered:
                    if (!order.EstimatedDeliveryDate.HasValue)
                        order.EstimatedDeliveryDate = DateTime.UtcNow;
                    break;
                case OrderStatus.Cancelled:
                    order.PaymentStatus = PaymentStatus.Refunded;
                    break;
                case OrderStatus.Completed:
                    if (!order.EstimatedDeliveryDate.HasValue)
                        order.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(7); // Default 7 days
                    break;
            }

            return true;
        }

        public async Task<bool> UpdatePaymentStatusAsync(Guid orderId, PaymentStatus paymentStatus, Guid? updatedBy = null)
        {
            var order = await _dbSet.FindAsync(orderId);
            if (order == null || order.IsDeleted) return false;

            order.PaymentStatus = paymentStatus;
            order.UpdatedAt = DateTime.UtcNow;
            if (updatedBy.HasValue)
                order.UpdatedBy = updatedBy.Value;

            return true;
        }
        
        public async Task<bool> AssignOrderToStaffAsync(Guid orderId, Guid staffId, Guid? updatedBy = null)
        {
            var order = await _dbSet.FindAsync(orderId);
            if (order == null || order.IsDeleted) return false;

            order.AssignedStaffId = staffId;
            order.UpdatedAt = DateTime.UtcNow;
            if (updatedBy.HasValue)
                order.UpdatedBy = updatedBy.Value;

            return true;
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"ORD{today}";

            var lastOrder = await _dbSet
                .Where(o => o.OrderNumber.StartsWith(prefix))
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();

            var sequence = 1;
            if (lastOrder != null)
            {
                var lastSequence = lastOrder.OrderNumber.Substring(prefix.Length);
                if (int.TryParse(lastSequence, out var parsed))
                    sequence = parsed + 1;
            }

            return $"{prefix}{sequence:D4}";
        }

        public async Task<decimal> CalculateOrderTotalAsync(Guid orderId)
        {
            return await _dbSet
                .Where(o => o.Id == orderId && !o.IsDeleted)
                .Select(o => o.TotalAmount + o.ShippingFee  - o.DiscountAmount)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsOrderOwnedByUserAsync(Guid orderId, Guid userId)
        {
            return await _dbSet.AnyAsync(o => o.Id == orderId && o.UserId == userId && !o.IsDeleted);
        }

        public async Task<IEnumerable<Order>> GetStaffOrdersAsync(Guid staffId)
        {
            return await _dbSet
                .Where(o => o.AssignedStaffId == staffId && !o.IsDeleted)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CancelOrderAsync(Guid orderId, string reason, Guid? cancelledBy = null)
        {
            var order = await _dbSet.FindAsync(orderId);
            if (order == null || order.IsDeleted) return false;

            // Only allow cancellation for certain statuses
            if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed)
                return false;

            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = reason;
            order.UpdatedAt = DateTime.UtcNow;

            if (cancelledBy.HasValue)
                order.UpdatedBy = cancelledBy.Value;

            if (order.PaymentStatus == PaymentStatus.Paid || order.PaymentStatus == PaymentStatus.Completed)
                order.PaymentStatus = PaymentStatus.Refunded;

            return true;
        }

        public async Task<IEnumerable<Order>> GetOrdersForAnalyticsAsync(DateTime fromDate, DateTime toDate)
        {
            return await _dbSet
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && !o.IsDeleted)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                .ToListAsync();
        }

        public async Task<Dictionary<OrderStatus, int>> GetOrderStatusCountsAsync()
        {
            return await _dbSet
                .Where(o => !o.IsDeleted)
                .GroupBy(o => o.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<IEnumerable<Order>> GetRecentOrdersAsync(int limit = 10)
        {
            return await _dbSet
                .Where(o => !o.IsDeleted)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> UpdateTrackingNumberAsync(Guid orderId, string trackingNumber, Guid? updatedBy = null)
        {
            var order = await _dbSet.FindAsync(orderId);
            if (order == null || order.IsDeleted) return false;

            order.TrackingNumber = trackingNumber;
            order.UpdatedAt = DateTime.UtcNow;
            if (updatedBy.HasValue)
                order.UpdatedBy = updatedBy.Value;

            return true;
        }
        public async Task<decimal> GetTotalRevenueFromCompletedOrdersAsync()
        {
            // QUAN TRỌNG: Chỉ tính doanh thu từ các đơn hàng có Status = Completed
            return await _dbSet
                .Where(o => o.Status == OrderStatus.Completed && !o.IsDeleted)
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<int> GetOrderCountAsync(DateTime fromDate, DateTime toDate)
        {
            return await _dbSet
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && !o.IsDeleted)
                .CountAsync();
        }

        // Bạn đã có phương thức GetOrderStatusCountsAsync, chúng ta sẽ tạo một phương thức tương tự cho PaymentStatus
        public async Task<Dictionary<PaymentStatus, int>> GetPaymentStatusCountsAsync()
        {
            return await _dbSet
                .Where(o => !o.IsDeleted)
                .GroupBy(o => o.PaymentStatus)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }
        public async Task<PagedList<Order>> GetCancelledOrdersAsync(PaginationParams paginationParams, Guid? userId = null)
        {
            // Bắt đầu với một IQueryable cơ bản từ DbSet Orders
            var query = _dbSet.AsQueryable();

            // 1. Lọc các đơn hàng có trạng thái là "Cancelled" và chưa bị xóa mềm
            query = query.Where(o => o.Status == OrderStatus.Cancelled && !o.IsDeleted);

            // 2. Nếu có userId, lọc các đơn hàng của người dùng đó
            if (userId.HasValue)
            {
                query = query.Where(o => o.UserId == userId.Value);
            }

            // 3. Sắp xếp kết quả
            query = query.OrderByDescending(o => o.UpdatedAt);

            // 4. Bao gồm các thông tin liên quan
            // Phương thức này của bạn cần ThenInclude, điều này quan trọng cho lựa chọn giải pháp
            query = query.Include(o => o.OrderItems)
                         .ThenInclude(oi => oi.ProductVariant)
                             .ThenInclude(pv => pv.Product)
                         .Include(o => o.User);

            // 5. SỬA LỖI TẠI ĐÂY: Đổi "CreateAsync" thành "ToPagedListAsync"
            return await PagedList<Order>.ToPagedListAsync(query, paginationParams.PageNumber, paginationParams.PageSize);
        }
    }
}