using AutoMapper;
using BusinessObjects.Orders;
using DTOs.Common;
using DTOs.Orders;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Interfaces;
using Services.Helpers;

namespace Services.Implementations
{
    public class OrderService : BaseService<Order, Guid>, IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository repository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            IMapper mapper,
            ILogger<OrderService> logger)
            : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _orderRepository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResponse<OrderDTO>> GetOrdersAsync(OrderFilterRequest filter)
        {
            try
            {
                var pagedOrders = await _orderRepository.GetOrdersAsync(filter);
                var orderDTOs = _mapper.Map<List<OrderDTO>>(pagedOrders);

                return new PagedResponse<OrderDTO>
                {
                    Data = orderDTOs,
                    CurrentPage = pagedOrders.MetaData.CurrentPage,
                    TotalPages = pagedOrders.MetaData.TotalPages,
                    PageSize = pagedOrders.MetaData.PageSize,
                    TotalCount = pagedOrders.MetaData.TotalCount,
                    HasNextPage = pagedOrders.MetaData.CurrentPage < pagedOrders.MetaData.TotalPages,
                    HasPreviousPage = pagedOrders.MetaData.CurrentPage > 1,
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
                    IsSuccess = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<OrderDTO?> GetOrderByIdAsync(Guid orderId)
        {
            try
            {
                var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
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
                // Generate order number
                var orderNumber = await _orderRepository.GenerateOrderNumberAsync();

                // Calculate totals
                var subtotal = request.OrderItems.Sum(item => item.UnitPrice * item.Quantity);
                var shippingFee = await CalculateShippingFeeAsync(request.ShippingMethodId, subtotal);
                var (discountAmount, taxAmount) = await CalculateDiscountAndTaxAsync(request.CouponId, subtotal);
                var totalAmount = subtotal + shippingFee + taxAmount - discountAmount;

                // Create order
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    UserId = createdBy ?? _currentUserService.GetUserId() ?? Guid.Empty,
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
                    ShippingMethodId = request.ShippingMethodId
                };

                // Create order items
                foreach (var itemRequest in request.OrderItems)
                {
                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = itemRequest.ProductId,
                        CustomDesignId = itemRequest.CustomDesignId,
                        ProductVariantId = itemRequest.ProductVariantId,
                        ItemName = itemRequest.ItemName,
                        SelectedColor = itemRequest.SelectedColor,
                        SelectedSize = itemRequest.SelectedSize,
                        Quantity = itemRequest.Quantity,
                        UnitPrice = itemRequest.UnitPrice,
                        TotalPrice = OrderItemBusinessLogic.CalculateTotalPrice(itemRequest.UnitPrice, itemRequest.Quantity)
                    };

                    order.OrderItems.Add(orderItem);
                }

                var createdOrder = await CreateAsync(order);
                await transaction.CommitAsync();

                return _mapper.Map<OrderDTO>(createdOrder);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order {@Request}", request);
                return null;
            }
        }

        public async Task<OrderDTO?> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, Guid? updatedBy = null)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null) return null;

                // Only allow updates for certain statuses
                if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                    return null;

                // Update fields
                if (!string.IsNullOrEmpty(request.ShippingAddress))
                    order.ShippingAddress = request.ShippingAddress;

                if (!string.IsNullOrEmpty(request.ReceiverName))
                    order.ReceiverName = request.ReceiverName;

                if (!string.IsNullOrEmpty(request.ReceiverPhone))
                    order.ReceiverPhone = request.ReceiverPhone;

                if (request.CustomerNotes != null)
                    order.CustomerNotes = request.CustomerNotes;

                if (request.CouponId.HasValue)
                    order.CouponId = request.CouponId;

                if (request.ShippingMethodId.HasValue)
                    order.ShippingMethodId = request.ShippingMethodId;

                if (request.EstimatedDeliveryDate.HasValue)
                    order.EstimatedDeliveryDate = request.EstimatedDeliveryDate;

                if (!string.IsNullOrEmpty(request.TrackingNumber))
                    order.TrackingNumber = request.TrackingNumber;

                if (request.AssignedStaffId.HasValue)
                    order.AssignedStaffId = request.AssignedStaffId;

                var updatedOrder = await UpdateAsync(order);
                return _mapper.Map<OrderDTO>(updatedOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", orderId);
                return null;
            }
        }

        public async Task<bool> DeleteOrderAsync(Guid orderId, Guid? deletedBy = null)
        {
            try
            {
                return await DeleteAsync(orderId);
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
                var orders = await _orderRepository.GetUserOrdersAsync(userId);
                return _mapper.Map<IEnumerable<OrderDTO>>(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders for user {UserId}", userId);
                return Enumerable.Empty<OrderDTO>();
            }
        }

        public async Task<bool> IsOrderOwnedByUserAsync(Guid orderId, Guid userId)
        {
            try
            {
                return await _orderRepository.IsOrderOwnedByUserAsync(orderId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking order ownership {OrderId} for user {UserId}", orderId, userId);
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, string status, Guid? updatedBy = null)
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(status, out var orderStatus))
                    return false;

                var result = await _orderRepository.UpdateOrderStatusAsync(orderId, orderStatus, updatedBy);
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
                if (!Enum.TryParse<PaymentStatus>(paymentStatus, out var paymentStatusEnum))
                    return false;

                var result = await _orderRepository.UpdatePaymentStatusAsync(orderId, paymentStatusEnum, updatedBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status {OrderId} to {PaymentStatus}", orderId, paymentStatus);
                return false;
            }
        }

        public async Task<bool> CancelOrderAsync(Guid orderId, string reason, Guid? cancelledBy = null)
        {
            try
            {
                var result = await _orderRepository.CancelOrderAsync(orderId, reason, cancelledBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> AssignOrderToStaffAsync(Guid orderId, Guid staffId, Guid? updatedBy = null)
        {
            try
            {
                var result = await _orderRepository.AssignOrderToStaffAsync(orderId, staffId, updatedBy);
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

        public async Task<IEnumerable<OrderDTO>> GetStaffOrdersAsync(Guid staffId)
        {
            try
            {
                var orders = await _orderRepository.GetStaffOrdersAsync(staffId);
                return _mapper.Map<IEnumerable<OrderDTO>>(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff orders for staff {StaffId}", staffId);
                return Enumerable.Empty<OrderDTO>();
            }
        }

        public async Task<BatchOperationResultDTO> BulkUpdateStatusAsync(List<Guid> orderIds, string status, Guid? updatedBy = null)
        {
            var result = new BatchOperationResultDTO
            {
                TotalRequested = orderIds.Count,
                SuccessfulOperations = new List<Guid>(),
                FailedOperations = new List<BatchOperationError>()
            };

            if (!Enum.TryParse<OrderStatus>(status, out var orderStatus))
            {
                result.FailedOperations = orderIds.Select(id => new BatchOperationError
                {
                    Id = id,
                    Error = "Invalid status value"
                }).ToList();
                return result;
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var orderId in orderIds)
                {
                    try
                    {
                        var success = await _orderRepository.UpdateOrderStatusAsync(orderId, orderStatus, updatedBy);
                        if (success)
                            result.SuccessfulOperations.Add(orderId);
                        else
                            result.FailedOperations.Add(new BatchOperationError
                            {
                                Id = orderId,
                                Error = "Failed to update order status"
                            });
                    }
                    catch (Exception ex)
                    {
                        result.FailedOperations.Add(new BatchOperationError
                        {
                            Id = orderId,
                            Error = ex.Message
                        });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                result.SuccessCount = result.SuccessfulOperations.Count;
                result.FailureCount = result.FailedOperations.Count;
                result.IsSuccess = result.SuccessCount > 0;

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bulk update status operation");

                result.FailedOperations = orderIds.Select(id => new BatchOperationError
                {
                    Id = id,
                    Error = "Transaction failed"
                }).ToList();
                result.FailureCount = orderIds.Count;
                result.IsSuccess = false;

                return result;
            }
        }

        public async Task<decimal> CalculateOrderTotalAsync(Guid orderId)
        {
            try
            {
                return await _orderRepository.CalculateOrderTotalAsync(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating order total {OrderId}", orderId);
                return 0;
            }
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            try
            {
                return await _orderRepository.GenerateOrderNumberAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating order number");
                return string.Empty;
            }
        }

        public async Task<Dictionary<string, int>> GetOrderStatusCountsAsync()
        {
            try
            {
                var counts = await _orderRepository.GetOrderStatusCountsAsync();
                return counts.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status counts");
                return new Dictionary<string, int>();
            }
        }

        public async Task<IEnumerable<OrderDTO>> GetRecentOrdersAsync(int limit = 10)
        {
            try
            {
                var orders = await _orderRepository.GetRecentOrdersAsync(limit);
                return _mapper.Map<IEnumerable<OrderDTO>>(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders");
                return Enumerable.Empty<OrderDTO>();
            }
        }

        public async Task<IEnumerable<OrderDTO>> GetOrdersForAnalyticsAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var orders = await _orderRepository.GetOrdersForAnalyticsAsync(fromDate, toDate);
                return _mapper.Map<IEnumerable<OrderDTO>>(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for analytics");
                return Enumerable.Empty<OrderDTO>();
            }
        }

        public async Task<bool> UpdateTrackingNumberAsync(Guid orderId, string trackingNumber, Guid? updatedBy = null)
        {
            try
            {
                var result = await _orderRepository.UpdateTrackingNumberAsync(orderId, trackingNumber, updatedBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracking number {OrderId}", orderId);
                return false;
            }
        }

        #region Private Helper Methods

        private async Task<decimal> CalculateShippingFeeAsync(Guid? shippingMethodId, decimal subtotal)
        {
            if (!shippingMethodId.HasValue) return 0;

            // TODO: Implement shipping fee calculation based on method and subtotal
            // This would typically involve getting the shipping method from repository
            // and checking for free shipping thresholds
            return 25000m; // Default shipping fee
        }

        private async Task<(decimal discountAmount, decimal taxAmount)> CalculateDiscountAndTaxAsync(Guid? couponId, decimal subtotal)
        {
            decimal discountAmount = 0;
            decimal taxAmount = subtotal * 0.1m; // 10% tax

            if (couponId.HasValue)
            {
                // TODO: Implement coupon discount calculation
                // This would involve getting the coupon from repository
                // and calculating discount based on coupon type and value
                discountAmount = subtotal * 0.05m; // 5% discount as example
            }

            return (discountAmount, taxAmount);
        }

        #endregion
    }
}