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
                    if (!item.CartItemId.HasValue && !item.ProductId.HasValue && !item.CustomDesignId.HasValue && !item.ProductVariantId.HasValue)
                    {
                        throw new ArgumentException("Mỗi sản phẩm trong đơn hàng phải có CartItemId hoặc ít nhất một trong các ID: ProductId, CustomDesignId, ProductVariantId");
                    }
                }

                var userId = createdBy ?? _currentUserService.GetUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedAccessException("Không thể xác định người dùng hiện tại");
                }

                // Handle address - either use existing or create new
                var (shippingAddress, receiverName, receiverPhone) = await HandleOrderAddressAsync(request, userId.Value);

                // Generate order number
                var orderNumber = await _orderRepository.GenerateOrderNumberAsync();

                // Process order items and calculate prices
                var orderItems = new List<OrderItem>();
                decimal subtotal = 0;

                foreach (var itemRequest in request.OrderItems)
                {
                    var (orderItem, itemPrice) = await ProcessOrderItemAsync(itemRequest, userId.Value);
                    orderItems.Add(orderItem);
                    subtotal += itemPrice;
                }

                // Calculate totals
                var shippingFee = await CalculateShippingFeeAsync(request.ShippingMethodId, subtotal);
                var (discountAmount, taxAmount) = await CalculateDiscountAndTaxAsync(request.CouponId, subtotal);
                var totalAmount = subtotal + shippingFee + taxAmount - discountAmount;

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
                    ShippingAddress = shippingAddress,
                    ReceiverName = receiverName,
                    ReceiverPhone = receiverPhone,
                    CustomerNotes = request.CustomerNotes,
                    CouponId = request.CouponId,
                    ShippingMethodId = request.ShippingMethodId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId.Value,
                    OrderItems = orderItems
                };

                // Update order IDs for all items
                foreach (var item in orderItems)
                {
                    item.OrderId = order.Id;
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

                // ✅ Chỉ cập nhật các trường có trong UpdateOrderRequest
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

                // ❌ Loại bỏ những dòng này vì không còn trong UpdateOrderRequest:
                // - EstimatedDeliveryDate
                // - TrackingNumber 
                // - AssignedStaffId

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

        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, Guid? updatedBy = null)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                    throw new ArgumentException("Đơn hàng không tồn tại");

                var result = await _orderRepository.UpdateOrderStatusAsync(orderId, status, updatedBy);
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

        public async Task<bool> UpdatePaymentStatusAsync(Guid orderId, PaymentStatus paymentStatus, Guid? updatedBy = null)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                    throw new ArgumentException("Đơn hàng không tồn tại");

                var result = await _orderRepository.UpdatePaymentStatusAsync(orderId, paymentStatus, updatedBy);
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

        public async Task<BatchOperationResultDTO> BulkUpdateStatusAsync(List<Guid> orderIds, OrderStatus status, Guid? updatedBy = null)
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

                        var success = await _orderRepository.UpdateOrderStatusAsync(orderId, status, updatedBy);
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

        public async Task<Dictionary<OrderStatus, int>> GetOrderStatusCountsAsync()
        {
            try
            {
                return await _orderRepository.GetOrderStatusCountsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status counts");
                return new Dictionary<OrderStatus, int>();
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
                    // TODO: Implement proper coupon discount calculation with ICouponService
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

        private async Task<(string shippingAddress, string receiverName, string receiverPhone)> HandleOrderAddressAsync(CreateOrderRequest request, Guid userId)
        {
            if (request.UserAddressId.HasValue)
            {
                // Use existing address
                // TODO: Inject IUserAddressRepository and fetch the address
                // For now, throw an exception to indicate this needs implementation
                throw new NotImplementedException("Sử dụng địa chỉ đã lưu chưa được triển khai đầy đủ");
            }
            else if (request.NewAddress != null)
            {
                // Use new address from request
                var shippingAddress = $"{request.NewAddress.DetailAddress}, {request.NewAddress.Ward}, {request.NewAddress.District}, {request.NewAddress.Province}";
                if (!string.IsNullOrEmpty(request.NewAddress.PostalCode))
                {
                    shippingAddress += $", {request.NewAddress.PostalCode}";
                }
                return (shippingAddress, request.NewAddress.ReceiverName, request.NewAddress.Phone);
            }
            else
            {
                // Try to load default address
                // TODO: Inject IUserAddressRepository and fetch default address
                // For now, throw an exception to indicate address is required
                throw new ArgumentException("Phải cung cấp địa chỉ giao hàng hoặc chọn địa chỉ đã lưu");
            }
        }

        private async Task<(OrderItem orderItem, decimal itemPrice)> ProcessOrderItemAsync(CreateOrderItemRequest itemRequest, Guid userId)
        {
            decimal unitPrice = 0;
            string itemName = itemRequest.ItemName ?? "Unknown Item";
            int quantity = itemRequest.Quantity ?? 1;

            if (itemRequest.CartItemId.HasValue)
            {
                // TODO: Implement cart item processing
                // Should fetch cart item details and get price from there
                throw new NotImplementedException("Xử lý từ giỏ hàng chưa được triển khai đầy đủ");
            }
            else
            {
                // Direct product order - need to calculate price
                // TODO: Implement product price lookup from ProductId/CustomDesignId/ProductVariantId
                // For now, use a default price
                unitPrice = 100000m; // Default price
            }

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = itemRequest.ProductId,
                CustomDesignId = itemRequest.CustomDesignId,
                ProductVariantId = itemRequest.ProductVariantId,
                ItemName = itemName,
                SelectedColor = itemRequest.SelectedColor?.ToString(),
                SelectedSize = itemRequest.SelectedSize?.ToString(),
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * quantity,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            return (orderItem, orderItem.TotalPrice);
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
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
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