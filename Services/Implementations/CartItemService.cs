using BusinessObjects.Cart;
using DTOs.Cart;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Helpers;
using Services.Helpers.Mappers;
using Services.Interfaces;

namespace Services.Implementations
{
    public class CartItemService : BaseService<CartItem, Guid>, ICartItemService
    {
        private readonly ICartItemRepository _cartItemRepository;

        public CartItemService(
            ICartItemRepository repository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime)
            : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _cartItemRepository = repository;
        }

        public async Task<ApiResult<CartItemDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var cartItem = await _cartItemRepository.GetWithDetailsAsync(id);
                if (cartItem == null)
                    return ApiResult<CartItemDto>.Failure("Không tìm thấy sản phẩm trong giỏ hàng");

                var cartItemDto = CartItemMapper.ToDto(cartItem);
                return ApiResult<CartItemDto>.Success(cartItemDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CartItemDto>.Failure("Lỗi khi lấy thông tin sản phẩm trong giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<PagedList<CartItemDto>>> GetCartItemsAsync(CartItemQueryDto query)
        {
            try
            {
                var pagedCartItems = await _cartItemRepository.GetCartItemsAsync(query);
                var cartItemDtos = CartItemMapper.ToPagedDto(pagedCartItems);
                return ApiResult<PagedList<CartItemDto>>.Success(cartItemDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<PagedList<CartItemDto>>.Failure("Lỗi khi lấy danh sách sản phẩm trong giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<IEnumerable<CartItemDto>>> GetUserCartItemsAsync(Guid userId)
        {
            try
            {
                var cartItems = await _cartItemRepository.GetUserCartItemsAsync(userId);
                var cartItemDtos = CartItemMapper.ToDtoList(cartItems);
                return ApiResult<IEnumerable<CartItemDto>>.Success(cartItemDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<IEnumerable<CartItemDto>>.Failure("Lỗi khi lấy giỏ hàng của người dùng", ex);
            }
        }

        public async Task<ApiResult<IEnumerable<CartItemDto>>> GetSessionCartItemsAsync(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                    return ApiResult<IEnumerable<CartItemDto>>.Failure("Session ID không được để trống");

                var cartItems = await _cartItemRepository.GetSessionCartItemsAsync(sessionId);
                var cartItemDtos = CartItemMapper.ToDtoList(cartItems);
                return ApiResult<IEnumerable<CartItemDto>>.Success(cartItemDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<IEnumerable<CartItemDto>>.Failure("Lỗi khi lấy giỏ hàng guest", ex);
            }
        }

        public async Task<ApiResult<CartSummaryDto>> GetCartSummaryAsync(Guid? userId, string? sessionId)
        {
            try
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<CartSummaryDto>.Failure("Phải có userId hoặc sessionId");

                IEnumerable<CartItem> cartItems;

                if (userId.HasValue)
                {
                    cartItems = await _cartItemRepository.GetUserCartItemsAsync(userId.Value);
                }
                else
                {
                    cartItems = await _cartItemRepository.GetSessionCartItemsAsync(sessionId!);
                }

                // Calculate estimated shipping and tax
                var subtotal = cartItems.Sum(ci => ci.TotalPrice);
                var estimatedShipping = CalculateEstimatedShipping(subtotal);
                var estimatedTax = CalculateEstimatedTax(subtotal);

                var cartSummary = CartItemMapper.ToCartSummaryDto(cartItems, estimatedShipping, estimatedTax);
                return ApiResult<CartSummaryDto>.Success(cartSummary);
            }
            catch (Exception ex)
            {
                return ApiResult<CartSummaryDto>.Failure("Lỗi khi lấy tổng quan giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<CartItemDto>> AddToCartAsync(InternalCreateCartItemDto createDto)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Validate business rules
                var validationResult = await ValidateCreateCartItemAsync(createDto);
                if (!validationResult.IsSuccess)
                    return validationResult;

                // Check if item already exists in cart
                var existingItem = await _cartItemRepository.FindExistingCartItemAsync(
                    createDto.UserId, createDto.SessionId, createDto.ProductId,
                    createDto.CustomDesignId, createDto.ProductVariantId);

                CartItem cartItem;
                if (existingItem != null)
                {
                    // Update existing item quantity
                    existingItem.Quantity += createDto.Quantity;
                    existingItem.UnitPrice = createDto.UnitPrice;
                    existingItem.UpdatedAt = _currentTime.GetVietnamTime();

                    cartItem = await UpdateAsync(existingItem);
                }
                else
                {
                    // Create new cart item
                    cartItem = CartItemMapper.ToEntity(createDto); // Update mapper to use InternalCreateCartItemDto
                    cartItem = await CreateAsync(cartItem);
                }

                await transaction.CommitAsync();

                var cartItemDto = CartItemMapper.ToDto(cartItem);
                return ApiResult<CartItemDto>.Success(cartItemDto, "Thêm vào giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResult<CartItemDto>.Failure("Lỗi khi thêm vào giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<CartItemDto>> UpdateCartItemAsync(Guid id, UpdateCartItemDto updateDto)
        {
            try
            {
                var existingCartItem = await _cartItemRepository.GetByIdAsync(id);
                if (existingCartItem == null)
                    return ApiResult<CartItemDto>.Failure("Không tìm thấy sản phẩm trong giỏ hàng");

                // Validate business rules
                var validationResult = ValidateUpdateCartItem(updateDto);
                if (!validationResult.IsSuccess)
                    return validationResult;

                // Update cart item properties
                CartItemMapper.UpdateEntity(existingCartItem, updateDto);

                var updatedCartItem = await UpdateAsync(existingCartItem);
                var cartItemDto = CartItemMapper.ToDto(updatedCartItem);

                return ApiResult<CartItemDto>.Success(cartItemDto, "Cập nhật giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<CartItemDto>.Failure("Lỗi khi cập nhật giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<bool>> RemoveFromCartAsync(Guid id)
        {
            try
            {
                var cartItem = await _cartItemRepository.GetByIdAsync(id);
                if (cartItem == null)
                    return ApiResult<bool>.Failure("Không tìm thấy sản phẩm trong giỏ hàng");

                var result = await DeleteAsync(id);
                return ApiResult<bool>.Success(result, "Xóa khỏi giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure("Lỗi khi xóa khỏi giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<bool>> ClearCartAsync(Guid? userId, string? sessionId)
        {
            try
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<bool>.Failure("Phải có userId hoặc sessionId");

                bool result;
                if (userId.HasValue)
                {
                    result = await _cartItemRepository.ClearUserCartAsync(userId.Value);
                }
                else
                {
                    result = await _cartItemRepository.ClearSessionCartAsync(sessionId!);
                }

                if (result)
                    await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(result, "Xóa giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure("Lỗi khi xóa giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<bool>> MergeGuestCartToUserAsync(string sessionId, Guid userId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                    return ApiResult<bool>.Failure("Session ID không được để trống");

                var result = await _cartItemRepository.MergeGuestCartToUserAsync(sessionId, userId);

                if (result)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                }

                return ApiResult<bool>.Success(result, "Merge giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResult<bool>.Failure("Lỗi khi merge giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<int>> GetCartItemCountAsync(Guid? userId, string? sessionId)
        {
            try
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<int>.Failure("Phải có userId hoặc sessionId");

                var count = await _cartItemRepository.GetCartItemCountAsync(userId, sessionId);
                return ApiResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                return ApiResult<int>.Failure("Lỗi khi đếm sản phẩm trong giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<decimal>> GetCartTotalAsync(Guid? userId, string? sessionId)
        {
            try
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<decimal>.Failure("Phải có userId hoặc sessionId");

                var total = await _cartItemRepository.GetCartTotalAsync(userId, sessionId);
                return ApiResult<decimal>.Success(total);
            }
            catch (Exception ex)
            {
                return ApiResult<decimal>.Failure("Lỗi khi tính tổng giỏ hàng", ex);
            }
        }

        public async Task<decimal> GetUnitPriceFromProduct(Guid productId)
        {
            var product = await _cartItemRepository.GetProductByIdAsync(productId);
            if (product == null)
                throw new Exception("Không tìm thấy sản phẩm");

            // Giả sử ưu tiên SalePrice, nếu không có thì lấy Price
            return product.SalePrice ?? product.Price;
        }

        public async Task<decimal> GetUnitPriceFromProductVariant(Guid productVariantId)
        {
            var productVariant = await _cartItemRepository.GetProductVariantByIdAsync(productVariantId);
            if (productVariant == null)
                throw new Exception("Không tìm thấy biến thể sản phẩm");

            // Lấy product gốc
            var product = await _cartItemRepository.GetProductByIdAsync(productVariant.ProductId);
            if (product == null)
                throw new Exception("Không tìm thấy sản phẩm gốc của biến thể");

            // Ưu tiên SalePrice nếu có, không có thì lấy Price
            decimal basePrice = product.SalePrice ?? product.Price;

            // Áp dụng điều chỉnh giá
            decimal finalPrice = basePrice + (productVariant.PriceAdjustment ?? 0);

            return finalPrice;
        }

        public async Task<ApiResult<IEnumerable<CartItemDto>>> GetCartItemsByIdsAsync(List<Guid> cartItemIds, Guid? userId, string? sessionId)
        {
            try
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<IEnumerable<CartItemDto>>.Failure("Phải có userId hoặc sessionId");

                if (!cartItemIds.Any())
                    return ApiResult<IEnumerable<CartItemDto>>.Success(new List<CartItemDto>());

                var cartItems = new List<CartItem>();

                foreach (var cartItemId in cartItemIds)
                {
                    var cartItem = await _cartItemRepository.GetWithDetailsAsync(cartItemId);
                    if (cartItem != null)
                    {
                        // Validate ownership
                        bool isOwner = userId.HasValue
                            ? cartItem.UserId == userId.Value
                            : cartItem.SessionId == sessionId && cartItem.UserId == null;

                        if (isOwner)
                        {
                            cartItems.Add(cartItem);
                        }
                    }
                }

                var cartItemDtos = CartItemMapper.ToDtoList(cartItems);
                return ApiResult<IEnumerable<CartItemDto>>.Success(cartItemDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<IEnumerable<CartItemDto>>.Failure("Lỗi khi lấy danh sách sản phẩm từ giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<bool>> ClearCartItemsByIdsAsync(List<Guid> cartItemIds, Guid? userId, string? sessionId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<bool>.Failure("Phải có userId hoặc sessionId");

                if (!cartItemIds.Any())
                    return ApiResult<bool>.Success(true);

                // Validate ownership before deletion
                foreach (var cartItemId in cartItemIds)
                {
                    var cartItem = await _cartItemRepository.GetByIdAsync(cartItemId);
                    if (cartItem != null)
                    {
                        bool isOwner = userId.HasValue
                            ? cartItem.UserId == userId.Value
                            : cartItem.SessionId == sessionId && cartItem.UserId == null;

                        if (isOwner)
                        {
                            await _cartItemRepository.DeleteAsync(cartItemId);
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return ApiResult<bool>.Success(true, "Xóa sản phẩm khỏi giỏ hàng thành công");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResult<bool>.Failure("Lỗi khi xóa sản phẩm khỏi giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<IEnumerable<CartItem>>> GetCartItemsForCheckoutAsync(Guid? userId, string? sessionId)
        {
            try
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<IEnumerable<CartItem>>.Failure("Phải có userId hoặc sessionId");

                IEnumerable<CartItem> cartItems;

                if (userId.HasValue)
                {
                    cartItems = await _cartItemRepository.GetUserCartItemsAsync(userId.Value);
                }
                else
                {
                    cartItems = await _cartItemRepository.GetSessionCartItemsAsync(sessionId!);
                }

                return ApiResult<IEnumerable<CartItem>>.Success(cartItems);
            }
            catch (Exception ex)
            {
                return ApiResult<IEnumerable<CartItem>>.Failure("Lỗi khi lấy giỏ hàng cho checkout", ex);
            }
        }

        public async Task<ApiResult<CartValidationDto>> ValidateCartForCheckoutAsync(Guid? userId, string? sessionId)
        {
            try
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<CartValidationDto>.Failure("Phải có userId hoặc sessionId");

                // Get cart items
                IEnumerable<CartItem> cartItems;
                if (userId.HasValue)
                {
                    cartItems = await _cartItemRepository.GetUserCartItemsAsync(userId.Value);
                }
                else
                {
                    cartItems = await _cartItemRepository.GetSessionCartItemsAsync(sessionId!);
                }

                var validationResult = new CartValidationDto
                {
                    TotalItems = cartItems.Count(),
                    Items = new List<CartItemValidationDto>()
                };

                if (!cartItems.Any())
                {
                    validationResult.Errors.Add("Giỏ hàng trống");
                    validationResult.IsValid = false;
                    validationResult.Summary = "Giỏ hàng trống, không thể checkout";
                    return ApiResult<CartValidationDto>.Success(validationResult);
                }

                decimal totalAmount = 0;
                var allValid = true;

                foreach (var cartItem in cartItems)
                {
                    var itemValidation = await ValidateCartItemAsync(cartItem);
                    validationResult.Items.Add(itemValidation);

                    if (!itemValidation.IsAvailable)
                    {
                        allValid = false;
                        validationResult.Errors.Add($"Sản phẩm '{itemValidation.ProductName}' không còn khả dụng");
                    }

                    if (itemValidation.HasStockIssue)
                    {
                        allValid = false;
                        validationResult.Errors.Add($"Sản phẩm '{itemValidation.ProductName}' không đủ hàng");
                    }

                    if (itemValidation.HasPriceChange)
                    {
                        var priceChangeMsg = itemValidation.PriceDifference > 0
                            ? $"Sản phẩm '{itemValidation.ProductName}' đã tăng giá {itemValidation.PriceDifference:C}"
                            : $"Sản phẩm '{itemValidation.ProductName}' đã giảm giá {Math.Abs(itemValidation.PriceDifference):C}";

                        // Price changes are warnings, not errors
                        if (itemValidation.WarningMessage == null)
                            itemValidation.WarningMessage = priceChangeMsg;
                    }

                    // Calculate total with current prices
                    totalAmount += itemValidation.CurrentPrice * itemValidation.Quantity;
                }

                validationResult.TotalAmount = totalAmount;
                validationResult.IsValid = allValid;

                // Generate summary
                if (validationResult.IsValid)
                {
                    if (validationResult.HasPriceChanges)
                    {
                        validationResult.Summary = "Giỏ hàng hợp lệ nhưng có thay đổi giá. Vui lòng xem lại trước khi checkout.";
                    }
                    else
                    {
                        validationResult.Summary = "Giỏ hàng hợp lệ và sẵn sàng checkout.";
                    }
                }
                else
                {
                    validationResult.Summary = $"Giỏ hàng có {validationResult.Errors.Count} lỗi cần khắc phục.";
                }

                return ApiResult<CartValidationDto>.Success(validationResult);
            }
            catch (Exception ex)
            {
                return ApiResult<CartValidationDto>.Failure("Lỗi khi kiểm tra giỏ hàng", ex);
            }
        }

        public async Task<ApiResult<bool>> UpdateCartPricesAsync(Guid? userId, string? sessionId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<bool>.Failure("Phải có userId hoặc sessionId");

                // Get cart items
                IEnumerable<CartItem> cartItems;
                if (userId.HasValue)
                {
                    cartItems = await _cartItemRepository.GetUserCartItemsAsync(userId.Value);
                }
                else
                {
                    cartItems = await _cartItemRepository.GetSessionCartItemsAsync(sessionId!);
                }

                if (!cartItems.Any())
                    return ApiResult<bool>.Success(true, "Giỏ hàng trống, không có gì để cập nhật");

                var updatedCount = 0;
                var errors = new List<string>();

                foreach (var cartItem in cartItems)
                {
                    try
                    {
                        decimal currentPrice = await GetCurrentPriceForCartItem(cartItem);

                        if (currentPrice != cartItem.UnitPrice)
                        {
                            var oldPrice = cartItem.UnitPrice;
                            cartItem.UnitPrice = currentPrice;
                            cartItem.UpdatedAt = _currentTime.GetVietnamTime();

                            await UpdateAsync(cartItem);
                            updatedCount++;

                            // Log price change
                            _logger?.LogInformation(
                                "Updated cart item {CartItemId} price from {OldPrice} to {NewPrice} for user {UserId}/session {SessionId}",
                                cartItem.Id, oldPrice, currentPrice, userId, sessionId);
                        }
                    }
                    catch (Exception ex)
                    {
                        var productName = GetProductNameFromCartItem(cartItem);
                        var errorMsg = $"Không thể cập nhật giá cho sản phẩm '{productName}': {ex.Message}";
                        errors.Add(errorMsg);

                        _logger?.LogError(ex, "Error updating price for cart item {CartItemId}", cartItem.Id);
                    }
                }

                if (errors.Any())
                {
                    await transaction.RollbackAsync();
                    return ApiResult<bool>.Failure($"Có lỗi khi cập nhật giá: {string.Join(", ", errors)}");
                }

                await transaction.CommitAsync();

                var message = updatedCount > 0
                    ? $"Đã cập nhật giá cho {updatedCount} sản phẩm trong giỏ hàng"
                    : "Tất cả giá sản phẩm đã là mới nhất";

                return ApiResult<bool>.Success(true, message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResult<bool>.Failure("Lỗi khi cập nhật giá giỏ hàng", ex);
            }
        }

        #region Private Helper Methods

        private async Task<ApiResult<CartItemDto>> ValidateCreateCartItemAsync(InternalCreateCartItemDto createDto)

        {
            // Validate user or session
            if (!CartItemBusinessLogic.ValidateUserOrSession(createDto.UserId, createDto.SessionId))
                return ApiResult<CartItemDto>.Failure("Phải có userId hoặc sessionId");

            // Validate product references
            if (!CartItemBusinessLogic.ValidateCartItemData(createDto.ProductId, createDto.CustomDesignId, createDto.ProductVariantId))
                return ApiResult<CartItemDto>.Failure("Phải có ít nhất một trong các ID: ProductId, CustomDesignId, hoặc ProductVariantId");

            // Validate quantity
            if (!CartItemBusinessLogic.IsValidQuantity(createDto.Quantity))
                return ApiResult<CartItemDto>.Failure("Số lượng không hợp lệ");

            // Validate unit price
            if (!CartItemBusinessLogic.IsValidUnitPrice(createDto.UnitPrice))
                return ApiResult<CartItemDto>.Failure("Đơn giá không hợp lệ");

            // Validate product exists
            if (createDto.ProductId.HasValue)
            {
                var productExists = await _cartItemRepository.ValidateProductExistsAsync(createDto.ProductId.Value);
                if (!productExists)
                    return ApiResult<CartItemDto>.Failure("Sản phẩm không tồn tại");
            }

            // Validate custom design exists
            if (createDto.CustomDesignId.HasValue)
            {
                var customDesignExists = await _cartItemRepository.ValidateCustomDesignExistsAsync(createDto.CustomDesignId.Value);
                if (!customDesignExists)
                    return ApiResult<CartItemDto>.Failure("Custom design không tồn tại");
            }

            // Validate product variant exists
            if (createDto.ProductVariantId.HasValue)
            {
                var productVariantExists = await _cartItemRepository.ValidateProductVariantExistsAsync(createDto.ProductVariantId.Value);
                if (!productVariantExists)
                    return ApiResult<CartItemDto>.Failure("Product variant không tồn tại");
            }

            return ApiResult<CartItemDto>.Success(null);
        }

        private static ApiResult<CartItemDto> ValidateUpdateCartItem(UpdateCartItemDto updateDto)
        {
            // Validate quantity
            if (!CartItemBusinessLogic.IsValidQuantity(updateDto.Quantity))
                return ApiResult<CartItemDto>.Failure("Số lượng không hợp lệ");

            // Validate unit price
            if (!CartItemBusinessLogic.IsValidUnitPrice(updateDto.UnitPrice))
                return ApiResult<CartItemDto>.Failure("Đơn giá không hợp lệ");

            return ApiResult<CartItemDto>.Success(null);
        }

        private static decimal CalculateEstimatedShipping(decimal subtotal)
        {
            // Free shipping for orders over 500,000 VND
            if (subtotal >= 500000m) return 0;
            return 25000m; // Default shipping fee
        }

        private static decimal CalculateEstimatedTax(decimal subtotal)
        {
            return subtotal * 0.1m; // 10% VAT
        }
        private async Task<CartItemValidationDto> ValidateCartItemAsync(CartItem cartItem)
        {
            var validation = new CartItemValidationDto
            {
                CartItemId = cartItem.Id,
                CartPrice = cartItem.UnitPrice,
                Quantity = cartItem.Quantity,
                IsAvailable = true,
                HasStockIssue = false,
                HasPriceChange = false
            };

            try
            {
                // Get current price and product info
                validation.CurrentPrice = await GetCurrentPriceForCartItem(cartItem);
                validation.ProductName = GetProductNameFromCartItem(cartItem);
                validation.VariantInfo = GetVariantInfoFromCartItem(cartItem);

                // Check price changes
                if (Math.Abs(validation.CurrentPrice - validation.CartPrice) > 0.01m) // Allow for small floating point differences
                {
                    validation.HasPriceChange = true;
                }

                // Check product availability
                if (cartItem.ProductId.HasValue)
                {
                    var product = await _cartItemRepository.GetProductByIdAsync(cartItem.ProductId.Value);
                    if (product == null || product.IsDeleted)
                    {
                        validation.IsAvailable = false;
                        validation.ErrorMessage = "Sản phẩm không còn tồn tại";
                        return validation;
                    }

                    // You can add more product availability checks here
                    // For example: product.IsActive, product.Status, etc.
                }

                // Check variant availability
                if (cartItem.ProductVariantId.HasValue)
                {
                    var variant = await _cartItemRepository.GetProductVariantByIdAsync(cartItem.ProductVariantId.Value);
                    if (variant == null)
                    {
                        validation.IsAvailable = false;
                        validation.ErrorMessage = "Biến thể sản phẩm không còn tồn tại";
                        return validation;
                    }

                    // You can add stock checking here if implemented
                    // if (variant.StockQuantity < cartItem.Quantity)
                    // {
                    //     validation.HasStockIssue = true;
                    //     validation.ErrorMessage = $"Chỉ còn {variant.StockQuantity} sản phẩm trong kho";
                    // }
                }

                // Check custom design availability
                if (cartItem.CustomDesignId.HasValue)
                {
                    var customDesignExists = await _cartItemRepository.ValidateCustomDesignExistsAsync(cartItem.CustomDesignId.Value);
                    if (!customDesignExists)
                    {
                        validation.IsAvailable = false;
                        validation.ErrorMessage = "Thiết kế tùy chỉnh không còn tồn tại";
                        return validation;
                    }
                }
            }
            catch (Exception ex)
            {
                validation.IsAvailable = false;
                validation.ErrorMessage = $"Lỗi khi kiểm tra sản phẩm: {ex.Message}";
            }

            return validation;
        }

        private async Task<decimal> GetCurrentPriceForCartItem(CartItem cartItem)
        {
            try
            {
                if (cartItem.ProductVariantId.HasValue)
                {
                    return await GetUnitPriceFromProductVariant(cartItem.ProductVariantId.Value);
                }
                else if (cartItem.ProductId.HasValue)
                {
                    return await GetUnitPriceFromProduct(cartItem.ProductId.Value);
                }
                else if (cartItem.CustomDesignId.HasValue)
                {
                    // Assuming custom designs have a price method
                    // You might need to implement this in your repository
                    // For now, return the current cart price as fallback
                    return cartItem.UnitPrice;
                }
                else
                {
                    throw new InvalidOperationException("Cart item không có product, variant, hoặc custom design ID");
                }
            }
            catch (Exception)
            {
                // If we can't get current price, return cart price to avoid breaking the flow
                return cartItem.UnitPrice;
            }
        }

        private string GetProductNameFromCartItem(CartItem cartItem)
        {
            if (cartItem.Product != null)
                return cartItem.Product.Name;

            if (cartItem.CustomDesign != null)
                return cartItem.CustomDesign.DesignName;

            if (cartItem.ProductVariant?.Product != null)
                return cartItem.ProductVariant.Product.Name;

            return "Sản phẩm không xác định";
        }

        private string? GetVariantInfoFromCartItem(CartItem cartItem)
        {
            if (cartItem.ProductVariant != null)
            {
                return $"{cartItem.ProductVariant.Color} - {cartItem.ProductVariant.Size}";
            }

            if (cartItem.SelectedColor.HasValue || cartItem.SelectedSize.HasValue)
            {
                var colorInfo = cartItem.SelectedColor?.ToString() ?? "";
                var sizeInfo = cartItem.SelectedSize?.ToString() ?? "";
                return $"{colorInfo} - {sizeInfo}".Trim(' ', '-');
            }

            return null;
        }

        #endregion
    }
}