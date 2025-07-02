using BusinessObjects.Wishlists;
using BusinessObjects.Products;
using DTOs.Wishlists;
using Microsoft.EntityFrameworkCore;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Interfaces;

namespace Services.Implementations
{
    public class WishlistService : BaseService<WishlistItem, Guid>, IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICartItemRepository _cartItemRepository;

        public WishlistService(
            IWishlistRepository wishlistRepository,
            IProductRepository productRepository,
            ICartItemRepository cartItemRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime)
            : base(wishlistRepository, currentUserService, unitOfWork, currentTime)
        {
            _wishlistRepository = wishlistRepository;
            _productRepository = productRepository;
            _cartItemRepository = cartItemRepository;
        }

        public async Task<ApiResult<List<WishlistItemDto>>> GetUserWishlistAsync(Guid userId)
        {
            try
            {
                var wishlistItems = await _wishlistRepository.GetUserWishlistAsync(userId);
                var wishlistDtos = new List<WishlistItemDto>();

                foreach (var item in wishlistItems)
                {
                    wishlistDtos.Add(await MapToWishlistItemDto(item));
                }

                return ApiResult<List<WishlistItemDto>>.Success(wishlistDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<List<WishlistItemDto>>.Failure($"Error retrieving user wishlist: {ex.Message}");
            }
        }

        public async Task<ApiResult<PagedList<WishlistItemDto>>> GetWishlistsAsync(WishlistFilterDto filter)
        {
            try
            {
                var query = _wishlistRepository.GetQueryable()
                    .Include(w => w.User)
                    .Include(w => w.Product)
                    .AsQueryable();

                // Apply filters
                if (filter.UserId.HasValue)
                    query = query.Where(w => w.UserId == filter.UserId);

                if (filter.FromDate.HasValue)
                    query = query.Where(w => w.CreatedAt >= filter.FromDate);

                if (filter.ToDate.HasValue)
                    query = query.Where(w => w.CreatedAt <= filter.ToDate);

                if (filter.IsProductAvailable.HasValue)
                    query = query.Where(w => (w.Product!.Status == ProductStatus.Active) == filter.IsProductAvailable);

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                    query = query.Where(w => w.Product!.Name.Contains(filter.SearchTerm) ||
                                           w.User!.UserName!.Contains(filter.SearchTerm));

                // Apply ordering
                query = filter.OrderBy.ToLower() switch
                {
                    "productname" => filter.OrderByDescending ? query.OrderByDescending(w => w.Product!.Name) : query.OrderBy(w => w.Product!.Name),
                    "username" => filter.OrderByDescending ? query.OrderByDescending(w => w.User!.UserName) : query.OrderBy(w => w.User!.UserName),
                    "price" => filter.OrderByDescending ? query.OrderByDescending(w => w.Product!.Price) : query.OrderBy(w => w.Product!.Price),
                    _ => filter.OrderByDescending ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt)
                };

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var wishlistDtos = new List<WishlistItemDto>();
                foreach (var item in items)
                {
                    wishlistDtos.Add(await MapToWishlistItemDto(item));
                }

                var pagedResult = new PagedList<WishlistItemDto>(wishlistDtos, totalCount, filter.Page, filter.PageSize);
                return ApiResult<PagedList<WishlistItemDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                return ApiResult<PagedList<WishlistItemDto>>.Failure($"Error retrieving wishlists: {ex.Message}");
            }
        }

        public async Task<ApiResult<WishlistItemDto>> AddToWishlistAsync(AddToWishlistDto addDto)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (!currentUserId.HasValue)
                {
                    return ApiResult<WishlistItemDto>.Failure("User not authenticated");
                }

                // Check if product exists
                var product = await _productRepository.GetByIdAsync(addDto.ProductId);
                if (product == null)
                {
                    return ApiResult<WishlistItemDto>.Failure("Product not found");
                }

                // Check if product is already in wishlist
                var existingItem = await _wishlistRepository.IsProductInWishlistAsync(currentUserId.Value, addDto.ProductId);
                if (existingItem)
                {
                    return ApiResult<WishlistItemDto>.Failure("Product is already in your wishlist");
                }

                var wishlistItem = new WishlistItem
                {
                    UserId = currentUserId.Value,
                    ProductId = addDto.ProductId,
                    CreatedAt = _currentTime.GetVietnamTime()
                };

                await _wishlistRepository.AddAsync(wishlistItem);
                await _unitOfWork.SaveChangesAsync();

                // Reload with navigation properties
                var savedItem = await _wishlistRepository.GetWishlistItemAsync(currentUserId.Value, addDto.ProductId);
                var wishlistDto = await MapToWishlistItemDto(savedItem!);

