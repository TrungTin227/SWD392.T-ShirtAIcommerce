using BusinessObjects.Cart;
using DTOs.Cart;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<CartItemService> _logger;


        public CartItemService(
            ICartItemRepository repository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            ILogger<CartItemService> logger)
            : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _cartItemRepository = repository;
            _logger = logger;
        }

        #region Basic CRUD Operations

        public async Task<ApiResult<CartItemDto>> GetByIdAsync(Guid id)
        {
            return await ExecuteAsync(async () =>
            {
                var cartItem = await _cartItemRepository.GetWithDetailsAsync(id);
                return cartItem == null
                    ? ApiResult<CartItemDto>.Failure("Không tìm thấy sản phẩm trong giỏ hàng")
                    : ApiResult<CartItemDto>.Success(CartItemMapper.ToDto(cartItem));
            }, "Lỗi khi lấy thông tin sản phẩm trong giỏ hàng");
        }

        public async Task<ApiResult<CartItemDto>> AddToCartAsync(InternalCreateCartItemDto createDto)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            return await ExecuteWithTransactionAsync(transaction, async () =>
            {
                var validationResult = await ValidateCreateCartItemAsync(createDto);
                if (!validationResult.IsSuccess) return validationResult;

                var existingItem = await _cartItemRepository.FindExistingCartItemAsync(
                    createDto.UserId, createDto.SessionId, createDto.ProductId,
                    createDto.CustomDesignId, createDto.ProductVariantId);

                CartItem cartItem = existingItem != null
                    ? await UpdateExistingCartItem(existingItem, createDto)
                    : await CreateNewCartItem(createDto);

                return ApiResult<CartItemDto>.Success(CartItemMapper.ToDto(cartItem), "Thêm vào giỏ hàng thành công");
            }, "Lỗi khi thêm vào giỏ hàng");
        }

        public async Task<ApiResult<CartItemDto>> UpdateCartItemAsync(Guid id, UpdateCartItemDto updateDto)
        {
            return await ExecuteAsync(async () =>
            {
                var existingCartItem = await _cartItemRepository.GetByIdAsync(id);
                if (existingCartItem == null)
                    return ApiResult<CartItemDto>.Failure("Không tìm thấy sản phẩm trong giỏ hàng");

                var validationResult = ValidateUpdateCartItem(updateDto);
                if (!validationResult.IsSuccess) return validationResult;

                CartItemMapper.UpdateEntity(existingCartItem, updateDto);
                var updatedCartItem = await UpdateAsync(existingCartItem);

                return ApiResult<CartItemDto>.Success(CartItemMapper.ToDto(updatedCartItem), "Cập nhật giỏ hàng thành công");
            }, "Lỗi khi cập nhật giỏ hàng");
        }

        public async Task<ApiResult<bool>> RemoveFromCartAsync(Guid id)
        {
            return await ExecuteAsync(async () =>
            {
                var cartItem = await _cartItemRepository.GetByIdAsync(id);
                if (cartItem == null)
                    return ApiResult<bool>.Failure("Không tìm thấy sản phẩm trong giỏ hàng");

                var result = await DeleteAsync(id);
                return ApiResult<bool>.Success(result, "Xóa khỏi giỏ hàng thành công");
            }, "Lỗi khi xóa khỏi giỏ hàng");
        }

        #endregion

        #region Cart Retrieval Methods

        public async Task<ApiResult<PagedList<CartItemDto>>> GetCartItemsAsync(CartItemQueryDto query)
        {
            return await ExecuteAsync(async () =>
            {
                var pagedCartItems = await _cartItemRepository.GetCartItemsAsync(query);
                var cartItemDtos = CartItemMapper.ToPagedDto(pagedCartItems);
                return ApiResult<PagedList<CartItemDto>>.Success(cartItemDtos);
            }, "Lỗi khi lấy danh sách sản phẩm trong giỏ hàng");
        }

        public async Task<ApiResult<IEnumerable<CartItemDto>>> GetUserCartItemsAsync(Guid userId)
        {
            return (ApiResult<IEnumerable<CartItemDto>>)await GetCartItemsAsync(userId, null);
        }

        public async Task<ApiResult<IEnumerable<CartItemDto>>> GetSessionCartItemsAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return ApiResult<IEnumerable<CartItemDto>>.Failure("Session ID không được để trống");

            return (ApiResult<IEnumerable<CartItemDto>>)await GetCartItemsAsync(null, sessionId);
        }

        public async Task<ApiResult<IEnumerable<CartItemDto>>> GetCartItemsByIdsAsync(List<Guid> cartItemIds, Guid? userId, string? sessionId)
        {
            return await ExecuteAsync(async () =>
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<IEnumerable<CartItemDto>>.Failure("Phải có userId hoặc sessionId");

                if (!cartItemIds.Any())
                    return ApiResult<IEnumerable<CartItemDto>>.Success(new List<CartItemDto>());

                var cartItems = await GetValidatedCartItemsByIds(cartItemIds, userId, sessionId);
                var cartItemDtos = CartItemMapper.ToDtoList(cartItems);
                return ApiResult<IEnumerable<CartItemDto>>.Success(cartItemDtos);
            }, "Lỗi khi lấy danh sách sản phẩm từ giỏ hàng");
        }

        #endregion

        #region Cart Management

        public async Task<ApiResult<bool>> ClearCartAsync(Guid? userId, string? sessionId)
        {
            return await ExecuteAsync(async () =>
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<bool>.Failure("Phải có userId hoặc sessionId");

                bool result = userId.HasValue
                    ? await _cartItemRepository.ClearUserCartAsync(userId.Value)
                    : await _cartItemRepository.ClearSessionCartAsync(sessionId!);

                if (result) await _unitOfWork.SaveChangesAsync();
                return ApiResult<bool>.Success(result, "Xóa giỏ hàng thành công");
            }, "Lỗi khi xóa giỏ hàng");
        }

        public async Task<ApiResult<bool>> ClearCartItemsByIdsAsync(List<Guid> cartItemIds, Guid? userId, string? sessionId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            return await ExecuteWithTransactionAsync(transaction, async () =>
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<bool>.Failure("Phải có userId hoặc sessionId");

                if (!cartItemIds.Any())
                    return ApiResult<bool>.Success(true);

                await DeleteCartItemsByOwnership(cartItemIds, userId, sessionId);
                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true, "Xóa sản phẩm khỏi giỏ hàng thành công");
            }, "Lỗi khi xóa sản phẩm khỏi giỏ hàng");
        }

        public async Task<ApiResult<bool>> MergeGuestCartToUserAsync(string sessionId, Guid userId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return ApiResult<bool>.Failure("Session ID không được để trống");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            return await ExecuteWithTransactionAsync(transaction, async () =>
            {
                var result = await _cartItemRepository.MergeGuestCartToUserAsync(sessionId, userId);
                if (result) await _unitOfWork.SaveChangesAsync();
                return ApiResult<bool>.Success(result, "Merge giỏ hàng thành công");
            }, "Lỗi khi merge giỏ hàng");
        }

        #endregion

        #region Cart Summary & Statistics

        public async Task<ApiResult<CartSummaryDto>> GetCartSummaryAsync(Guid? userId, string? sessionId)
        {
            return await ExecuteAsync(async () =>
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<CartSummaryDto>.Failure("Phải có userId hoặc sessionId");

                var cartItems = await GetCartItems(userId, sessionId);
                var subtotal = cartItems.Sum(ci => ci.TotalPrice);
                var estimatedShipping = CalculateEstimatedShipping(subtotal);
                var estimatedTax = CalculateEstimatedTax(subtotal);

                var cartSummary = CartItemMapper.ToCartSummaryDto(cartItems, estimatedShipping, estimatedTax);
                return ApiResult<CartSummaryDto>.Success(cartSummary);
            }, "Lỗi khi lấy tổng quan giỏ hàng");
        }

        public async Task<ApiResult<int>> GetCartItemCountAsync(Guid? userId, string? sessionId)
        {
            return await ExecuteAsync(async () =>
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<int>.Failure("Phải có userId hoặc sessionId");

                var count = await _cartItemRepository.GetCartItemCountAsync(userId, sessionId);
                return ApiResult<int>.Success(count);
            }, "Lỗi khi đếm sản phẩm trong giỏ hàng");
        }

        public async Task<ApiResult<decimal>> GetCartTotalAsync(Guid? userId, string? sessionId)
        {
            return await ExecuteAsync(async () =>
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<decimal>.Failure("Phải có userId hoặc sessionId");

                var total = await _cartItemRepository.GetCartTotalAsync(userId, sessionId);
                return ApiResult<decimal>.Success(total);
            }, "Lỗi khi tính tổng giỏ hàng");
        }

        #endregion

        #region Checkout Related Methods

        public async Task<ApiResult<IEnumerable<CartItem>>> GetCartItemEntitiesForCheckoutAsync(Guid? userId, string? sessionId)
        {
            return await ExecuteAsync(async () =>
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<IEnumerable<CartItem>>.Failure("Phải có userId hoặc sessionId");

                var cartItems = userId.HasValue
                    ? await _cartItemRepository.GetUserCartItemsWithDetailsAsync(userId.Value)
                    : await _cartItemRepository.GetSessionCartItemsWithDetailsAsync(sessionId!);

                return ApiResult<IEnumerable<CartItem>>.Success(cartItems);
            }, "Lỗi khi lấy giỏ hàng cho checkout");
        }

        public async Task<ApiResult<bool>> ClearCartItemsAfterCheckoutAsync(List<Guid> cartItemIds, Guid? userId, string? sessionId)
        {
            return await ClearCartItemsByIdsAsync(cartItemIds, userId, sessionId);
        }

        public async Task<ApiResult<CartValidationDto>> ValidateCartForCheckoutAsync(Guid? userId, string? sessionId)
        {
            return await ExecuteAsync(async () =>
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<CartValidationDto>.Failure("Phải có userId hoặc sessionId");

                var cartItems = await GetCartItems(userId, sessionId);
                return await BuildCartValidationResult(cartItems, false);
            }, "Lỗi khi kiểm tra giỏ hàng");
        }

        public async Task<ApiResult<CartValidationDto>> ValidateCartForCheckoutDetailedAsync(Guid? userId, string? sessionId)
        {
            return await ExecuteAsync(async () =>
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<CartValidationDto>.Failure("Phải có userId hoặc sessionId");

                var cartItemsResult = await GetCartItemEntitiesForCheckoutAsync(userId, sessionId);
                if (!cartItemsResult.IsSuccess)
                    return ApiResult<CartValidationDto>.Failure(cartItemsResult.Message);

                var cartItems = cartItemsResult.Data ?? Enumerable.Empty<CartItem>();
                return await BuildCartValidationResult(cartItems, true);
            }, "Lỗi khi kiểm tra giỏ hàng chi tiết");
        }

        public async Task<ApiResult<bool>> UpdateCartPricesAsync(Guid? userId, string? sessionId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            return await ExecuteWithTransactionAsync(transaction, async () =>
            {
                if (!CartItemBusinessLogic.ValidateUserOrSession(userId, sessionId))
                    return ApiResult<bool>.Failure("Phải có userId hoặc sessionId");

                var cartItems = await GetCartItems(userId, sessionId);
                if (!cartItems.Any())
                    return ApiResult<bool>.Success(true, "Giỏ hàng trống, không có gì để cập nhật");

                var updatedCount = await UpdateCartItemPrices(cartItems);
                var message = updatedCount > 0
                    ? $"Đã cập nhật giá cho {updatedCount} sản phẩm trong giỏ hàng"
                    : "Tất cả giá sản phẩm đã là mới nhất";

                return ApiResult<bool>.Success(true, message);
            }, "Lỗi khi cập nhật giá giỏ hàng");
        }

        #endregion

        #region Price Calculation Methods

        public async Task<decimal> GetUnitPriceFromProduct(Guid productId)
        {
            var product = await _cartItemRepository.GetProductByIdAsync(productId);
            if (product == null)
                throw new Exception("Không tìm thấy sản phẩm");

            return product.SalePrice ?? product.Price;
        }

        public async Task<decimal> GetUnitPriceFromProductVariant(Guid productVariantId)
        {
            var productVariant = await _cartItemRepository.GetProductVariantByIdAsync(productVariantId);
            if (productVariant == null)
                throw new Exception("Không tìm thấy biến thể sản phẩm");

            var product = await _cartItemRepository.GetProductByIdAsync(productVariant.ProductId);
            if (product == null)
                throw new Exception("Không tìm thấy sản phẩm gốc của biến thể");

            decimal basePrice = product.SalePrice ?? product.Price;
            return basePrice + (productVariant.PriceAdjustment ?? 0);
        }

        #endregion

        #region Private Helper Methods

        private async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string errorMessage) where T : class
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ApiResult<>))
                {
                    var resultType = typeof(T).GetGenericArguments()[0];
                    var failureMethod = typeof(ApiResult<>).MakeGenericType(resultType)
                        .GetMethod("Failure", new[] { typeof(string), typeof(Exception) });
                    return (T)failureMethod.Invoke(null, new object[] { errorMessage, ex });
                }
                throw;
            }
        }

        private async Task<T> ExecuteWithTransactionAsync<T>(IDbContextTransaction transaction, Func<Task<T>> operation, string errorMessage) where T : class
        {
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ApiResult<>))
                {
                    var resultType = typeof(T).GetGenericArguments()[0];
                    var failureMethod = typeof(ApiResult<>).MakeGenericType(resultType)
                        .GetMethod("Failure", new[] { typeof(string), typeof(Exception) });
                    return (T)failureMethod.Invoke(null, new object[] { errorMessage, ex });
                }
                throw;
            }
        }

        private async Task<CartItem> UpdateExistingCartItem(CartItem existingItem, InternalCreateCartItemDto createDto)
        {
            existingItem.Quantity += createDto.Quantity;
            existingItem.UnitPrice = createDto.UnitPrice;
            existingItem.UpdatedAt = _currentTime.GetVietnamTime();
            return await UpdateAsync(existingItem);
        }

        private async Task<CartItem> CreateNewCartItem(InternalCreateCartItemDto createDto)
        {
            var cartItem = CartItemMapper.ToEntity(createDto);
            return await CreateAsync(cartItem);
        }

        private async Task<ApiResult<IEnumerable<CartItemDto>>> GetCartItemsAsync(Guid? userId, string? sessionId)
        {
            return await ExecuteAsync(async () =>
            {
                var cartItems = await GetCartItems(userId, sessionId);
                var cartItemDtos = CartItemMapper.ToDtoList(cartItems);
                return ApiResult<IEnumerable<CartItemDto>>.Success(cartItemDtos);
            }, "Lỗi khi lấy giỏ hàng");
        }

        private async Task<IEnumerable<CartItem>> GetCartItems(Guid? userId, string? sessionId)
        {
            return userId.HasValue
                ? await _cartItemRepository.GetUserCartItemsAsync(userId.Value)
                : await _cartItemRepository.GetSessionCartItemsAsync(sessionId!);
        }

        private async Task<List<CartItem>> GetValidatedCartItemsByIds(List<Guid> cartItemIds, Guid? userId, string? sessionId)
        {
            var cartItems = new List<CartItem>();
            foreach (var cartItemId in cartItemIds)
            {
                var cartItem = await _cartItemRepository.GetWithDetailsAsync(cartItemId);
                if (cartItem != null && IsOwner(cartItem, userId, sessionId))
                {
                    cartItems.Add(cartItem);
                }
            }
            return cartItems;
        }

        private async Task DeleteCartItemsByOwnership(List<Guid> cartItemIds, Guid? userId, string? sessionId)
        {
            foreach (var cartItemId in cartItemIds)
            {
                var cartItem = await _cartItemRepository.GetByIdAsync(cartItemId);
                if (cartItem != null && IsOwner(cartItem, userId, sessionId))
                {
                    await _cartItemRepository.DeleteAsync(cartItemId);
                }
            }
        }

        private bool IsOwner(CartItem cartItem, Guid? userId, string? sessionId)
        {
            return userId.HasValue
                ? cartItem.UserId == userId.Value
                : cartItem.SessionId == sessionId && cartItem.UserId == null;
        }

        private async Task<ApiResult<CartValidationDto>> BuildCartValidationResult(IEnumerable<CartItem> cartItems, bool isDetailed)
        {
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
                var itemValidation = isDetailed
                    ? await ValidateCartItemDetailedAsync(cartItem)
                    : await ValidateCartItemAsync(cartItem);

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

                    validationResult.Warnings.Add(priceChangeMsg);
                }

                totalAmount += itemValidation.CurrentPrice * itemValidation.Quantity;
            }

            validationResult.TotalAmount = totalAmount;
            validationResult.IsValid = allValid;
            validationResult.HasPriceChanges = validationResult.Items.Any(i => i.HasPriceChange);
            validationResult.Summary = GenerateValidationSummary(validationResult);

            return ApiResult<CartValidationDto>.Success(validationResult);
        }

        private string GenerateValidationSummary(CartValidationDto validation)
        {
            if (validation.IsValid)
            {
                return validation.HasPriceChanges
                    ? "Giỏ hàng hợp lệ nhưng có thay đổi giá. Vui lòng xem lại trước khi checkout."
                    : "Giỏ hàng hợp lệ và sẵn sàng checkout.";
            }
            return $"Giỏ hàng có {validation.Errors.Count} lỗi cần khắc phục.";
        }

        private async Task<int> UpdateCartItemPrices(IEnumerable<CartItem> cartItems)
        {
            var updatedCount = 0;
            foreach (var cartItem in cartItems)
            {
                try
                {
                    decimal currentPrice = await GetCurrentPriceForCartItem(cartItem);
                    if (currentPrice != cartItem.UnitPrice)
                    {
                        cartItem.UnitPrice = currentPrice;
                        cartItem.UpdatedAt = _currentTime.GetVietnamTime();
                        await UpdateAsync(cartItem);
                        updatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other items
                    _logger?.LogError(ex, "Error updating price for cart item {CartItemId}", cartItem.Id);
                }
            }
            return updatedCount;
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
                HasPriceChange = false,
                AvailableQuantity = 0 // Initialize with default value
            };

            try
            {
                // Get current price and product info
                validation.CurrentPrice = await GetCurrentPriceForCartItem(cartItem);
                validation.ProductName = GetProductNameFromCartItem(cartItem);
                validation.VariantInfo = GetVariantInfoFromCartItem(cartItem);

                // Check price changes
                validation.PriceDifference = validation.CurrentPrice - validation.CartPrice;
                if (Math.Abs(validation.PriceDifference) > 0.01m)
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

                    // Stock checking if implemented
                    if (variant.Quantity < cartItem.Quantity)
                    {
                        validation.HasStockIssue = true;
                        validation.AvailableQuantity = variant.Quantity;
                        validation.ErrorMessage = $"Chỉ còn {variant.Quantity} sản phẩm trong kho";
                    }
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

        private async Task<CartItemValidationDto> ValidateCartItemDetailedAsync(CartItem cartItem)
        {
            var validation = await ValidateCartItemAsync(cartItem);

            // Add more detailed validation logic here if needed
            // This is where you would add enhanced stock checking, status validation, etc.

            return validation;
        }

        private async Task ValidateProductAvailability(CartItem cartItem, CartItemValidationDto validation)
        {
            if (cartItem.ProductId.HasValue)
            {
                var product = await _cartItemRepository.GetProductByIdAsync(cartItem.ProductId.Value);
                if (product == null || product.IsDeleted)
                {
                    validation.IsAvailable = false;
                    validation.ErrorMessage = "Sản phẩm không còn tồn tại";
                    return;
                }
            }

            if (cartItem.ProductVariantId.HasValue)
            {
                var variant = await _cartItemRepository.GetProductVariantByIdAsync(cartItem.ProductVariantId.Value);
                if (variant == null)
                {
                    validation.IsAvailable = false;
                    validation.ErrorMessage = "Biến thể sản phẩm không còn tồn tại";
                    return;
                }

                if (variant.Quantity < cartItem.Quantity)
                {
                    validation.HasStockIssue = true;
                    validation.AvailableQuantity = variant.Quantity;
                    validation.ErrorMessage = $"Chỉ còn {variant.Quantity} sản phẩm trong kho";
                }
            }

            if (cartItem.CustomDesignId.HasValue)
            {
                var customDesignExists = await _cartItemRepository.ValidateCustomDesignExistsAsync(cartItem.CustomDesignId.Value);
                if (!customDesignExists)
                {
                    validation.IsAvailable = false;
                    validation.ErrorMessage = "Thiết kế tùy chỉnh không còn tồn tại";
                }
            }
        }
        private string GetProductNameFromCartItem(CartItem cartItem)
        {
            return cartItem.Product?.Name
                ?? cartItem.CustomDesign?.DesignName
                ?? cartItem.ProductVariant?.Product?.Name
                ?? "Sản phẩm không xác định";
        }


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

        private string? GetVariantInfoFromCartItem(CartItem cartItem)
        {
            if (cartItem.ProductVariant != null)
            {
                var color = cartItem.ProductVariant.Color.ToString() ?? "";
                var size = cartItem.ProductVariant.Size.ToString() ?? "";
                return $"{color} - {size}".Trim(' ', '-');
            }

            return null;
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
                    cartItems = await _cartItemRepository.GetUserCartItemsWithDetailsAsync(userId.Value);
                }
                else
                {
                    cartItems = await _cartItemRepository.GetSessionCartItemsWithDetailsAsync(sessionId!);
                }

                return ApiResult<IEnumerable<CartItem>>.Success(cartItems);
            }
            catch (Exception ex)
            {
                return ApiResult<IEnumerable<CartItem>>.Failure("Lỗi khi lấy giỏ hàng cho checkout", ex);
            }
        }

        #endregion
    }
}