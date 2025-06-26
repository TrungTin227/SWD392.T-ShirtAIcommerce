using BusinessObjects.Orders;
using DTOs.Common;
using DTOs.Orders;
using Microsoft.Extensions.Logging;
using Repositories.WorkSeeds.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IUnitOfWork unitOfWork, ILogger<OrderService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PagedResponse<OrderDTO>> GetOrdersAsync(OrderFilterRequest filter)
        {
            try
            {
                var orderRepo = _unitOfWork.GetRepository<Order, Guid>() as Repositories.Interfaces.IOrderRepository;
                if (orderRepo == null)
                    throw new InvalidOperationException("Order repository not found");

                var pagedOrders = await orderRepo.GetOrdersAsync(filter);
                var orderDTOs = _mapper.Map<List<OrderDTO>>(pagedOrders.Items);

                return new PagedResponse<OrderDTO>
                {
                    Data = orderDTOs,
                    PageNumber = pagedOrders.PageNumber,
                    PageSize = pagedOrders.PageSize,
                    TotalPages = pagedOrders.TotalPages,
                    TotalRecords = pagedOrders.TotalRecords,
                    HasNextPage = pagedOrders.HasNextPage,
                    HasPreviousPage = pagedOrders.HasPreviousPage,
                    Message = "Lấy danh sách đơn hàng thành công",
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders with filter {@Filter}", filter);
                return new PagedResponse<OrderDTO>
                {
                    Message = "Có lỗi xảy ra khi lấy danh sách đơn hàng",
                    IsSuccess = false
                };
            }
        }

        public async Task<OrderDTO?> GetOrderByIdAsync(Guid orderId)
        {
            try
            {
                var orderRepo = _unitOfWork.GetRepository<Order, Guid>() as Repositories.Interfaces.IOrderRepository;
                if (orderRepo == null) return null;

                var order = await orderRepo.GetOrderWithDetailsAsync(orderId);
                return order != null ? _mapper.Map<OrderDTO>(order) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order by id {OrderId}", orderId);
                return null;
            }
        }

        public async Task<OrderDTO?> CreateOrderAsync(CreateOrderRequest request, Guid? createdBy = null)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var orderRepo = _unitOfWork.GetRepository<Order, Guid>() as Repositories.Interfaces.IOrderRepository;
                if (orderRepo == null)
                    throw new InvalidOperationException("Order repository not found");

                // Generate order number
                var orderNumber = await orderRepo.GenerateOrderNumberAsync();

                // Calculate totals
                var subtotal = request.OrderItems.Sum(item => item.UnitPrice * item.Quantity);
                var shippingFee = await CalculateShippingFeeAsync(request.ShippingMethodId);
                var (discountAmount, taxAmount) = await CalculateDiscountAndTaxAsync(request.CouponId, subtotal);
                var totalAmount = subtotal + shippingFee + taxAmount - discountAmount;

                // Create order
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    UserId = request.UserId,
                    TotalAmount = totalAmount,
                    ShippingFee = shippingFee,
                    DiscountAmount = discountAmount,
                    TaxAmount = taxAmount,
                    Status = OrderStatus.Pending,
                    PaymentStatus = PaymentStatus.Unpaid,
                    ShippingAddress = request.ShippingAddress,
                    ReceiverName = request.ReceiverName,
                    ReceiverPhone = request.ReceiverPhone,
                    CustomerNotes = request.CustomerNotes,
                    CouponId = request.CouponId,
                    ShippingMethodId = request.ShippingMethodId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                await orderRepo.AddAsync(order);

                // Create order items
                var orderItemRepo = _unitOfWork.GetRepository<OrderItem, Guid>();
                foreach (var itemRequest in request.OrderItems)
                {
                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = itemRequest.ProductId,
                        ProductVariantId = itemRequest.ProductVariantId,
                        Quantity = itemRequest.Quantity,
                        UnitPrice = itemRequest.UnitPrice,
                        TotalPrice = itemRequest.UnitPrice * itemRequest.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = createdBy
                    };
                    await orderItemRepo.AddAsync(orderItem);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var createdOrder = await orderRepo.GetOrderWithDetailsAsync(order.Id);
                return _mapper.Map<OrderDTO>(createdOrder);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating order {@Request}", request);
                return null;
            }
        }

        public async Task<OrderDTO?> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, Guid? updatedBy = null)
        {
            try
            {
                var orderRepo = _unitOfWork.GetRepository<Order, Guid>();
                var order = await orderRepo.GetByIdAsync(orderId);

                if (order == null) return null;

                // Update fields if provided
                if (request.Status.HasValue)
                    order.Status = request.Status.Value;

                if (request.PaymentStatus.HasValue)
                    order.PaymentStatus = request.PaymentStatus.Value;

                if (!string.IsNullOrEmpty(request.ShippingAddress))
                    order.ShippingAddress = request.ShippingAddress;

                if (!string.IsNullOrEmpty(request.ReceiverName))
                    order.ReceiverName = request.ReceiverName;

                if (!string.IsNullOrEmpty(request.ReceiverPhone))
                    order.ReceiverPhone = request.ReceiverPhone;

                if (request.CustomerNotes != null)
                    order.CustomerNotes = request.CustomerNotes;

                if (request.EstimatedDeliveryDate.HasValue)
                    order.EstimatedDeliveryDate = request.EstimatedDeliveryDate;

                if (!string.IsNullOrEmpty(request.TrackingNumber))
                    order.TrackingNumber = request.TrackingNumber;

                if (!string.IsNullOrEmpty(request.CancellationReason))
                    order.CancellationReason = request.CancellationReason;

                if (request.AssignedStaffId.HasValue)
                    order.AssignedStaffId = request.AssignedStaffId;

                if (request.ShippingMethodId.HasValue)
                    order.ShippingMethodId = request.ShippingMethodId;

                order.UpdatedAt = DateTime.UtcNow;
                if (updatedBy.HasValue)
                    order.UpdatedBy = updatedBy.Value;

                await orderRepo.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<OrderDTO>(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} with {@Request}", orderId, request);
                return null;
            }
        }

        public async Task<bool> DeleteOrderAsync(Guid orderId, Guid? deletedBy = null)
        {
            try
            {
                var orderRepo = _unitOfWork.GetRepository<Order, Guid>();
                var result = await orderRepo.SoftDeleteAsync(orderId, deletedBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<IEnumerable<OrderDTO>> GetUserOrdersAsync(Guid userId)
        {
            try
            {
                var orderRepo = _unitOfWork.GetRepository<Order, Guid>() as Repositories.Interfaces.IOrderRepository;
                if (orderRepo == null) return new List<OrderDTO>();

                var orders = await orderRepo.GetUserOrdersAsync(userId);
                return _mapper.Map<IEnumerable<OrderDTO>>(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders for {UserId}", userId);
                return new List<OrderDTO>();
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, string status, Guid? updatedBy = null)
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(status, out var orderStatus))
                    return false;

                var orderRepo = _unitOfWork.GetRepository<Order, Guid>() as Repositories.Interfaces.IOrderRepository;
                if (orderRepo == null) return false;

                var result = await orderRepo.UpdateOrderStatusAsync(orderId, orderStatus, updatedBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status {OrderId} to {Status}", orderId, status);
                return false;
            }
        }

        public async Task<bool> UpdatePaymentStatusAsync(Guid orderId, string paymentStatus, Guid? updatedBy = null)
        {
            try
            {
                if (!Enum.TryParse<PaymentStatus>(paymentStatus, out var payStatus))
                    return false;

                var orderRepo = _unitOfWork.GetRepository<Order, Guid>() as Repositories.Interfaces.IOrderRepository;
                if (orderRepo == null) return false;

                var result = await orderRepo.UpdatePaymentStatusAsync(orderId, payStatus, updatedBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status {OrderId} to {Status}", orderId, paymentStatus);
                return false;
            }
        }

        public async Task<bool> AssignOrderToStaffAsync(Guid orderId, Guid staffId, Guid? updatedBy = null)
        {
            try
            {
                var orderRepo = _unitOfWork.GetRepository<Order, Guid>() as Repositories.Interfaces.IOrderRepository;
                if (orderRepo == null) return false;

                var result = await orderRepo.AssignOrderToStaffAsync(orderId, staffId, updatedBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning order {OrderId} to staff {StaffId}", orderId, staffId);
                return false;
            }
        }

        public async Task<bool> CancelOrderAsync(Guid orderId, string reason, Guid? cancelledBy = null)
        {
            try
            {
                var orderRepo = _unitOfWork.GetRepository<Order, Guid>() as Repositories.Interfaces.IOrderRepository;
                if (orderRepo == null) return false;

                var result = await orderRepo.CancelOrderAsync(orderId, reason, cancelledBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<IEnumerable<OrderDTO>> GetStaffOrdersAsync(Guid staffId)
        {
            try
            {
                var orderRepo = _unitOfWork.GetRepository<Order, Guid>() as Repositories.Interfaces.IOrderRepository;
                if (orderRepo == null) return new List<OrderDTO>();

                var orders = await orderRepo.GetStaffOrdersAsync(staffId);
                return _mapper.Map<IEnumerable<OrderDTO>>(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff orders for {StaffId}", staffId);
                return new List<OrderDTO>();
            }
        }

        public async Task<BatchOperationResultDTO> BulkUpdateStatusAsync(List<Guid> orderIds, string status, Guid? updatedBy = null)
        {
            var result = new BatchOperationResultDTO
            {
                TotalRequested = orderIds.Count
            };

            if (!Enum.TryParse<OrderStatus>(status, out var orderStatus))
            {
                result.Message = "Trạng thái không hợp lệ";
                result.FailureCount = orderIds.Count;
                result.Errors = orderIds.Select(id => new BatchOperationErrorDTO
                {
                    Id = id.ToString(),
                    Error = "Invalid status",
                    Details = $"Status '{status}' is not valid"
                }).ToList();
                return result;
            }

            foreach (var orderId in orderIds)
            {
                try
                {
                    var success = await UpdateOrderStatusAsync(orderId, status, updatedBy);
                    if (success)
                    {
                        result.SuccessCount++;
                        result.SuccessIds.Add(orderId.ToString());
                    }
                    else
                    {
                        result.FailureCount++;
                        result.Errors.Add(new BatchOperationErrorDTO
                        {
                            Id = orderId.ToString(),
                            Error = "Update failed",
                            Details = "Could not update order status"
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BatchOperationErrorDTO
                    {
                        Id = orderId.ToString(),
                        Error = "Exception occurred",
                        Details = ex.Message
                    });
                }
            }

            result.Message = $"Cập nhật thành công {result.SuccessCount}/{result.TotalRequested} đơn hàng";
            return result;
        }

        // Helper methods
        private async Task<decimal> CalculateShippingFeeAsync(Guid? shippingMethodId)
        {
            if (!shippingMethodId.HasValue) return 0;

            // Implementation would get shipping method and calculate fee
            // For now, return a default value
            return 30000; // 30k VND default shipping fee
        }

        private async Task<(decimal discountAmount, decimal taxAmount)> CalculateDiscountAndTaxAsync(Guid? couponId, decimal subtotal)
        {
            decimal discountAmount = 0;
            decimal taxAmount = subtotal * 0.1m; // 10% VAT

            if (couponId.HasValue)
            {
                // Implementation would get coupon and calculate discount
                // For now, return a default discount
                discountAmount = subtotal * 0.05m; // 5% discount
            }

            return (discountAmount, taxAmount);
        }
    }
}