                return ApiResult<WishlistItemDto>.Success(wishlistDto);
            }
            catch (Exception ex)
            {
                return ApiResult<WishlistItemDto>.Failure($"Error adding to wishlist: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> RemoveFromWishlistAsync(Guid userId, Guid productId)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (!currentUserId.HasValue)
                {
                    return ApiResult<bool>.Failure("User not authenticated");
                }

                // Users can only remove their own wishlist items, admins can remove any
                if (currentUserId.Value != userId && !IsAdminOrStaff())
                {
                    return ApiResult<bool>.Failure("You can only remove items from your own wishlist");
                }

                var result = await _wishlistRepository.RemoveFromWishlistAsync(userId, productId);
                if (!result)
                {
                    return ApiResult<bool>.Failure("Product not found in wishlist");
                }

                await _unitOfWork.SaveChangesAsync();
                return ApiResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error removing from wishlist: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> IsProductInWishlistAsync(Guid userId, Guid productId)
        {
            try
            {
                var isInWishlist = await _wishlistRepository.IsProductInWishlistAsync(userId, productId);
                return ApiResult<bool>.Success(isInWishlist);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error checking wishlist status: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> ClearUserWishlistAsync(Guid userId)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (!currentUserId.HasValue)
                {
                    return ApiResult<bool>.Failure("User not authenticated");
                }

                // Users can only clear their own wishlist, admins can clear any
                if (currentUserId.Value != userId && !IsAdminOrStaff())
                {
                    return ApiResult<bool>.Failure("You can only clear your own wishlist");
                }

                var result = await _wishlistRepository.ClearUserWishlistAsync(userId);
                if (!result)
                {
                    return ApiResult<bool>.Failure("No items found in wishlist");
                }

                await _unitOfWork.SaveChangesAsync();
                return ApiResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error clearing wishlist: {ex.Message}");
            }
        }

        public async Task<ApiResult<WishlistStatsDto>> GetWishlistStatsAsync()
        {
            try
            {
                var stats = await _wishlistRepository.GetWishlistStatsAsync();
                return ApiResult<WishlistStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                return ApiResult<WishlistStatsDto>.Failure($"Error retrieving wishlist stats: {ex.Message}");
            }
        }

        public async Task<ApiResult<UserWishlistSummaryDto>> GetUserWishlistSummaryAsync(Guid userId)
        {
            try
            {
                var summary = await _wishlistRepository.GetUserWishlistSummaryAsync(userId);
                return ApiResult<UserWishlistSummaryDto>.Success(summary);
            }
            catch (Exception ex)
            {
                return ApiResult<UserWishlistSummaryDto>.Failure($"Error retrieving user wishlist summary: {ex.Message}");
            }
        }

        public async Task<ApiResult<List<ProductWishlistStatsDto>>> GetTopWishlistedProductsAsync(int count = 10)
        {
            try
            {
                var topProducts = await _wishlistRepository.GetTopWishlistedProductsAsync(count);
                return ApiResult<List<ProductWishlistStatsDto>>.Success(topProducts.ToList());
            }
            catch (Exception ex)
            {
                return ApiResult<List<ProductWishlistStatsDto>>.Failure($"Error retrieving top wishlisted products: {ex.Message}");
            }
        }

        public async Task<ApiResult<int>> GetProductWishlistCountAsync(Guid productId)
        {
            try
            {
                var count = await _wishlistRepository.GetProductWishlistCountAsync(productId);
                return ApiResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                return ApiResult<int>.Failure($"Error retrieving product wishlist count: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> MoveWishlistToCartAsync(Guid userId, List<Guid> productIds)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (!currentUserId.HasValue)
                {
                    return ApiResult<bool>.Failure("User not authenticated");
                }

                if (currentUserId.Value != userId && !IsAdminOrStaff())
                {
                    return ApiResult<bool>.Failure("You can only move your own wishlist items");
                }

                var userWishlist = await _wishlistRepository.GetUserWishlistAsync(userId);
                var itemsToMove = userWishlist.Where(w => productIds.Contains(w.ProductId)).ToList();

                if (!itemsToMove.Any())
                {
                    return ApiResult<bool>.Failure("No valid wishlist items found");
                }

                var movedCount = 0;
                foreach (var wishlistItem in itemsToMove)
                {
                    // Check if product is already in cart
                    var existingCartItem = await _cartItemRepository.FindExistingCartItemAsync(userId, null, wishlistItem.ProductId, null, null);
                    
                    if (existingCartItem == null && wishlistItem.Product != null)
                    {
                        // Add to cart with default quantity 1
                        var cartItem = new BusinessObjects.Cart.CartItem
                        {
                            UserId = userId,
                            ProductId = wishlistItem.ProductId,
                            Quantity = 1,
                            CreatedAt = _currentTime.GetVietnamTime()
                        };

                        await _cartItemRepository.AddAsync(cartItem);
                        movedCount++;
                    }

                    // Remove from wishlist
                    await _wishlistRepository.RemoveFromWishlistAsync(userId, wishlistItem.ProductId);
                }

                await _unitOfWork.SaveChangesAsync();

                if (movedCount == 0)
                {
                    return ApiResult<bool>.Failure("No items were moved (all products were already in cart)");
                }

                return ApiResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error moving wishlist to cart: {ex.Message}");
            }
        }

        private async Task<WishlistItemDto> MapToWishlistItemDto(WishlistItem wishlistItem)
        {
            return await Task.FromResult(new WishlistItemDto
            {
                Id = wishlistItem.Id,
                UserId = wishlistItem.UserId,
                UserName = wishlistItem.User?.UserName ?? "Unknown User",
                ProductId = wishlistItem.ProductId,
                ProductName = wishlistItem.Product?.Name ?? "Unknown Product",
                ProductImageUrl = GetFirstImageFromJson(wishlistItem.Product?.Images),
                ProductPrice = wishlistItem.Product?.Price ?? 0,
                ProductDescription = wishlistItem.Product?.Description,
                IsProductAvailable = wishlistItem.Product?.Status == ProductStatus.Active,
                ProductStock = wishlistItem.Product?.Quantity ?? 0,
                CreatedAt = wishlistItem.CreatedAt
            });
        }

        private bool IsAdminOrStaff()
        {
            // This would need to check user roles - simplified implementation
            return false; // Implement based on your user context
        }

        private static string? GetFirstImageFromJson(string? imagesJson)
        {
            if (string.IsNullOrEmpty(imagesJson))
                return null;

            try
            {
                var images = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imagesJson);
                return images?.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}