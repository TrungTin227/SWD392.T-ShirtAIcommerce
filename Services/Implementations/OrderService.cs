using BusinessObjects.Orders;
using BusinessObjects.Products;
using DTOs.Common;
using DTOs.OrderItem;
using DTOs.Orders;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Implementations
{
    public class OrderService : BaseService<Order, Guid>, IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository repository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            ILogger<OrderService> logger)
            : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _orderRepository = repository;
            _logger = logger;
        }

        public async Task<PagedResponse<OrderDTO>> GetOrdersAsync(OrderFilterRequest filter)
        {
            try
            {
                var pagedOrders = await _orderRepository.GetOrdersAsync(filter);
                var orderDTOs = pagedOrders.Select(ConvertToOrderDTO).ToList();

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
                return order != null ? ConvertToOrderDTO(order) : null;
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
                // Validate order items
                if (request.OrderItems == null || !request.OrderItems.Any())
                {
                    throw new ArgumentException("Đơn hàng phải có ít nhất một sản phẩm");
                }

                // Validate each order item has at least one product reference
                foreach (var item in request.OrderItems)
                {
                    if (!item.ProductId.HasValue && !item.CustomDesignId.HasValue && !item.ProductVariantId.HasValue)
                    {
                        throw new ArgumentException("Mỗi sản phẩm trong đơn hàng phải có ít nhất một trong các ID: ProductId, CustomDesignId, hoặc ProductVariantId");
                    }
                }

                // Generate order number
                var orderNumber = await _orderRepository.GenerateOrderNumberAsync();

                // Calculate totals
                var subtotal = request.OrderItems.Sum(item => item.UnitPrice * item.Quantity);
                var shippingFee = await CalculateShippingFeeAsync(request.ShippingMethodId, subtotal);
                var (discountAmount, taxAmount) = await CalculateDiscountAndTaxAsync(request.CouponId, subtotal);
                var totalAmount = subtotal + shippingFee + taxAmount - discountAmount;

                var userId = createdBy ?? _currentUserService.GetUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedAccessException("Không thể xác định người dùng hiện tại");
                }

                // Create order
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    UserId = userId.Value,
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
                    CreatedBy = userId.Value,
                    OrderItems = new List<OrderItem>()
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
                        TotalPrice = OrderItemBusinessLogic.CalculateTotalPrice(itemRequest.UnitPrice, itemRequest.Quantity),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId.Value
                    };

                    order.OrderItems.Add(orderItem);
                }

                var createdOrder = await CreateAsync(order);
                await transaction.CommitAsync();

                return ConvertToOrderDTO(createdOrder);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order {@Request}", request);
                throw;
            }
        }

        public async Task<OrderDTO?> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, Guid? updatedBy = null)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new ArgumentException("Đơn hàng không tồn tại");
                }

                // Only allow updates for certain statuses
                if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                {
                    throw new InvalidOperationException("Không thể cập nhật đơn hàng đã giao hoặc đã hủy");
                }

                var userId = updatedBy ?? _currentUserService.GetUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedAccessException("Không thể xác định người dùng hiện tại");
                }

                // Update fields if provided
                if (!string.IsNullOrWhiteSpace(request.ShippingAddress))
                    order.ShippingAddress = request.ShippingAddress.Trim();

                if (!string.IsNullOrWhiteSpace(request.ReceiverName))
                    order.ReceiverName = request.ReceiverName.Trim();

                if (!string.IsNullOrWhiteSpace(request.ReceiverPhone))
                    order.ReceiverPhone = request.ReceiverPhone.Trim();

                if (request.CustomerNotes != null)
                    order.CustomerNotes = request.CustomerNotes.Trim();

                if (request.CouponId.HasValue)
                {
                    order.CouponId = request.CouponId;
                    // Recalculate discount and total if coupon changed
                    var (discountAmount, _) = await CalculateDiscountAndTaxAsync(request.CouponId, order.TotalAmount);
                    order.DiscountAmount = discountAmount;
                }

                if (request.ShippingMethodId.HasValue)
                {
                    order.ShippingMethodId = request.ShippingMethodId;
                    // Recalculate shipping fee if method changed
                    order.ShippingFee = await CalculateShippingFeeAsync(request.ShippingMethodId, order.TotalAmount);
                }

                if (request.EstimatedDeliveryDate.HasValue)
                    order.EstimatedDeliveryDate = request.EstimatedDeliveryDate;

                if (!string.IsNullOrWhiteSpace(request.TrackingNumber))
                    order.TrackingNumber = request.TrackingNumber.Trim();

                if (request.AssignedStaffId.HasValue)
                    order.AssignedStaffId = request.AssignedStaffId;

                // Update audit fields
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = userId.Value;

                var updatedOrder = await UpdateAsync(order);
                return ConvertToOrderDTO(updatedOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> DeleteOrderAsync(Guid orderId, Guid? deletedBy = null)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                    return false;

                // Only allow deletion for pending orders
                if (order.Status != OrderStatus.Pending)
                {
                    throw new InvalidOperationException("Chỉ có thể xóa đơn hàng đang chờ xử lý");
                }

                return await DeleteAsync(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<IEnumerable<OrderDTO>> GetUserOrdersAsync(Guid userId)
        {
            try
            {
                var orders = await _orderRepository.GetUserOrdersAsync(userId);
                return orders.Select(ConvertToOrderDTO);
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
                if (string.IsNullOrWhiteSpace(status))
                {
                    throw new ArgumentException("Trạng thái đơn hàng không được để trống");
                }

                if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                {
                    throw new ArgumentException($"Trạng thái đơn hàng không hợp lệ: {status}");
                }

                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new ArgumentException("Đơn hàng không tồn tại");
                }

                // Validate status transition
                if (!IsValidStatusTransition(order.Status, orderStatus))
                {
                    throw new InvalidOperationException($"Không thể chuyển trạng thái từ {order.Status} sang {orderStatus}");
                }

                var result = await _orderRepository.UpdateOrderStatusAsync(orderId, orderStatus, updatedBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status {OrderId} to {Status}", orderId, status);
                throw;
            }
        }

        public async Task<bool> UpdatePaymentStatusAsync(Guid orderId, string paymentStatus, Guid? updatedBy = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(paymentStatus))
                {
                    throw new ArgumentException("Trạng thái thanh toán không được để trống");
                }

                if (!Enum.TryParse<PaymentStatus>(paymentStatus, true, out var paymentStatusEnum))
                {
                    throw new ArgumentException($"Trạng thái thanh toán không hợp lệ: {paymentStatus}");
                }

                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new ArgumentException("Đơn hàng không tồn tại");
                }

                var result = await _orderRepository.UpdatePaymentStatusAsync(orderId, paymentStatusEnum, updatedBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status {OrderId} to {PaymentStatus}", orderId, paymentStatus);
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(Guid orderId, string reason, Guid? cancelledBy = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentException("Lý do hủy đơn hàng là bắt buộc");
                }

                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new ArgumentException("Đơn hàng không tồn tại");
                }

                if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                {
                    throw new InvalidOperationException("Không thể hủy đơn hàng đã giao hoặc đã hủy");
                }

                var result = await _orderRepository.CancelOrderAsync(orderId, reason.Trim(), cancelledBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> AssignOrderToStaffAsync(Guid orderId, Guid staffId, Guid? updatedBy = null)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new ArgumentException("Đơn hàng không tồn tại");
                }

                if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
                {
                    throw new InvalidOperationException("Không thể phân công đơn hàng đã hủy hoặc đã giao");
                }

                var result = await _orderRepository.AssignOrderToStaffAsync(orderId, staffId, updatedBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning order {OrderId} to staff {StaffId}", orderId, staffId);
                throw;
            }
        }

        public async Task<IEnumerable<OrderDTO>> GetStaffOrdersAsync(Guid staffId)
        {
            try
            {
                var orders = await _orderRepository.GetStaffOrdersAsync(staffId);
                return orders.Select(ConvertToOrderDTO);
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
                TotalRequested = orderIds?.Count ?? 0,
                SuccessIds = new List<string>(),
                Errors = new List<BatchOperationErrorDTO>()
            };

            if (orderIds == null || !orderIds.Any())
            {
                result.Errors.Add(new BatchOperationErrorDTO
                {
                    Id = Guid.Empty.ToString(),
                    ErrorMessage = "Danh sách ID đơn hàng không được để trống"
                });
                result.FailureCount = 1;
                result.Message = "Danh sách ID đơn hàng không hợp lệ";
                return result;
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                result.Errors = orderIds.Select(id => new BatchOperationErrorDTO
                {
                    Id = id.ToString(),
                    ErrorMessage = "Trạng thái không được để trống"
                }).ToList();
                result.FailureCount = orderIds.Count;
                result.Message = "Trạng thái không hợp lệ";
                return result;
            }

            if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            {
                result.Errors = orderIds.Select(id => new BatchOperationErrorDTO
                {
                    Id = id.ToString(),
                    ErrorMessage = $"Trạng thái không hợp lệ: {status}"
                }).ToList();
                result.FailureCount = orderIds.Count;
                result.Message = $"Trạng thái '{status}' không hợp lệ";
                return result;
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var successfulIds = new List<string>();
                var errors = new List<BatchOperationErrorDTO>();

                foreach (var orderId in orderIds)
                {
                    try
                    {
                        var order = await _orderRepository.GetByIdAsync(orderId);
                        if (order == null || order.IsDeleted)
                        {
                            errors.Add(new BatchOperationErrorDTO
                            {
                                Id = orderId.ToString(),
                                ErrorMessage = "Đơn hàng không tồn tại"
                            });
                            continue;
                        }

                        if (!IsValidStatusTransition(order.Status, orderStatus))
                        {
                            errors.Add(new BatchOperationErrorDTO
                            {
                                Id = orderId.ToString(),
                                ErrorMessage = $"Không thể chuyển trạng thái từ {order.Status} sang {orderStatus}"
                            });
                            continue;
                        }

                        var success = await _orderRepository.UpdateOrderStatusAsync(orderId, orderStatus, updatedBy);
                        if (success)
                        {
                            successfulIds.Add(orderId.ToString());
                        }
                        else
                        {
                            errors.Add(new BatchOperationErrorDTO
                            {
                                Id = orderId.ToString(),
                                ErrorMessage = "Cập nhật trạng thái thất bại"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new BatchOperationErrorDTO
                        {
                            Id = orderId.ToString(),
                            ErrorMessage = ex.Message
                        });
                        _logger.LogError(ex, "Error updating order {OrderId} status to {Status}", orderId, status);
                    }
                }

                // Only commit if we have at least one success
                if (successfulIds.Any())
                {
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                }

                result.SuccessIds = successfulIds;
                result.Errors = errors;
                result.SuccessCount = successfulIds.Count;
                result.FailureCount = errors.Count;

                // Set appropriate message based on results
                if (result.IsCompleteSuccess)
                {
                    result.Message = $"Đã cập nhật thành công trạng thái cho tất cả {result.SuccessCount} đơn hàng";
                }
                else if (result.IsCompleteFailure)
                {
                    result.Message = $"Không thể cập nhật trạng thái cho bất kỳ đơn hàng nào. {result.FailureCount} đơn hàng thất bại";
                }
                else if (result.IsPartialSuccess)
                {
                    result.Message = $"Cập nhật một phần thành công: {result.SuccessCount} thành công, {result.FailureCount} thất bại";
                }

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bulk update status operation for orders: {OrderIds}", string.Join(", ", orderIds));

                result.Errors = orderIds.Select(id => new BatchOperationErrorDTO
                {
                    Id = id.ToString(),
                    ErrorMessage = "Giao dịch thất bại do lỗi hệ thống"
                }).ToList();
                result.FailureCount = orderIds.Count;
                result.SuccessCount = 0;
                result.Message = "Cập nhật thất bại do lỗi hệ thống";

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
                if (limit <= 0) limit = 10;
                if (limit > 100) limit = 100; // Prevent too large requests

                var orders = await _orderRepository.GetRecentOrdersAsync(limit);
                return orders.Select(ConvertToOrderDTO);
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
                if (fromDate > toDate)
                {
                    throw new ArgumentException("Ngày bắt đầu không thể lớn hơn ngày kết thúc");
                }

                var orders = await _orderRepository.GetOrdersForAnalyticsAsync(fromDate, toDate);
                return orders.Select(ConvertToOrderDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for analytics");
                throw;
            }
        }

        public async Task<bool> UpdateTrackingNumberAsync(Guid orderId, string trackingNumber, Guid? updatedBy = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(trackingNumber))
                {
                    throw new ArgumentException("Mã vận đơn không được để trống");
                }

                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new ArgumentException("Đơn hàng không tồn tại");
                }

                if (order.Status != OrderStatus.Shipping && order.Status != OrderStatus.Confirmed)
                {
                    throw new InvalidOperationException("Chỉ có thể cập nhật mã vận đơn cho đơn hàng đã xác nhận hoặc đang giao");
                }

                var result = await _orderRepository.UpdateTrackingNumberAsync(orderId, trackingNumber.Trim(), updatedBy);
                if (result)
                    await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracking number {OrderId}", orderId);
                throw;
            }
        }

        #region Private Helper Methods

        private async Task<decimal> CalculateShippingFeeAsync(Guid? shippingMethodId, decimal subtotal)
        {
            if (!shippingMethodId.HasValue) return 0;

            try
            {
                // TODO: Implement proper shipping fee calculation
                // For now, return a default fee
                var defaultFee = 25000m;

                // Free shipping for orders over 500,000 VND
                if (subtotal >= 500000m) return 0;

                return defaultFee;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating shipping fee, using default");
                return 25000m; // Default shipping fee
            }
        }

        private async Task<(decimal discountAmount, decimal taxAmount)> CalculateDiscountAndTaxAsync(Guid? couponId, decimal subtotal)
        {
            try
            {
                decimal discountAmount = 0;
                decimal taxAmount = subtotal * 0.1m; // 10% VAT

                if (couponId.HasValue)
                {
                    // TODO: Implement proper coupon discount calculation
                    // For now, apply a simple percentage discount
                    discountAmount = subtotal * 0.05m; // 5% discount
                }

                return (discountAmount, taxAmount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating discount and tax, using defaults");
                return (0, subtotal * 0.1m);
            }
        }

        private static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            // Define valid status transitions
            return currentStatus switch
            {
                OrderStatus.Pending => newStatus is OrderStatus.Confirmed or OrderStatus.Cancelled,
                OrderStatus.Confirmed => newStatus is OrderStatus.Processing or OrderStatus.Cancelled,
                OrderStatus.Processing => newStatus is OrderStatus.Shipping or OrderStatus.Cancelled,
                OrderStatus.Shipping => newStatus is OrderStatus.Delivered or OrderStatus.Returned,
                OrderStatus.Delivered => newStatus is OrderStatus.Returned,
                OrderStatus.Cancelled => false, // Cannot change from cancelled
                OrderStatus.Returned => false, // Cannot change from returned
                _ => false
            };
        }

        private OrderDTO ConvertToOrderDTO(Order order)
        {
            if (order == null) return null;

            return new OrderDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                TotalAmount = order.TotalAmount,
                ShippingFee = order.ShippingFee,
                DiscountAmount = order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                ShippingAddress = order.ShippingAddress,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                CustomerNotes = order.CustomerNotes,
                EstimatedDeliveryDate = order.EstimatedDeliveryDate,
                TrackingNumber = order.TrackingNumber,
                AssignedStaffId = order.AssignedStaffId,
                CouponId = order.CouponId,
                ShippingMethodId = order.ShippingMethodId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                CreatedBy = order.CreatedBy,
                UpdatedBy = order.UpdatedBy,
                UserName = order.User?.UserName ?? "",
                AssignedStaffName = order.AssignedStaff?.UserName ?? "",
                CouponCode = order.Coupon?.Code ?? "",
                ShippingMethodName = order.ShippingMethod != null
                    ? order.ShippingMethod.Name.ToString()
                    : string.Empty,
                OrderItems = order.OrderItems?.Select(ConvertToOrderItemDto).ToList() ?? new List<OrderItemDto>()
            };
        }

        private OrderItemDto ConvertToOrderItemDto(OrderItem orderItem)
        {
            if (orderItem == null) return null;

            return new OrderItemDto
            {
                Id = orderItem.Id,
                OrderId = orderItem.OrderId,
                ProductId = orderItem.ProductId,
                CustomDesignId = orderItem.CustomDesignId,
                ProductVariantId = orderItem.ProductVariantId,
                ItemName = orderItem.ItemName,
                SelectedColor = orderItem.SelectedColor,
                SelectedSize = orderItem.SelectedSize,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.TotalPrice
            };
        }

        #endregion
    }
}