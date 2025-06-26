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
                    createDto.CustomDesignId, createDto.ProductVariantId,
                    createDto.SelectedColor, createDto.SelectedSize);

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

        #endregion
    }
}