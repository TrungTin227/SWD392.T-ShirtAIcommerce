using BusinessObjects.Common;
using BusinessObjects.Orders;
using Data.Repositories.CustomDesigns;
using DTOs.Analytics;
using DTOs.Common;
using DTOs.Coupons;
using DTOs.OrderItem;
using DTOs.Orders;
using DTOs.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Helpers;
using Services.Helpers.Mappers;
using Services.Interfaces;

namespace Services.Implementations
{
    public class OrderService : BaseService<Order, Guid>, IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly ICartItemRepository _cartItemRepository;
        private readonly ICartItemService _cartItemService;
        private readonly IUserAddressService _userAddressService;
        private readonly ICouponService _couponService;
        private readonly IShippingMethodService _shippingMethodService;
        private readonly IProductVariantRepository _productVariantRepository;
        private readonly IAICustomDesignRepository _customDesignRepository;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository repository,
            IOrderItemRepository orderItemRepository,
            IPaymentRepository paymentRepository,
            ICartItemService cartItemService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            IUserAddressService userAddressService,
            ICouponService couponService,
            IShippingMethodService shippingMethodService,
            IProductVariantRepository productVariantRepository,
            IAICustomDesignRepository customDesignedRepository,
            ICartItemRepository cartItemRepository,
            IPaymentService paymentService,


