using BusinessObjects.Cart;
using BusinessObjects.CustomDesigns;
using BusinessObjects.Orders;
using BusinessObjects.Products;
using DTOs.Cart;
using DTOs.Common;
using DTOs.Coupons;
using DTOs.OrderItem;
using DTOs.Orders;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Helpers.Mappers;
using Services.Interfaces;

namespace Services.Implementations
{
    public class OrderService : BaseService<Order, Guid>, IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderItemRepository _orderItemRepository; // Add this
        private readonly ICartItemService _cartItemService; // Add this
        private readonly IUserAddressService _userAddressService;
        private readonly ICouponService _couponService;
        private readonly IShippingMethodService _shippingMethodService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository repository,
            IOrderItemRepository orderItemRepository, // Add this
            ICartItemService cartItemService, // Add this
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            IUserAddressService userAddressService,
            ICouponService couponService,
            IShippingMethodService shippingMethodService,
            ILogger<OrderService> logger)
            : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _orderRepository = repository;
            _orderItemRepository = orderItemRepository; // Add this
            _cartItemService = cartItemService; // Add this
            _userAddressService = userAddressService;
            _couponService = couponService;
            _shippingMethodService = shippingMethodService;
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
                _logger.LogInformation("Creating order with request {@Request}", request);

                // Validate order items
                if (request.OrderItems == null || !request.OrderItems.Any())
                {
                    throw new ArgumentException("Đơn hàng phải có ít nhất một sản phẩm");
                }

                var userId = createdBy ?? _currentUserService.GetUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedAccessException("Không thể xác định người dùng hiện tại");
                }

                // Separate cart items from direct items
                var cartItemIds = request.OrderItems
                    .Where(item => item.CartItemId.HasValue)
                    .Select(item => item.CartItemId!.Value)
                    .ToList();

                var directItems = request.OrderItems
                    .Where(item => !item.CartItemId.HasValue)
                    .ToList();

                var orderItems = new List<OrderItem>();

                // Process cart items
                if (cartItemIds.Any())
                {
                    var cartItemsResult = await _cartItemService.GetCartItemsByIdsAsync(cartItemIds, userId, null);
                    if (!cartItemsResult.IsSuccess)
                    {
                        throw new ArgumentException($"Lỗi khi lấy cart items: {cartItemsResult.Message}");
                    }

                    var cartItems = cartItemsResult.Data ?? new List<CartItemDto>();

                    // Validate all requested cart items were found
                    if (cartItems.Count() != cartItemIds.Count)
                    {
                        throw new ArgumentException("Một số sản phẩm trong giỏ hàng không tồn tại hoặc không thuộc về bạn");
                    }

                    // Convert CartItems to OrderItems (will add orderId later)
                    var cartItemEntities = cartItems.Select(dto => new CartItem
                    {
                        Id = dto.Id,
                        ProductId = dto.ProductId,
                        CustomDesignId = dto.CustomDesignId,
                        ProductVariantId = dto.ProductVariantId,
                        Quantity = dto.Quantity,
                        UnitPrice = dto.UnitPrice,
                        Product = new Product { Name = dto.ProductName ?? "" },
                        CustomDesign = dto.CustomDesignId.HasValue ? new CustomDesign { DesignName = dto.ProductName ?? "" } : null,
                        ProductVariant = dto.ProductVariantId.HasValue ? new ProductVariant() : null
                    });

                    // Will set OrderId after order creation
                    var cartOrderItems = OrderItemMapper.CartItemsToOrderItems(cartItemEntities, Guid.Empty);
                    orderItems.AddRange(cartOrderItems);
                }

                // Process direct items
                foreach (var directItem in directItems)
                {
                    // Validate direct item data
                    if (!directItem.ProductId.HasValue && !directItem.CustomDesignId.HasValue && !directItem.ProductVariantId.HasValue)
                    {
                        throw new ArgumentException("Mỗi sản phẩm trong đơn hàng phải có ít nhất một trong các ID: ProductId, CustomDesignId, hoặc ProductVariantId");
                    }

                    if (!directItem.UnitPrice.HasValue || directItem.UnitPrice <= 0)
                    {
                        throw new ArgumentException("Giá sản phẩm phải lớn hơn 0");
                    }

                    if (!directItem.Quantity.HasValue || directItem.Quantity <= 0)
                    {
                        throw new ArgumentException("Số lượng sản phẩm phải lớn hơn 0");
                    }

                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = Guid.Empty, // Will set after order creation
                        ProductId = directItem.ProductId,
                        CustomDesignId = directItem.CustomDesignId,
                        ProductVariantId = directItem.ProductVariantId,
                        ItemName = directItem.ItemName ?? "Unknown Item",
                        SelectedColor = directItem.SelectedColor?.ToString(),
                        SelectedSize = directItem.SelectedSize?.ToString(),
                        Quantity = directItem.Quantity.Value,
                        UnitPrice = directItem.UnitPrice.Value,
                        TotalPrice = directItem.UnitPrice.Value * directItem.Quantity.Value
                    };

                    orderItems.Add(orderItem);
                }

                // Resolve shipping address
                var (shippingAddress, receiverName, receiverPhone) = await ResolveShippingAddressAsync(request, userId.Value);

                // Validate coupon if provided
                decimal discountAmount = 0;
                if (request.CouponId.HasValue)
                {
                    var couponResult = await _couponService.GetByIdAsync(request.CouponId.Value);
                    if (!couponResult.IsSuccess || couponResult.Data == null)
                    {
                        throw new ArgumentException("Mã giảm giá không hợp lệ");
                    }
                    // Calculate discount (implement your discount logic)
                    discountAmount = CalculateDiscount(couponResult.Data, orderItems.Sum(oi => oi.TotalPrice));
                }

                // Get shipping fee
                decimal shippingFee = 0;
                if (request.ShippingMethodId.HasValue)
                {
                    var shippingMethodResult = await _shippingMethodService.GetByIdAsync(request.ShippingMethodId.Value);
                    if (shippingMethodResult.IsSuccess && shippingMethodResult.Data != null)
                    {
                        shippingFee = shippingMethodResult.Data.Fee;
                    }
                }

                // Calculate totals
                var subtotal = orderItems.Sum(oi => oi.TotalPrice);
                var taxAmount = subtotal * 0.1m; // 10% VAT
                var totalAmount = subtotal + shippingFee + taxAmount - discountAmount;

                // Create Order
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = await _orderRepository.GenerateOrderNumberAsync(),
                    UserId = userId.Value,
                    ShippingAddress = shippingAddress,
                    ReceiverName = receiverName,
                    ReceiverPhone = receiverPhone,
                    CustomerNotes = request.CustomerNotes,
                    CouponId = request.CouponId,
                    ShippingMethodId = request.ShippingMethodId,
                    ShippingFee = shippingFee,
                    TaxAmount = taxAmount,
                    DiscountAmount = discountAmount,
                    TotalAmount = totalAmount,
                    Status = OrderStatus.Pending,
                    PaymentStatus = PaymentStatus.Unpaid,
                    CreatedAt = _currentTime.GetVietnamTime(),
                    CreatedBy = userId.Value
                };

                // Save Order
                var createdOrder = await _orderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // Update OrderItems with correct OrderId and save
                foreach (var orderItem in orderItems)
                {
                    orderItem.OrderId = createdOrder.Id;
                    await _orderItemRepository.AddAsync(orderItem);
                }
                await _unitOfWork.SaveChangesAsync();

                // Sau khi save OrderItems, SubtotalAmount sẽ tự động tính được
                // Nếu cần cập nhật TotalAmount dựa trên SubtotalAmount thực tế:
                var actualSubtotal = createdOrder.SubtotalAmount; // Computed property
                createdOrder.TotalAmount = actualSubtotal + shippingFee + taxAmount - discountAmount;
                await _orderRepository.UpdateAsync(createdOrder);
                await _unitOfWork.SaveChangesAsync();

                // Clear cart items that were used
                if (cartItemIds.Any())
                {
                    var clearResult = await _cartItemService.ClearCartItemsByIdsAsync(cartItemIds, userId, null);
                    if (!clearResult.IsSuccess)
                    {
                        _logger.LogWarning("Failed to clear cart items after order creation: {Message}", clearResult.Message);
                        // Don't throw here, order was created successfully
                    }
                }

                await transaction.CommitAsync();

                // Return created order with details
                var orderWithDetails = await _orderRepository.GetOrderWithDetailsAsync(createdOrder.Id);
                return orderWithDetails != null ? ConvertToOrderDTO(orderWithDetails) : null;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        private decimal CalculateDiscount(CouponDto coupon, decimal subtotal)
        {
            // Validate coupon is active and not expired
            if (coupon.Status != CouponStatus.Active)
                return 0;

            if (DateTime.UtcNow < coupon.StartDate || DateTime.UtcNow > coupon.EndDate)
                return 0;

            // Check minimum order amount
            if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
                return 0;

            decimal discountAmount = 0;

            // Calculate discount based on coupon type
            if (coupon.Type == CouponType.Percentage)
            {
                discountAmount = subtotal * (coupon.Value / 100m);
            }
            else if (coupon.Type == CouponType.FixedAmount)
            {
                discountAmount = coupon.Value;
            }

            // Apply max discount limit
            if (coupon.MaxDiscountAmount.HasValue)
            {
                discountAmount = Math.Min(discountAmount, coupon.MaxDiscountAmount.Value);
            }

            // Ensure discount doesn't exceed subtotal
            return Math.Min(discountAmount, subtotal);
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
                    // Validate the new coupon before applying
                    var couponResult = await _couponService.GetByIdAsync(request.CouponId.Value);
                    if (!couponResult.IsSuccess || couponResult.Data == null)
                    {
                        throw new ArgumentException("Mã giảm giá không tồn tại");
                    }

                    // Validate coupon eligibility
                    var validationResult = await _couponService.ValidateCouponAsync(couponResult.Data.Code, order.TotalAmount, order.UserId);
                    if (!validationResult.IsSuccess || !validationResult.Data)
                    {
                        throw new ArgumentException("Mã giảm giá không hợp lệ hoặc không áp dụng được: " + (validationResult.Message ?? ""));
                    }

                    order.CouponId = request.CouponId;
                    // Recalculate discount and total if coupon changed
                    var (discountAmount, _) = await CalculateDiscountAndTaxAsync(request.CouponId, order.TotalAmount);
                    order.DiscountAmount = discountAmount;
                }
                else if (request.CouponId == null && order.CouponId.HasValue)
                {
                    // Remove coupon if explicitly set to null
                    order.CouponId = null;
                    order.DiscountAmount = 0;
                }

                if (request.ShippingMethodId.HasValue)
                {
                    // Validate shipping method exists and is active
                    var shippingMethodValidation = await _shippingMethodService.ValidateShippingMethodAsync(request.ShippingMethodId.Value);
                    if (!shippingMethodValidation.IsSuccess)
                    {
                        throw new ArgumentException("Phương thức vận chuyển không hợp lệ: " + (shippingMethodValidation.Message ?? ""));
                    }

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
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentException("Lý do hủy đơn hàng là bắt buộc");
                }

                var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new ArgumentException("Đơn hàng không tồn tại");
                }

                if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                {
                    throw new InvalidOperationException("Không thể hủy đơn hàng đã giao hoặc đã hủy");
                }

                _logger.LogInformation("Cancelling order {OrderId} with reason: {Reason}", orderId, reason);

                // Cancel the order in repository
                var result = await _orderRepository.CancelOrderAsync(orderId, reason.Trim(), cancelledBy);
                
                if (result)
                {
                    // Handle coupon rollback if order used a coupon
                    if (order.CouponId.HasValue)
                    {
                        try
                        {
                            // Note: The actual coupon usage rollback would depend on how the CouponService tracks usage
                            // This is a placeholder for the actual implementation
                            _logger.LogInformation("Order {OrderId} used coupon {CouponId}, consider implementing rollback logic", 
                                orderId, order.CouponId);
                            
                            // TODO: Implement coupon usage rollback if needed
                            // await _couponService.RollbackCouponUsageAsync(order.CouponId.Value, order.UserId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to rollback coupon usage for order {OrderId}, coupon {CouponId}", 
                                orderId, order.CouponId);
                            // Don't fail the entire operation for coupon rollback issues
                        }
                    }

                    await transaction.CommitAsync();
                    _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);
                }
                else
                {
                    await transaction.RollbackAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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
                // Use ShippingMethodService to calculate the shipping fee
                var shippingFee = await _shippingMethodService.CalculateShippingFeeAsync(shippingMethodId.Value, subtotal);
                return shippingFee;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating shipping fee for method {ShippingMethodId}, using default", shippingMethodId);
                
                // Fallback logic
                var defaultFee = 25000m;

                // Free shipping for orders over 500,000 VND
                if (subtotal >= 500000m) return 0;

                return defaultFee;
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
                    // Get coupon details first
                    var couponResult = await _couponService.GetByIdAsync(couponId.Value);
                    if (couponResult.IsSuccess && couponResult.Data != null)
                    {
                        // Validate coupon
                        var validationResult = await _couponService.ValidateCouponAsync(couponResult.Data.Code, subtotal);
                        if (validationResult.IsSuccess && validationResult.Data)
                        {
                            // Calculate discount
                            var discountResult = await _couponService.CalculateDiscountAsync(couponResult.Data.Code, subtotal);
                            if (discountResult.IsSuccess)
                            {
                                discountAmount = discountResult.Data;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Coupon validation failed for coupon {CouponId}: {Message}", couponId, validationResult.Message);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Coupon not found or invalid for coupon {CouponId}", couponId);
                    }
                }

                return (discountAmount, taxAmount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating discount and tax, using defaults");
                return (0, subtotal * 0.1m);
            }
        }

        private async Task<(string shippingAddress, string receiverName, string receiverPhone)> ResolveShippingAddressAsync(CreateOrderRequest request, Guid userId)
        {
            try
            {
                // If UserAddressId is provided, use existing address
                if (request.UserAddressId.HasValue)
                {
                    var addressResult = await _userAddressService.GetUserAddressByIdAsync(request.UserAddressId.Value);
                    if (addressResult.IsSuccess && addressResult.Data != null)
                    {
                        var address = addressResult.Data;
                        return (address.FullAddress, address.ReceiverName, address.Phone);
                    }
                    else
                    {
                        throw new ArgumentException("Địa chỉ được chọn không tồn tại hoặc không thuộc về người dùng");
                    }
                }

                // If NewAddress is provided, create new address and use it
                if (request.NewAddress != null)
                {
                    var createAddressResult = await _userAddressService.CreateUserAddressAsync(request.NewAddress);
                    if (createAddressResult.IsSuccess && createAddressResult.Data != null)
                    {
                        var address = createAddressResult.Data;
                        return (address.FullAddress, address.ReceiverName, address.Phone);
                    }
                    else
                    {
                        throw new ArgumentException("Không thể tạo địa chỉ mới: " + (createAddressResult.Message ?? "Lỗi không xác định"));
                    }
                }

                // If neither is provided, try to get default address
                var defaultAddressResult = await _userAddressService.GetDefaultAddressAsync();
                if (defaultAddressResult.IsSuccess && defaultAddressResult.Data != null)
                {
                    var address = defaultAddressResult.Data;
                    return (address.FullAddress, address.ReceiverName, address.Phone);
                }

                throw new ArgumentException("Không tìm thấy địa chỉ giao hàng. Vui lòng chọn địa chỉ có sẵn hoặc tạo địa chỉ mới.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving shipping address for user {UserId}", userId);
                throw;
            }
        }
        private OrderDTO ConvertToOrderDTO(Order order)
        {
            if (order == null) return null;

            return new OrderDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                SubtotalAmount = order.SubtotalAmount,
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