            ILogger<OrderService> logger)
            : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _orderRepository = repository;
            _orderItemRepository = orderItemRepository;
            _cartItemRepository = cartItemRepository;
            _cartItemService = cartItemService;
            _userAddressService = userAddressService;
            _couponService = couponService;
            _shippingMethodService = shippingMethodService;
            _productVariantRepository = productVariantRepository;
            _customDesignRepository = customDesignedRepository;
            _paymentService = paymentService;
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

        public async Task<CreateOrderResult> CreateOrderAsync(
            CreateOrderRequest request,
            Guid? createdBy = null)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Xác định user
                var userId = createdBy ?? _currentUserService.GetUserId();
                if (!userId.HasValue)
                    throw new UnauthorizedAccessException("Không xác định được người dùng.");

                // 2. Phân loại cart items vs direct items
                var cartItemIds = request.OrderItems
                    .Where(i => i.CartItemId.HasValue)
                    .Select(i => i.CartItemId!.Value)
                    .ToList();
                var directItems = request.OrderItems
                    .Where(i => !i.CartItemId.HasValue)
                    .ToList();

                // 3. Lấy địa chỉ giao hàng
                var (shippingAddress, receiverName, receiverPhone) =
                    await ResolveShippingAddressAsync(request, userId.Value);

                // 4. Build danh sách OrderItem và giảm tồn kho
                var orderItems = new List<OrderItem>();

                // 4.1. Cart items
                if (cartItemIds.Any())
                {
                    var cartEntities = await _cartItemRepository.GetAllAsync(
                        ci => cartItemIds.Contains(ci.Id),
                        orderBy: null,
                        ci => ci.ProductVariant,
                        ci => ci.Product);

                    if (cartEntities.Count != cartItemIds.Count)
                        throw new ArgumentException("Một số CartItem không tồn tại.");

                    foreach (var ci in cartEntities)
                    {
                        if (ci.ProductVariant != null)
                        {
                            if (ci.ProductVariant.Quantity < ci.Quantity)
                                throw new ArgumentException(
                                    $"Sản phẩm '{ci.ProductVariant.Product?.Name}' không đủ tồn kho.");

                            ci.ProductVariant.Quantity -= ci.Quantity;
                            await _productVariantRepository.UpdateAsync(ci.ProductVariant);
                        }

                        orderItems.Add(new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = ci.ProductId,
                            ProductVariantId = ci.ProductVariantId,
                            CustomDesignId = ci.CustomDesignId,
                            Quantity = ci.Quantity,
                            UnitPrice = ci.UnitPrice,
                            TotalPrice = ci.TotalPrice,
                            ItemName = ci.Product?.Name ?? string.Empty
                        });
                    }
                }

                // 4.2. Direct items
                foreach (var di in directItems)
                {
                    if (!di.ProductVariantId.HasValue)
                        throw new ArgumentException("Direct item phải có ProductVariantId.");

                    var variant = await _productVariantRepository
                        .GetByIdWithProductAsync(di.ProductVariantId.Value);
                    if (variant == null)
                        throw new ArgumentException("Không tìm thấy sản phẩm biến thể.");

                    if (variant.Quantity < di.Quantity)
                        throw new ArgumentException(
                            $"Sản phẩm '{variant.Product?.Name}' không đủ tồn kho.");

                    variant.Quantity -= di.Quantity.Value;
                    await _productVariantRepository.UpdateAsync(variant);

                    var unitPrice = (variant.Product?.Price ?? 0m) + (variant.PriceAdjustment ?? 0m);
                    orderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductVariantId = di.ProductVariantId,
                        Quantity = di.Quantity.Value,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * di.Quantity.Value,
                        ItemName = variant.Product?.Name ?? string.Empty
                    });
                }

                // 5. Tính toán subtotal
                var subtotal = orderItems.Sum(oi => oi.TotalPrice);

                // 5.1. Validate và tính discount
                decimal discount = 0m;
                CouponDto? validatedCoupon = null;

                if (request.CouponId.HasValue)
                {
                    var couponRes = await _couponService.GetByIdAsync(request.CouponId.Value);
                    if (!couponRes.IsSuccess || couponRes.Data == null)
                        throw new ArgumentException("Coupon không tồn tại.");

                    validatedCoupon = couponRes.Data;

                    var isValidCoupon = await _unitOfWork.CouponRepository.ValidateCouponForOrderAsync(
                        request.CouponId.Value,
                        subtotal,
                        userId.Value);
                    if (!isValidCoupon)
                        throw new ArgumentException("Coupon không hợp lệ hoặc không thể sử dụng cho đơn hàng này.");

                    discount = await _unitOfWork.CouponRepository.CalculateDiscountAmountAsync(
                        request.CouponId.Value,
                        subtotal);
                }

                // 5.2. Shipping fee
                decimal shippingFee = await CalculateShippingFeeAsync(request.ShippingMethodId, subtotal);
                var totalAmount = subtotal + shippingFee - discount;

                // 6. Tạo Order ban đầu
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
                    DiscountAmount = discount,
                    TotalAmount = totalAmount,
                    PaymentStatus = PaymentStatus.Unpaid,
                    Status = OrderStatus.Pending,
                    CreatedAt = _currentTime.GetVietnamTime(),
                    CreatedBy = userId.Value
                };
                order.PaymentMethod = request.PaymentMethod;
                await _orderRepository.AddAsync(order);

                // 7. Lưu OrderItems
                foreach (var oi in orderItems)
                {
                    oi.OrderId = order.Id;
                    await _unitOfWork.OrderItemRepository.AddAsync(oi);
                }

                // 8. Ghi nhận sử dụng coupon
                if (request.CouponId.HasValue && validatedCoupon != null)
                {
                    var useCouponRes = await _couponService.UseCouponAsync(
                        request.CouponId.Value,
                        userId.Value);
                    if (!useCouponRes.IsSuccess)
                    {
                        _logger.LogError(
                            "Failed to use coupon {CouponId} for user {UserId}: {Error}",
                            request.CouponId.Value, userId.Value, useCouponRes.Message);
                        throw new InvalidOperationException($"Không thể sử dụng coupon: {useCouponRes.Message}");
                    }
                }

                // 9. Xóa cart items đã order
                if (cartItemIds.Any())
                {
                    await _cartItemRepository.DeleteRangeAsync(cartItemIds);
                }

                // **10. Lưu Order và OrderItems trước khi tạo payment**
                await _unitOfWork.SaveChangesAsync();

                // 11. TẠO PAYMENT, lấy URL nếu VNPAY
                var paymentReq = new PaymentCreateRequest
                {
                    OrderId = order.Id,
                    PaymentMethod = request.PaymentMethod,
                    Description = request.PaymentDescription
                };

                PaymentResponse payment;
                string? paymentUrl = null;

                if (request.PaymentMethod == PaymentMethod.VNPAY)
                {
                    // Gọi method VNPAY để lấy URL
                    var vnPayResp = await _paymentService.CreateVnPayPaymentAsync(paymentReq);
                    payment = vnPayResp.Payment!;    // vẫn dùng DTO cũ
                    paymentUrl = vnPayResp.PaymentUrl;  // đây là link để FE redirect
                }
                else
                {
                    // COD hoặc các phương thức khác
                    payment = await _paymentService.CreatePaymentAsync(paymentReq);
                }

                // 12. Commit transaction như cũ
                await transaction.CommitAsync();

                // 13. Trả về kết quả, thêm trường PaymentUrl
                var createdOrder = await _orderRepository.GetOrderWithDetailsAsync(order.Id);
                var orderDto = ConvertToOrderDTO(createdOrder!);

                return new CreateOrderResult
                {
                    Order = orderDto,
                    Payment = payment,
                    PaymentUrl = paymentUrl
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<OrderDTO?> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, Guid? updatedBy = null)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                    throw new ArgumentException("Đơn hàng không tồn tại");

                if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                    throw new InvalidOperationException("Không thể cập nhật đơn hàng đã giao hoặc đã hủy");

                var userId = updatedBy ?? _currentUserService.GetUserId();
                if (!userId.HasValue)
                    throw new UnauthorizedAccessException("Không thể xác định người dùng hiện tại");

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
                    var couponResult = await _couponService.GetByIdAsync(request.CouponId.Value);
                    if (!couponResult.IsSuccess || couponResult.Data == null)
                        throw new ArgumentException("Mã giảm giá không tồn tại");

                    var validationResult = await _couponService.ValidateCouponAsync(couponResult.Data.Code, order.TotalAmount, order.UserId);
                    if (!validationResult.IsSuccess || !validationResult.Data)
                        throw new ArgumentException("Mã giảm giá không hợp lệ hoặc không áp dụng được: " + (validationResult.Message ?? ""));

                    order.CouponId = request.CouponId;
                    var (discountAmount, _) = await CalculateDiscountAndTaxAsync(request.CouponId, order.TotalAmount);
                    order.DiscountAmount = discountAmount;
                }
                else if (request.CouponId == null && order.CouponId.HasValue)
                {
                    order.CouponId = null;
                    order.DiscountAmount = 0;
                }

                if (request.ShippingMethodId.HasValue)
                {
                    var shippingMethodValidation = await _shippingMethodService.ValidateShippingMethodAsync(request.ShippingMethodId.Value);
                    if (!shippingMethodValidation.IsSuccess)
                        throw new ArgumentException("Phương thức vận chuyển không hợp lệ: " + (shippingMethodValidation.Message ?? ""));

                    order.ShippingMethodId = request.ShippingMethodId;
                    order.ShippingFee = await CalculateShippingFeeAsync(request.ShippingMethodId, order.TotalAmount);
                }

                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = userId.Value;

                var updatedOrder = await UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
                return ConvertToOrderDTO(updatedOrder);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating order {OrderId}", orderId);
                throw;
            }
        }
        public async Task<BatchOperationResultDTO> BulkDeleteOrdersAsync(List<Guid> orderIds, Guid? deletedBy = null)
        {
            var result = new BatchOperationResultDTO
            {
                TotalRequested = orderIds.Count
            };

            if (!orderIds.Any())
            {
                result.Message = "Danh sách ID đơn hàng không được để trống.";
                return result;
            }

            // Sử dụng một transaction để đảm bảo tất cả các thao tác xóa thành công hoặc không có gì xảy ra.
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var orderId in orderIds)
                {
                    var order = await _orderRepository.GetByIdAsync(orderId);

                    // Kiểm tra các điều kiện xóa
                    if (order == null || order.IsDeleted)
                    {
                        result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = "Đơn hàng không tồn tại hoặc đã bị xóa." });
                        continue; // Bỏ qua và xử lý ID tiếp theo
                    }

                    if (order.Status != OrderStatus.Pending)
                    {
                        result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = "Chỉ có thể xóa đơn hàng đang chờ xử lý." });
                        continue; // Bỏ qua và xử lý ID tiếp theo
                    }

                    // Nếu mọi thứ đều ổn, thực hiện xóa
                    var deleteResult = await _orderRepository.DeleteAsync(orderId); // Giả sử đây là xóa vật lý hoặc soft delete
                    if (deleteResult)
                    {
                        result.SuccessIds.Add(orderId.ToString());
                    }
                    else
                    {
                        result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = "Xóa đơn hàng thất bại từ repository." });
                    }
                }

                result.SuccessCount = result.SuccessIds.Count;
                result.FailureCount = result.Errors.Count;

                // Chỉ commit transaction nếu có ít nhất một đơn hàng được xóa thành công
                if (result.SuccessCount > 0)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    result.Message = $"Thao tác hoàn tất. Đã xóa thành công {result.SuccessCount}/{result.TotalRequested} đơn hàng.";
                }
                else
                {
                    // Nếu không có gì để xóa hoặc tất cả đều lỗi, hãy rollback để không có thay đổi nào được lưu.
                    await transaction.RollbackAsync();
                    result.Message = "Không có đơn hàng nào được xóa do không thỏa mãn điều kiện.";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi nghiêm trọng xảy ra khi đang xóa hàng loạt đơn hàng. User: {DeletedBy}", deletedBy);
                // Cập nhật kết quả để phản ánh lỗi
                result.Message = "Đã có lỗi hệ thống xảy ra. Vui lòng thử lại.";
                result.Errors.Add(new BatchOperationErrorDTO { Id = "System", ErrorMessage = ex.Message });
                result.FailureCount = result.TotalRequested;
                result.SuccessCount = 0;
                result.SuccessIds.Clear();
            }

            return result;
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
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                    throw new ArgumentException("Đơn hàng không tồn tại");

                var result = await _orderRepository.UpdateOrderStatusAsync(orderId, status, updatedBy);
                if (result)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating order status {OrderId} to {Status}", orderId, status);
                throw;
            }
        }
        public async Task<bool> UpdatePaymentStatusAsync(Guid orderId, PaymentStatus paymentStatus, Guid? updatedBy = null)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                    throw new ArgumentException("Đơn hàng không tồn tại");

                var result = await _orderRepository.UpdatePaymentStatusAsync(orderId, paymentStatus, updatedBy);
                if (result)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating payment status {OrderId} to {PaymentStatus}", orderId, paymentStatus);
                throw;
            }
        }
        public async Task<bool> CancelOrderAsync(Guid orderId, string reason, Guid? cancelledBy = null)
        {
            // Bắt đầu một transaction để đảm bảo tất cả các thao tác (hủy đơn, hoàn kho, hoàn coupon)
            // hoặc thành công hết hoặc thất bại hết.
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentException("Lý do hủy đơn hàng là bắt buộc");
                }

                // Bước 1: Lấy thông tin chi tiết đơn hàng, bao gồm cả các sản phẩm trong đơn (OrderItems)
                // Lưu ý: Đảm bảo phương thức GetOrderWithDetailsAsync() của bạn có .Include(o => o.OrderItems)
                var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new ArgumentException("Đơn hàng không tồn tại");
                }

                // Kiểm tra xem đơn hàng có ở trạng thái cho phép hủy hay không
                if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed)
                {
                    throw new InvalidOperationException("Không thể hủy đơn hàng đã được giao hoặc đã bị hủy trước đó");
                }

                _logger.LogInformation("Bắt đầu quá trình hủy đơn hàng {OrderId} với lý do: {Reason}", orderId, reason);

                // Bước 2: Thay đổi trạng thái của đơn hàng trong bộ nhớ (chưa ghi vào DB)
                var result = await _orderRepository.CancelOrderAsync(orderId, reason.Trim(), cancelledBy);

                if (result)
                {
                    // Bước 3: Hoàn trả số lượng sản phẩm về kho
                    _logger.LogInformation("Bắt đầu hoàn kho cho đơn hàng {OrderId}", orderId);
                    foreach (var item in order.OrderItems)
                    {
                        // Chỉ hoàn kho cho các sản phẩm có ProductVariantId (được quản lý theo biến thể)
                        if (item.ProductVariantId.HasValue && item.ProductVariantId.Value != Guid.Empty)
                        {
                            var stockUpdated = await _productVariantRepository.IncreaseStockAsync(item.ProductVariantId.Value, item.Quantity);
                            if (!stockUpdated)
                            {
                                // Nếu không tìm thấy biến thể sản phẩm để hoàn kho, đây là lỗi nghiêm trọng.
                                // Hủy bỏ toàn bộ giao dịch ngay lập tức.
                                _logger.LogError("Không thể hoàn kho cho ProductVariantId {ProductVariantId}. Giao dịch sẽ được rollback.", item.ProductVariantId.Value);
                                await transaction.RollbackAsync();
                                return false;
                            }
                            _logger.LogInformation("Đã hoàn lại {Quantity} sản phẩm vào kho cho ProductVariantId: {ProductVariantId}", item.Quantity, item.ProductVariantId.Value);
                        }
                    }

                    // Bước 4: Xử lý hoàn lại coupon (nếu có)
                    if (order.CouponId.HasValue)
                    {
                        try
                        {
                            _logger.LogInformation("Đơn hàng {OrderId} đã sử dụng coupon {CouponId}, đang xử lý logic hoàn lại.", orderId, order.CouponId);

                            // TODO: Triển khai logic hoàn lại coupon ở đây.
                            // Ví dụ: await _couponService.RollbackCouponUsageAsync(order.CouponId.Value, order.UserId);
                        }
                        catch (Exception ex)
                        {
                            // Việc hoàn coupon thất bại không nên làm hỏng toàn bộ giao dịch hủy đơn hàng.
                            // Ghi lại cảnh báo và tiếp tục.
                            _logger.LogWarning(ex, "Không thể hoàn lại coupon {CouponId} cho đơn hàng {OrderId}. Quá trình hủy đơn vẫn tiếp tục.",
                                order.CouponId, orderId);
                        }
                    }

                    // Bước 5: Lưu tất cả thay đổi vào database
                    // Lệnh này sẽ ghi cả thay đổi trạng thái của Order và thay đổi Quantity của ProductVariant.
                    await _unitOfWork.SaveChangesAsync();

                    // Bước 6: Commit transaction để xác nhận tất cả các thay đổi thành công
                    await transaction.CommitAsync();

                    _logger.LogInformation("Đơn hàng {OrderId} đã được hủy và hoàn kho thành công.", orderId);
                }
                else
                {
                    // Nếu _orderRepository.CancelOrderAsync() trả về false, rollback transaction
                    _logger.LogWarning("Phương thức _orderRepository.CancelOrderAsync() trả về false cho đơn hàng {OrderId}. Giao dịch đã được rollback.", orderId);
                    await transaction.RollbackAsync();
                }

                return result;
            }
            catch (Exception ex)
            {

                await transaction.RollbackAsync();
                _logger.LogError(ex, "Đã xảy ra lỗi nghiêm trọng khi đang hủy đơn hàng {OrderId}. Giao dịch đã được rollback an toàn.", orderId);
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

                if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed)
                {
                    throw new InvalidOperationException("Không thể phân công đơn hàng đã hủy hoặc đã giao, hoặc đã hoàn thành");
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
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(trackingNumber))
                    throw new ArgumentException("Mã vận đơn không được để trống");

                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                    throw new ArgumentException("Đơn hàng không tồn tại");

                if (order.Status != OrderStatus.Shipping && order.Status != OrderStatus.Processing)
                    throw new InvalidOperationException("Chỉ có thể cập nhật mã vận đơn cho đơn hàng đã xác nhận hoặc đang giao");

                var result = await _orderRepository.UpdateTrackingNumberAsync(orderId, trackingNumber.Trim(), updatedBy);
                if (result)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating tracking number {OrderId}", orderId);
                throw;
            }
        }

        public async Task<BatchOperationResultDTO> BulkMarkOrdersAsShippingAsync(List<Guid> orderIds, Guid staffId)
        {
            var result = new BatchOperationResultDTO
            {
                TotalRequested = orderIds.Count,
                SuccessIds = new List<string>(),
                Errors = new List<BatchOperationErrorDTO>()
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var orderId in orderIds)
                {
                    try
                    {
                        var order = await _orderRepository.GetByIdAsync(orderId);
                        if (order == null || order.IsDeleted)
                        {
                            result.Errors.Add(new BatchOperationErrorDTO
                            {
                                Id = orderId.ToString(),
                                ErrorMessage = "Đơn hàng không tồn tại hoặc đã bị xóa"
                            });
                            continue;
                        }

                        if (order.Status != OrderStatus.Processing && order.Status != OrderStatus.Paid)
                        {
                            result.Errors.Add(new BatchOperationErrorDTO
                            {
                                Id = orderId.ToString(),
                                ErrorMessage = "Đơn hàng không ở trạng thái hợp lệ để chuyển sang Shipping (chỉ chấp nhận Paid hoặc Processing)"
                            });
                            continue;
                        }

                        var success = await _orderRepository.UpdateOrderStatusAsync(orderId, OrderStatus.Shipping, staffId);
                        if (success)
                            result.SuccessIds.Add(orderId.ToString());
                        else
                            result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = "Cập nhật trạng thái thất bại" });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi cập nhật Shipping cho đơn hàng {OrderId}", orderId);
                        result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = ex.Message });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi hệ thống khi cập nhật hàng loạt trạng thái Shipping");
                result.Errors.Add(new BatchOperationErrorDTO
                {
                    Id = "Hệ thống",
                    ErrorMessage = "Lỗi hệ thống: " + ex.Message
                });
            }

            result.SuccessCount = result.SuccessIds.Count;
            result.FailureCount = result.Errors.Count;
            result.Message = result.IsCompleteSuccess
                ? $"Thành công: đã cập nhật Shipping cho {result.SuccessCount} đơn hàng."
                : result.IsCompleteFailure
                    ? $"Không thể cập nhật bất kỳ đơn hàng nào."
                    : $"Một phần thành công: {result.SuccessCount} thành công, {result.FailureCount} thất bại.";

            return result;
        }
        public async Task<BatchOperationResultDTO> BulkConfirmDeliveredByUserAsync(List<Guid> orderIds, Guid userId)
        {
            var result = new BatchOperationResultDTO
            {
                TotalRequested = orderIds.Count,
                SuccessIds = new List<string>(),
                Errors = new List<BatchOperationErrorDTO>()
            };

            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var orderId in orderIds)
                {
                    try
                    {
                        var order = await _orderRepository.GetByIdAsync(orderId);

                        // Kiểm tra xem đơn hàng có hợp lệ không
                        if (order == null || order.IsDeleted || order.UserId != userId)
                        {
                            result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = "Đơn hàng không hợp lệ hoặc không thuộc về người dùng" });
                            continue;
                        }

                        // Chỉ cho phép xác nhận khi đơn hàng đang ở trạng thái "Đang giao"
                        if (order.Status != OrderStatus.Shipping)
                        {
                            result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = "Chỉ có thể xác nhận đơn hàng đang ở trạng thái 'Đang giao'" });
                            continue;
                        }

                        // === THAY ĐỔI CHÍNH Ở ĐÂY ===
                        // 1. Cập nhật trạng thái đơn hàng
                        order.Status = OrderStatus.Delivered;

                        // 2. Ghi lại thời điểm xác nhận đã nhận hàng
                        order.DeliveredAt = DateTime.UtcNow; // Sử dụng UtcNow để tránh lỗi múi giờ

                        // 3. Đánh dấu đối tượng order đã thay đổi (quan trọng!)
                        //    (Dòng này có thể không cần thiết nếu DbContext của bạn tự động theo dõi,
                        //     nhưng thêm vào sẽ đảm bảo entity được đánh dấu là Modified)
                        _orderRepository.UpdateAsync(order);

                        // Thêm vào danh sách thành công, việc lưu sẽ được thực hiện ở cuối
                        result.SuccessIds.Add(orderId.ToString());
                    }
                    catch (Exception ex)
                    {
                        // Ghi log lỗi cho từng đơn hàng cụ thể
                        _logger.LogError(ex, "Lỗi khi xác nhận 'Đã giao' cho đơn hàng {OrderId}", orderId);
                        result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = "Lỗi xử lý: " + ex.Message });
                    }
                }

                // Chỉ lưu các thay đổi vào DB nếu có ít nhất một đơn hàng thành công
                if (result.SuccessIds.Any())
                {
                    await _unitOfWork.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi hệ thống khi xác nhận 'Đã giao' hàng loạt");
                // Thêm một lỗi chung để thông báo cho người dùng về sự cố hệ thống
                result.Errors.Add(new BatchOperationErrorDTO
                {
                    Id = "Hệ thống",
                    ErrorMessage = "Lỗi hệ thống nghiêm trọng đã xảy ra. Vui lòng thử lại sau. " + ex.Message
                });
            }

            // Cập nhật lại số lượng thành công/thất bại và thông báo
            result.SuccessCount = result.SuccessIds.Count;
            result.FailureCount = result.Errors.Count;
            result.Message = result.IsCompleteSuccess
                ? $"Xác nhận đã nhận thành công cho {result.SuccessCount} đơn hàng."
                : result.IsCompleteFailure
                    ? $"Không thể xác nhận bất kỳ đơn hàng nào."
                    : $"Một phần thành công: {result.SuccessCount} thành công, {result.FailureCount} thất bại.";

            return result;
        }

        public async Task<BatchOperationResultDTO> BulkProcessOrdersAsync(List<Guid> orderIds, Guid staffId)
        {
            var result = new BatchOperationResultDTO
            {
                TotalRequested = orderIds.Count,
                SuccessIds = new List<string>(),
                Errors = new List<BatchOperationErrorDTO>()
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var orderId in orderIds)
                {
                    try
                    {
                        var order = await _orderRepository.GetByIdAsync(orderId);
                        if (order == null || order.IsDeleted)
                        {
                            result.Errors.Add(new BatchOperationErrorDTO
                            {
                                Id = orderId.ToString(),
                                ErrorMessage = "Đơn hàng không tồn tại hoặc đã bị xóa"
                            });
                            continue;
                        }

                        if (order.Status != OrderStatus.Pending)
                        {
                            result.Errors.Add(new BatchOperationErrorDTO
                            {
                                Id = orderId.ToString(),
                                ErrorMessage = "Chỉ đơn ở trạng thái Pending mới được chuyển sang Processing"
                            });
                            continue;
                        }

                        // Cập nhật trạng thái
                        var success = await _orderRepository.UpdateOrderStatusAsync(
                            orderId,
                            OrderStatus.Processing,
                            staffId
                        );

                        if (success)
                            result.SuccessIds.Add(orderId.ToString());
                        else
                            result.Errors.Add(new BatchOperationErrorDTO
                            {
                                Id = orderId.ToString(),
                                ErrorMessage = "Cập nhật trạng thái thất bại"
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi chuyển Processing cho đơn hàng {OrderId}", orderId);
                        result.Errors.Add(new BatchOperationErrorDTO
                        {
                            Id = orderId.ToString(),
                            ErrorMessage = ex.Message
                        });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi hệ thống khi batch chuyển Processing");
                result.Errors.Add(new BatchOperationErrorDTO
                {
                    Id = "Hệ thống",
                    ErrorMessage = "Lỗi hệ thống: " + ex.Message
                });
            }

            result.SuccessCount = result.SuccessIds.Count;
            result.FailureCount = result.Errors.Count;
            result.Message = result.IsCompleteSuccess
                ? $"Thành công: đã chuyển Processing cho {result.SuccessCount} đơn hàng."
                : result.IsCompleteFailure
                    ? $"Không thể chuyển đơn hàng nào."
                    : $"Một phần thành công: {result.SuccessCount} thành công, {result.FailureCount} thất bại.";

            return result;
        }

        public async Task<BatchOperationResultDTO> BulkCompleteCODOrdersAsync(List<Guid> orderIds, Guid staffId)
        {
            var result = new BatchOperationResultDTO
            {
                TotalRequested = orderIds.Count,
                SuccessIds = new List<string>(),
                Errors = new List<BatchOperationErrorDTO>()
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var orderId in orderIds)
                {
                    try
                    {
                        var order = await _orderRepository.GetByIdAsync(orderId);
                        if (order == null || order.IsDeleted)
                        {
                            result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = "Đơn hàng không hợp lệ" });
                            continue;
                        }

                        if (order.Status != OrderStatus.Delivered)
                        {
                            result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = "Đơn hàng chưa được xác nhận đã giao" });
                            continue;
                        }

                        var statusUpdated = await _orderRepository.UpdateOrderStatusAsync(orderId, OrderStatus.Completed, staffId);
                        var paymentUpdated = await _orderRepository.UpdatePaymentStatusAsync(orderId, PaymentStatus.Completed, staffId);

                        if (statusUpdated && paymentUpdated)
                            result.SuccessIds.Add(orderId.ToString());
                        else
                            result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = "Không thể cập nhật trạng thái hoặc thanh toán" });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi hoàn tất đơn COD {OrderId}", orderId);
                        result.Errors.Add(new BatchOperationErrorDTO { Id = orderId.ToString(), ErrorMessage = ex.Message });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi hệ thống khi hoàn tất đơn hàng COD hàng loạt");
                result.Errors.Add(new BatchOperationErrorDTO
                {
                    Id = "Hệ thống",
                    ErrorMessage = "Lỗi hệ thống: " + ex.Message
                });
            }

            result.SuccessCount = result.SuccessIds.Count;
            result.FailureCount = result.Errors.Count;
            result.Message = result.IsCompleteSuccess
                ? $"Đã hoàn tất thành công {result.SuccessCount} đơn COD."
                : result.IsCompleteFailure
                    ? $"Không thể hoàn tất bất kỳ đơn hàng nào."
                    : $"Một phần thành công: {result.SuccessCount} thành công, {result.FailureCount} thất bại.";

            return result;
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
                decimal taxAmount = 0;
                decimal discountAmount = 0;

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
            string? orderImage = order.OrderItems?.FirstOrDefault()?.ProductVariant?.ImageUrl;
            var mainPayment = order.Payments?.FirstOrDefault();

            return new OrderDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                SubtotalAmount = order.SubtotalAmount,
                TotalAmount = order.TotalAmount,
                ShippingFee = order.ShippingFee,
                DiscountAmount = order.DiscountAmount,
                // THAY ĐỔI TẠI ĐÂY
                RefundAmount = (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Returned)
                   ? order.TotalAmount
                   : 0,
                Status = order.Status,
                Image = orderImage,
                PaymentStatus = order.PaymentStatus,
                PaymentMethod = mainPayment?.PaymentMethod ?? default(PaymentMethod),
                CancellationRequestStatus = order.CancellationStatus,
                ShippingAddress = order.ShippingAddress,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                CustomerNotes = order.CustomerNotes,
                EstimatedDeliveryDate = order.EstimatedDeliveryDate,
                TrackingNumber = order.TrackingNumber,
                CancellationReason = order.CancellationReason,
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
                DeliveredAt = order.DeliveredAt ?? DateTime.MinValue,
                ImageUrls = string.IsNullOrEmpty(order.CancellationImageUrls)
? new List<string>()
: order.CancellationImageUrls.Split(';').ToList(),
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
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.TotalPrice,

                SelectedColor = orderItem.ProductVariant?.Color.ToString(),
                SelectedSize = orderItem.ProductVariant?.Size.ToString(),

                ProductName = orderItem.ProductVariant?.Product?.Name ?? "Không có tên",
                CustomDesignName = orderItem.CustomDesign?.DesignName,
                ImageUrl = orderItem.ProductVariant?.ImageUrl,
                VariantName = orderItem.ProductVariant?.VariantSku

            };
        }

        #endregion

        #region Enhanced Order Management Methods

        /// <summary>
        /// Tạo đơn hàng từ giỏ hàng với validation đầy đủ
        /// </summary>
        public async Task<OrderDTO?> CreateOrderFromCartAsync(CreateOrderFromCartRequest request, Guid userId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Validate cart first
                var validationResult = await ValidateCartForOrderAsync(userId, request.SessionId);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Cart validation failed for user {UserId}: {Errors}",
                        userId, string.Join("; ", validationResult.Errors));
                    return null;
                }

                // Get cart items
                var cartItemsResult = await _cartItemService.GetCartItemEntitiesForCheckoutAsync(userId, request.SessionId);
                if (!cartItemsResult.IsSuccess || !cartItemsResult.Data.Any())
                {
                    _logger.LogWarning("No cart items found for user {UserId}", userId);
                    return null;
                }

                var cartItems = cartItemsResult.Data.ToList();

                // Filter by selected items if specified
                if (request.SelectedCartItemIds?.Any() == true)
                {
                    cartItems = cartItems.Where(ci => request.SelectedCartItemIds.Contains(ci.Id)).ToList();
                }

                // Create order
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = await GenerateOrderNumberAsync(),
                    UserId = userId,
                    Status = OrderStatus.Pending,
                    PaymentStatus = PaymentStatus.Unpaid,
                    ShippingMethodId = request.ShippingMethodId,
                    CouponId = request.CouponId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    UpdatedBy = userId
                };

                // Calculate totals
                var subtotal = cartItems.Sum(ci => ci.TotalPrice);

                // Apply shipping
                var shippingFee = await CalculateShippingFeeAsync(request.ShippingMethodId, subtotal);

                // Apply discount and tax
                var (discountAmount, taxAmount) = await CalculateDiscountAndTaxAsync(request.CouponId, subtotal);

                order.ShippingFee = shippingFee;
                order.DiscountAmount = discountAmount;
                order.TotalAmount = subtotal + shippingFee - discountAmount;

                // Save order
                var createdOrder = await CreateAsync(order);

                // Create order items
                var orderItems = new List<OrderItem>();
                foreach (var cartItem in cartItems)
                {
                    var orderItem = OrderItemBusinessLogic.CreateOrderItemFromCartItem(cartItem, createdOrder);
                    var createdOrderItem = await _orderItemRepository.AddAsync(orderItem);
                    orderItems.Add(createdOrderItem);
                }

                // Reserve inventory
                await ReserveInventoryForOrderAsync(createdOrder.Id);

                // Clear cart items
                var cartItemIds = cartItems.Select(ci => ci.Id).ToList();
                await _cartItemService.ClearCartItemsAfterCheckoutAsync(cartItemIds, userId, request.SessionId);

                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderId} created successfully for user {UserId}",
                    createdOrder.Id, userId);

                // Convert to DTO
                var orderDto = ConvertToOrderDTO(createdOrder);
                orderDto.OrderItems = orderItems.Select(oi => OrderItemMapper.ToDto(oi)).ToList();

                return orderDto;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order from cart for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Validate giỏ hàng trước khi tạo đơn hàng
        /// </summary>
        public async Task<OrderValidationResult> ValidateCartForOrderAsync(Guid? userId, string? sessionId)
        {
            try
            {
                var result = new OrderValidationResult();

                // Get cart validation from cart service
                var cartValidation = await _cartItemService.ValidateCartForCheckoutDetailedAsync(userId, sessionId);

                if (!cartValidation.IsSuccess)
                {
                    result.Errors.Add(cartValidation.Message);
                    return result;
                }

                var cartData = cartValidation.Data;
                result.IsValid = cartData.IsValid;
                result.Errors = cartData.Errors;
                result.Warnings = cartData.Warnings;
                result.TotalItems = cartData.TotalItems;

                // Map cart items to order items validation
                result.Items = cartData.Items.Select(ci => new OrderItemValidationDto
                {
                    CartItemId = ci.CartItemId,
                    ProductName = ci.ProductName,
                    VariantInfo = ci.VariantInfo,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.CurrentPrice,
                    TotalPrice = ci.CurrentPrice * ci.Quantity,
                    IsAvailable = ci.IsAvailable,
                    HasStockIssue = ci.HasStockIssue,
                    HasPriceChange = ci.HasPriceChange,
                    ErrorMessage = ci.ErrorMessage
                }).ToList();

                // Calculate estimated totals
                var subtotal = result.Items.Where(i => i.IsAvailable).Sum(i => i.TotalPrice);
                result.ShippingFee = await CalculateShippingFeeAsync(null, subtotal);
                var (discountAmount, taxAmount) = await CalculateDiscountAndTaxAsync(null, subtotal);
                result.DiscountAmount = discountAmount;
                result.EstimatedTotal = subtotal + result.ShippingFee - result.DiscountAmount;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cart for order");
                return new OrderValidationResult
                {
                    IsValid = false,
                    Errors = { "Lỗi khi validate giỏ hàng" }
                };
            }
        }

        /// <summary>
        /// Reserve inventory cho đơn hàng
        /// </summary>
        public async Task<bool> ReserveInventoryForOrderAsync(Guid orderId)
        {
            try
            {
                // TODO: Implement inventory reservation
                // This would require proper product repository integration
                _logger.LogInformation("Inventory reservation for order {OrderId} (placeholder implementation)", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving inventory for order {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Release inventory khi hủy đơn hàng
        /// </summary>
        public async Task<bool> ReleaseInventoryForOrderAsync(Guid orderId)
        {
            try
            {
                // TODO: Implement inventory release
                // This would require proper product repository integration
                _logger.LogInformation("Inventory release for order {OrderId} (placeholder implementation)", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing inventory for order {OrderId}", orderId);
                return false;
            }
        }

        /// <summary>
        /// Tính toán lại tổng tiền đơn hàng
        /// </summary>
        public async Task<decimal> RecalculateOrderTotalAsync(Guid orderId)
        {
            try
            {
                var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null) return 0;

                var subtotal = order.OrderItems.Sum(oi => oi.TotalPrice);
                var total = subtotal + order.ShippingFee - order.DiscountAmount;

                if (Math.Abs(order.TotalAmount - total) > 0.01m)
                {
                    order.TotalAmount = total;
                    order.UpdatedAt = DateTime.UtcNow;
                    await UpdateAsync(order);

                    _logger.LogInformation("Order total recalculated for {OrderId}: {Total}", orderId, total);
                }

                return total;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating order total for {OrderId}", orderId);
                return 0;
            }
        }

        /// <summary>
        /// Lấy order analytics nâng cao
        /// </summary>
        public async Task<OrderAnalyticsDto> GetOrderAnalyticsAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var orders = await GetOrdersForAnalyticsAsync(fromDate, toDate);
                var ordersList = orders.ToList();

                var analytics = new OrderAnalyticsDto
                {
                    TotalOrders = ordersList.Count,
                    TotalRevenue = ordersList.Sum(o => o.TotalAmount),
                    TotalItems = ordersList.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity)
                };

                if (analytics.TotalOrders > 0)
                {
                    analytics.AverageOrderValue = analytics.TotalRevenue / analytics.TotalOrders;
                }

                // Orders by status
                analytics.OrdersByStatus = ordersList
                    .GroupBy(o => o.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                // Revenue by status
                analytics.RevenueByStatus = ordersList
                    .GroupBy(o => o.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));

                // Top products (simplified - would need proper product data)
                analytics.TopProducts = ordersList
                    .SelectMany(o => o.OrderItems)
                    .Where(oi => oi.ProductId.HasValue)
                    .GroupBy(oi => new { oi.ProductId, oi.ItemName })
                    .Select(g => new TopProductDto
                    {
                        ProductId = g.Key.ProductId!.Value,
                        ProductName = g.Key.ItemName,
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.TotalPrice)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(10)
                    .ToList();

                // Daily stats
                analytics.DailyStats = ordersList
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new DailyOrderStatsDto
                    {
                        Date = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount),
                        ItemsSold = g.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // Calculate trends (simplified)
                analytics.Trends = CalculateOrderTrends(analytics.DailyStats);

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order analytics");
                return new OrderAnalyticsDto();
            }
        }

        /// <summary>
        /// Bulk cancel orders
        /// </summary>
        public async Task<BatchOperationResultDTO> BulkCancelOrdersAsync(List<Guid> orderIds, string reason, Guid? cancelledBy = null)
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
                return result;
            }

            foreach (var orderId in orderIds)
            {
                try
                {
                    var success = await CancelOrderAsync(orderId, reason, cancelledBy);
                    if (success)
                    {
                        result.SuccessIds.Add(orderId.ToString());
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.Errors.Add(new BatchOperationErrorDTO
                        {
                            Id = orderId.ToString(),
                            ErrorMessage = "Không thể hủy đơn hàng"
                        });
                        result.FailureCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new BatchOperationErrorDTO
                    {
                        Id = orderId.ToString(),
                        ErrorMessage = $"Lỗi khi hủy đơn hàng: {ex.Message}"
                    });
                    result.FailureCount++;
                    _logger.LogError(ex, "Error cancelling order {OrderId} in bulk operation", orderId);
                }
            }

            return result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Tính toán xu hướng đơn hàng
        /// </summary>
        private OrderTrendsDto CalculateOrderTrends(List<DailyOrderStatsDto> dailyStats)
        {
            var trends = new OrderTrendsDto();

            if (dailyStats.Count < 2)
            {
                return trends;
            }

            var firstHalf = dailyStats.Take(dailyStats.Count / 2).ToList();
            var secondHalf = dailyStats.Skip(dailyStats.Count / 2).ToList();

            var firstHalfRevenue = firstHalf.Sum(d => d.Revenue);
            var secondHalfRevenue = secondHalf.Sum(d => d.Revenue);

            if (firstHalfRevenue > 0)
            {
                trends.RevenueGrowth = Math.Round(((secondHalfRevenue - firstHalfRevenue) / firstHalfRevenue) * 100, 2);
            }

            var firstHalfOrders = firstHalf.Sum(d => d.OrderCount);
            var secondHalfOrders = secondHalf.Sum(d => d.OrderCount);

            if (firstHalfOrders > 0)
            {
                trends.OrderCountGrowth = Math.Round(((secondHalfOrders - firstHalfOrders) / (decimal)firstHalfOrders) * 100, 2);
            }

            // Determine trend direction
            if (trends.RevenueGrowth > 5)
                trends.TrendDirection = "Growing";
            else if (trends.RevenueGrowth < -5)
                trends.TrendDirection = "Declining";
            else
                trends.TrendDirection = "Stable";

            return trends;
        }

        #endregion
        public async Task<DashboardAnalyticsDto?> GetDashboardAnalyticsAsync()
        {
            try
            {
                var now = _currentTime.GetVietnamTime();
                var todayStart = now.Date;
                var todayEnd = todayStart.AddDays(1).AddTicks(-1);

                var startOfWeek = todayStart.AddDays(-(int)todayStart.DayOfWeek + (int)DayOfWeek.Monday);
                if (todayStart.DayOfWeek == DayOfWeek.Sunday)
                {
                    startOfWeek = startOfWeek.AddDays(-7);
                }

                var totalRevenueTask = _orderRepository.GetTotalRevenueFromCompletedOrdersAsync();
                var ordersTodayTask = _orderRepository.GetOrderCountAsync(todayStart, todayEnd);
                var ordersThisWeekTask = _orderRepository.GetOrderCountAsync(startOfWeek, todayEnd);
                var paymentCountsTask = _orderRepository.GetPaymentStatusCountsAsync();

                await Task.WhenAll(totalRevenueTask, ordersTodayTask, ordersThisWeekTask, paymentCountsTask);

                var paymentCounts = paymentCountsTask.Result;
                var paidCount = paymentCounts.GetValueOrDefault(PaymentStatus.Completed, 0);
                var unpaidCount = paymentCounts.GetValueOrDefault(PaymentStatus.Unpaid, 0);
                var refundedCount = paymentCounts.GetValueOrDefault(PaymentStatus.Refunded, 0);
                var totalPaymentOrders = paidCount + unpaidCount + refundedCount;

                var analyticsDto = new DashboardAnalyticsDto
                {
                    TotalRevenue = totalRevenueTask.Result,
                    OrdersToday = ordersTodayTask.Result,
                    OrdersThisWeek = ordersThisWeekTask.Result,
                    PaymentStatusRatio = new PaymentStatusRatioDto
                    {
                        PaidCount = paidCount,
                        UnpaidCount = unpaidCount,
                        RefundedCount = refundedCount,
                        TotalCount = totalPaymentOrders,
                        PaidPercentage = totalPaymentOrders > 0
                            ? Math.Round((double)paidCount / totalPaymentOrders * 100, 2)
                            : 0
                    }
                };

                return analyticsDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu analytics cho dashboard");
                return null; // Trả về null khi có lỗi, giống với mẫu thiết kế của bạn
            }
        }
        public async Task<BatchOperationResultDTO> PurgeCompletedOrdersAsync(int daysOld)
        {
            var result = new BatchOperationResultDTO
            {
                SuccessIds = new List<string>(),
                Errors = new List<BatchOperationErrorDTO>()
            };

            if (daysOld <= 0)
            {
                result.Message = "Số ngày phải lớn hơn 0.";
                return result;
            }

            var thresholdDate = DateTime.UtcNow.AddDays(-daysOld);

            // Use GetQueryable() to filter orders
            var ordersToPurge = _orderRepository.GetQueryable()
                        .Where(o => o.Status == OrderStatus.Completed &&
                                    o.UpdatedAt < thresholdDate);

            var ordersList = await ordersToPurge.ToListAsync();
            result.TotalRequested = ordersList.Count;

            if (!ordersList.Any())
            {
                result.Message = "Không tìm thấy đơn hàng nào phù hợp để xóa.";
                return result;
            }

            _logger.LogInformation("Found {Count} completed orders older than {Date} to purge.", result.TotalRequested, thresholdDate.Date);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var order in ordersList)
                {
                    var deleteResult = await _orderRepository.DeleteAsync(order.Id);
                    if (deleteResult)
                    {
                        result.SuccessIds.Add(order.Id.ToString());
                    }
                    else
                    {
                        result.Errors.Add(new BatchOperationErrorDTO
                        {
                            Id = order.Id.ToString(),
                            ErrorMessage = "Xóa đơn hàng thất bại"
                        });
                    }
                }

                result.SuccessCount = result.SuccessIds.Count;
                result.FailureCount = result.Errors.Count;

                if (result.SuccessCount > 0)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    result.Message = $"Đã xóa thành công {result.SuccessCount} đơn hàng.";
                    _logger.LogInformation(result.Message);
                }
                else
                {
                    await transaction.RollbackAsync();
                    result.Message = $"Thao tác bị hủy bỏ. Không xóa được đơn hàng nào.";
                    _logger.LogWarning(result.Message);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi nghiêm trọng xảy ra khi đang xóa hàng loạt các đơn hàng cũ.");
                result.Message = "Đã có lỗi xảy ra trong quá trình xử lý.";
            }

            return result;
        }



        // Refactor logic hủy đơn hàng thực sự (hoàn kho, coupon) vào một phương thức private
        private async Task<bool> _executeOrderCancellationLogic(Order order, string reason, Guid? processedBy)
        {
            // Transaction đã được xử lý ở tầng gọi (RequestOrderCancellationAsync hoặc ProcessCancellationRequestAsync)
            // Vì vậy không cần transaction ở đây nữa nếu nó được gọi trong một transaction lớn hơn.
            // Tuy nhiên, nếu bạn muốn mỗi hành động nhỏ có transaction riêng, bạn có thể để lại.
            // Để đơn giản và tránh nested transaction, chúng ta sẽ giả định transaction bên ngoài đủ.

            _logger.LogInformation("Thực thi logic hủy đơn hàng {OrderId} bởi {ProcessedBy}", order.Id, processedBy);

            // Cập nhật trạng thái đơn hàng và các trường liên quan đến hủy
            // Lưu ý: Các trường này đã được set bởi phương thức gọi (RequestOrderCancellationAsync hoặc ProcessCancellationRequestAsync)
            // nhưng cần đảm bảo trạng thái cuối cùng là Cancelled.
            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = reason;
            order.CancellationStatus = CancellationRequestStatus.Approved; // Đơn đã thực sự bị hủy

            order.UpdatedAt = DateTime.UtcNow;
            if (processedBy.HasValue)
                order.UpdatedBy = processedBy.Value;

            if (order.PaymentStatus == PaymentStatus.Completed)
                order.PaymentStatus = PaymentStatus.Refunded; // Có thể cần logic hoàn tiền thực tế ở đây

            // Lưu ý: _orderRepository.Update(order) có thể không cần thiết nếu order được truy vấn trong cùng context
            // và đã được EF theo dõi thay đổi. SaveChangesAsync sẽ phát hiện.
            // Tuy nhiên, để rõ ràng, có thể thêm:
            // _orderRepository.Update(order); 


            _logger.LogInformation("Bắt đầu hoàn kho cho đơn hàng {OrderId}", order.Id);
            foreach (var item in order.OrderItems)
            {
                // Chỉ hoàn kho cho các sản phẩm có ProductVariantId (được quản lý theo biến thể)
                if (item.ProductVariantId.HasValue && item.ProductVariantId.Value != Guid.Empty)
                {
                    var stockUpdated = await _productVariantRepository.IncreaseStockAsync(item.ProductVariantId.Value, item.Quantity);
                    if (!stockUpdated)
                    {
                        _logger.LogError("Không thể hoàn kho cho ProductVariantId {ProductVariantId}. Giao dịch sẽ được rollback.", item.ProductVariantId.Value);
                        // Ném exception để transaction cha bắt và rollback
                        throw new InvalidOperationException($"Không thể hoàn kho cho sản phẩm biến thể ID: {item.ProductVariantId.Value}");
                    }
                    _logger.LogInformation("Đã hoàn lại {Quantity} sản phẩm vào kho cho ProductVariantId: {ProductVariantId}", item.Quantity, item.ProductVariantId.Value);
                }
            }

            // Xử lý hoàn lại coupon (nếu có)
            if (order.CouponId.HasValue)
            {
                try
                {
                    _logger.LogInformation("Đơn hàng {OrderId} đã sử dụng coupon {CouponId}, đang xử lý logic hoàn lại.", order.Id, order.CouponId);
                    // TODO: Triển khai logic hoàn lại coupon ở đây.
                    // Ví dụ: await _couponService.RollbackCouponUsageAsync(order.CouponId.Value, order.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể hoàn lại coupon {CouponId} cho đơn hàng {OrderId}. Quá trình hủy đơn vẫn tiếp tục.",
                        order.CouponId, order.Id);
                }
            }

            return true; // Logic đã được thực thi
        }


        /// <inheritdoc />
        public async Task<bool> RequestOrderCancellationAsync(Guid orderId, RequestCancellationRequest request, Guid? userId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    throw new ArgumentException("Lý do hủy đơn hàng là bắt buộc.");
                }

                // Lấy thông tin chi tiết đơn hàng (cần OrderItems để hoàn kho)
                var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new ArgumentException("Đơn hàng không tồn tại.");
                }

                // Kiểm tra quyền truy cập: Người dùng phải là chủ đơn hàng HOẶC Admin/Staff
                if (userId.HasValue && order.UserId != userId.Value && !(_currentUserService.IsAdmin() || _currentUserService.IsStaff()))
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền yêu cầu hủy đơn hàng này.");
                }

                // Không cho phép yêu cầu hủy nếu đã ở trạng thái hủy cuối cùng hoặc đã có yêu cầu xử lý
                if (order.Status == OrderStatus.Cancelled || order.CancellationStatus == CancellationRequestStatus.Approved || order.CancellationStatus == CancellationRequestStatus.Rejected)
                {
                    throw new InvalidOperationException("Đơn hàng đã bị hủy hoặc yêu cầu hủy đã được xử lý trước đó.");
                }

                if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Processing)
                {
                    // Hủy trực tiếp đối với đơn hàng Pending hoặc Processing
                    _logger.LogInformation("Đơn hàng {OrderId} ở trạng thái Pending/Processing. Tiến hành hủy trực tiếp.", orderId);

                    // Thực thi logic hủy (hoàn kho, coupon)
                    var success = await _executeOrderCancellationLogic(order, request.Reason, userId);

                    if (success)
                    {
                        await _unitOfWork.SaveChangesAsync(); // Lưu các thay đổi từ _executeOrderCancellationLogic
                        await transaction.CommitAsync();
                        _logger.LogInformation("Đơn hàng {OrderId} đã được hủy trực tiếp thành công.", orderId);
                        return true;
                    }
                    else
                    {
                        await transaction.RollbackAsync(); // Nếu _executeOrderCancellationLogic thất bại (ví dụ: không hoàn kho được)
                        return false;
                    }
                }
                else if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed)
                {
                    // Tạo yêu cầu hủy, cần Admin/Staff duyệt
                    if (order.CancellationStatus == CancellationRequestStatus.Pending)
                    {
                        throw new InvalidOperationException("Đơn hàng đã có yêu cầu hủy đang chờ duyệt.");
                    }

                    _logger.LogInformation("Đơn hàng {OrderId} ở trạng thái Delivered/Completed. Ghi nhận yêu cầu hủy và chờ duyệt.", orderId);
                    order.CancellationStatus = CancellationRequestStatus.Pending; // Đặt trạng thái yêu cầu là chờ duyệt
                    order.CancellationReason = request.Reason.Trim();
                    order.CancellationRequestedAt = DateTime.UtcNow;
                    order.CancellationImageUrls = request.ImageUrls != null && request.ImageUrls.Any() ? string.Join(";", request.ImageUrls) : null;
                    order.UpdatedAt = DateTime.UtcNow;
                    order.UpdatedBy = userId; // Người dùng/admin đã gửi yêu cầu

                    // Chuyển trạng thái đơn hàng sang "CancellationRequested"
                    order.Status = OrderStatus.CancellationRequested;

                    _orderRepository.UpdateAsync(order); // Mark as modified
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogInformation("Yêu cầu hủy đơn hàng {OrderId} đã được ghi nhận và chờ duyệt.", orderId);
                    return true;
                }
                else
                {
                    // Các trạng thái khác không cho phép hủy qua luồng này
                    throw new InvalidOperationException($"Không thể yêu cầu hủy đơn hàng ở trạng thái '{order.Status}'.");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xử lý yêu cầu hủy đơn hàng {OrderId}.", orderId);
                throw; // Ném lại để controller xử lý ngoại lệ
            }
        }

        /// <inheritdoc />
        public async Task<bool> ProcessCancellationRequestAsync(Guid orderId, ProcessCancellationRequest request, Guid staffId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new ArgumentException("Đơn hàng không tồn tại.");
                }

                // Chỉ xử lý các yêu cầu đang chờ duyệt và đơn hàng phải ở trạng thái "CancellationRequested"
                if (order.CancellationStatus != CancellationRequestStatus.Pending || order.Status != OrderStatus.CancellationRequested)
                {
                    throw new InvalidOperationException("Yêu cầu hủy đơn hàng không ở trạng thái chờ duyệt hoặc đơn hàng không ở trạng thái yêu cầu hủy.");
                }

                order.ReviewNotes = request.AdminNotes?.Trim(); // Ghi chú của Admin/Staff
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = staffId; // Admin/Staff xử lý

                if (request.Status == CancellationRequestStatus.Approved)
                {
                    _logger.LogInformation("Đơn hàng {OrderId} được Admin/Staff {StaffId} duyệt hủy.", orderId, staffId);

                    // Thực thi logic hủy thực sự (hoàn kho, coupon)
                    var success = await _executeOrderCancellationLogic(order, order.CancellationReason ?? "Admin/Staff Approved", staffId);

                    if (success)
                    {
                        if (order.PaymentStatus == PaymentStatus.Paid)
                        {
                            order.PaymentStatus = PaymentStatus.Refunded;
                            _logger.LogInformation("Trạng thái thanh toán của đơn hàng {OrderId} đã được cập nhật thành 'Refunded'.", orderId);
                        }
                        await _unitOfWork.SaveChangesAsync(); // Lưu các thay đổi từ _executeOrderCancellationLogic
                        await transaction.CommitAsync();
                        _logger.LogInformation("Đơn hàng {OrderId} đã được duyệt và hủy thành công.", orderId);
                        return true;
                    }
                    else
                    {
                        await transaction.RollbackAsync(); // Nếu _executeOrderCancellationLogic thất bại
                        return false;
                    }

                }
                else if (request.Status == CancellationRequestStatus.Rejected)
                {
                    _logger.LogInformation("Đơn hàng {OrderId} được Admin/Staff {StaffId} từ chối hủy.", orderId, staffId);
                    order.CancellationStatus = CancellationRequestStatus.Rejected; // Đặt trạng thái yêu cầu là từ chối

                    // Chuyển trạng thái đơn hàng về trạng thái trước đó (ví dụ: Completed hoặc Delivered)
                    // Đây là một điểm quyết định nghiệp vụ. Nếu bạn không lưu trạng thái trước đó,
                    // bạn cần chọn một trạng thái hợp lý. "Completed" là một lựa chọn phổ biến.
                    order.Status = OrderStatus.Completed; // Hoặc OrderStatus.Delivered tùy thuộc vào logic nghiệp vụ của bạn.
                                                          // Hoặc nếu bạn có một trường PreviousStatus trong Order model, hãy dùng nó.

                    _orderRepository.UpdateAsync(order); // Mark as modified
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogInformation("Yêu cầu hủy đơn hàng {OrderId} đã bị từ chối.", orderId);
                    return true;
                }
                else
                {
                    throw new ArgumentException("Trạng thái xử lý yêu cầu hủy không hợp lệ.");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xử lý yêu cầu hủy đơn hàng {OrderId}.", orderId);
                throw; // Ném lại để controller xử lý ngoại lệ
            }
        }
        public async Task<PagedList<CancelledOrderDto>> GetCancelledOrdersAsync(PaginationParams paginationParams)
        {
            Guid? userId = null;

            // Logic nghiệp vụ: Nếu người dùng không phải Admin/Staff, họ chỉ có thể xem đơn hàng của mình.
            if (!_currentUserService.IsAdmin() && !_currentUserService.IsStaff())
            {
                userId = _currentUserService.GetUserId();
                if (!userId.HasValue)
                {
                    throw new UnauthorizedAccessException("Người dùng chưa đăng nhập.");
                }
            }

            _logger.LogInformation("Bắt đầu lấy danh sách đơn hàng đã hủy. UserID: {UserId}", userId ?? Guid.Empty);

            // 1. Gọi phương thức Repository để lấy PagedList<Order>
            var cancelledOrders = await _unitOfWork.OrderRepository.GetCancelledOrdersAsync(paginationParams, userId);

            // 2. Chuyển đổi (Map) thủ công từ List<Order> sang List<CancelledOrderDto>
            // Đây là phần thay thế cho AutoMapper
            var cancelledOrdersDtoList = cancelledOrders
    .Select(order => new CancelledOrderDto
    {
        OrderId = order.Id,
        OrderNumber = order.OrderNumber,
        ReceiverName = order.ReceiverName,
        ReceiverPhone = order.ReceiverPhone,
        Email = order.User.Email,
        ShippingAddress = order.ShippingAddress,
        SubtotalAmount = order.SubtotalAmount,
        ShippingFee = order.ShippingFee,
        DiscountAmount = order.DiscountAmount,
        TotalAmount = order.TotalAmount,
        CancellationReason = order.CancellationReason,
        DateCancelled = order.UpdatedAt,
        AdminReviewNotes = order.ReviewNotes,
        CancellationRequestStatus = order.CancellationStatus,
        PaymentStatus = order.PaymentStatus,
        ImageUrls = string.IsNullOrEmpty(order.CancellationImageUrls)
            ? new List<string>()
            : order.CancellationImageUrls.Split(';').ToList(),
        Items = order.OrderItems.Select(oi => new CancelledOrderItemDto
        {
            // Sử dụng toán tử ?. để tránh lỗi nếu ProductVariant hoặc Product bị null
            ProductName = oi.ProductVariant?.Product?.Name ?? "Sản phẩm không xác định",
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,

            VariantName = (oi.ProductVariant != null)
                            ? $"{oi.ProductVariant.Color} - {oi.ProductVariant.Size}"
                            : "Không có biến thể",
            // Lấy URL hình ảnh từ ProductVariant
            VariantImageUrl = oi.ProductVariant?.ImageUrl
        }).ToList()
    }).ToList();

            // 3. Tạo một PagedList<CancelledOrderDto> mới
            // Truyền vào danh sách DTO đã map và các thông tin phân trang từ danh sách gốc.
            var pagedResult = new PagedList<CancelledOrderDto>(
                cancelledOrdersDtoList,
                cancelledOrders.TotalCount,
                cancelledOrders.CurrentPage,
                cancelledOrders.PageSize
            );

            _logger.LogInformation("Đã lấy thành công {Count} đơn hàng đã hủy.", pagedResult.Count);

            return pagedResult;
        }
    }

}