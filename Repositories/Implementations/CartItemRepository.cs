using BusinessObjects.Cart;
using BusinessObjects.CustomDesigns;
using BusinessObjects.Products;
using DTOs.Cart;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;
using System.Linq.Expressions;

namespace Repositories.Implementations
{
    public class CartItemRepository : GenericRepository<CartItem, Guid>, ICartItemRepository
    {
        public CartItemRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<PagedList<CartItem>> GetCartItemsAsync(CartItemQueryDto query)
        {
            // Sử dụng các include expressions từ common pattern
            var includes = new Expression<Func<CartItem, object>>[]
            {
        ci => ci.User,
        ci => ci.Product,
        ci => ci.CustomDesign,
        ci => ci.ProductVariant
            };

            // Predicate để filter dữ liệu
            Expression<Func<CartItem, bool>>? predicate = BuildFilterPredicate(query);

            // Ordering function
            Func<IQueryable<CartItem>, IOrderedQueryable<CartItem>>? orderBy = BuildOrderingFunction(query);

            // ✅ Sử dụng PageNumber và PageSize từ query (theo pattern OrderRepository)
            return await GetPagedAsync(
                query.PageNumber,
                query.PageSize,
                predicate,
                orderBy,
                includes);
        }
        public async Task<IEnumerable<CartItem>> GetUserCartItemsAsync(Guid userId)
        {
            return await GetAllAsync(
                ci => ci.UserId == userId,
                q => q.OrderBy(ci => ci.CreatedAt),
                ci => ci.Product,
                ci => ci.CustomDesign,
                ci => ci.ProductVariant);
        }

        public async Task<IEnumerable<CartItem>> GetSessionCartItemsAsync(string sessionId)
        {
            return await GetAllAsync(
                ci => ci.SessionId == sessionId && ci.UserId == null,
                q => q.OrderBy(ci => ci.CreatedAt),
                ci => ci.Product,
                ci => ci.CustomDesign,
                ci => ci.ProductVariant);
        }

        public async Task<CartItem?> GetWithDetailsAsync(Guid id)
        {
            return await GetByIdAsync(id,
                ci => ci.User,
                ci => ci.Product,
                ci => ci.CustomDesign,
                ci => ci.ProductVariant);
        }

        public async Task<CartItem?> FindExistingCartItemAsync(Guid? userId, string? sessionId, Guid? productId, Guid? customDesignId, Guid? productVariantId, string? selectedColor, string? selectedSize)
        {
            Expression<Func<CartItem, bool>> predicate = ci =>
                ci.UserId == userId &&
                ci.SessionId == sessionId &&
                ci.ProductId == productId &&
                ci.CustomDesignId == customDesignId &&
                ci.ProductVariantId == productVariantId &&
                ci.SelectedColor == selectedColor &&
                ci.SelectedSize == selectedSize;

            return await FirstOrDefaultAsync(predicate);
        }

        public async Task<bool> ClearUserCartAsync(Guid userId)
        {
            var cartItems = await GetAllAsync(ci => ci.UserId == userId);
            var cartItemIds = cartItems.Select(ci => ci.Id).ToList();

            if (cartItemIds.Any())
            {
                await DeleteRangeAsync(cartItemIds);
                return true;
            }
            return false;
        }

        public async Task<bool> ClearSessionCartAsync(string sessionId)
        {
            var cartItems = await GetAllAsync(ci => ci.SessionId == sessionId && ci.UserId == null);
            var cartItemIds = cartItems.Select(ci => ci.Id).ToList();

            if (cartItemIds.Any())
            {
                await DeleteRangeAsync(cartItemIds);
                return true;
            }
            return false;
        }

        public async Task<decimal> GetCartTotalAsync(Guid? userId, string? sessionId)
        {
            IEnumerable<CartItem> cartItems;

            if (userId.HasValue)
            {
                cartItems = await GetAllAsync(ci => ci.UserId == userId.Value);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cartItems = await GetAllAsync(ci => ci.SessionId == sessionId && ci.UserId == null);
            }
            else
            {
                return 0;
            }

            return cartItems.Sum(ci => ci.TotalPrice);
        }

        public async Task<int> GetCartItemCountAsync(Guid? userId, string? sessionId)
        {
            IEnumerable<CartItem> cartItems;

            if (userId.HasValue)
            {
                cartItems = await GetAllAsync(ci => ci.UserId == userId.Value);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cartItems = await GetAllAsync(ci => ci.SessionId == sessionId && ci.UserId == null);
            }
            else
            {
                return 0;
            }

            return cartItems.Sum(ci => ci.Quantity);
        }

        public async Task<bool> MergeGuestCartToUserAsync(string sessionId, Guid userId)
        {
            var guestCartItems = await GetAllAsync(ci => ci.SessionId == sessionId && ci.UserId == null);

            if (!guestCartItems.Any())
                return true;

            foreach (var guestItem in guestCartItems)
            {
                // Tìm item tương tự trong cart của user
                var existingUserItem = await FindExistingCartItemAsync(
                    userId, null, guestItem.ProductId, guestItem.CustomDesignId,
                    guestItem.ProductVariantId, guestItem.SelectedColor, guestItem.SelectedSize);

                if (existingUserItem != null)
                {
                    // Merge quantity
                    existingUserItem.Quantity += guestItem.Quantity;
                    existingUserItem.UpdatedAt = DateTime.UtcNow;
                    await UpdateAsync(existingUserItem);

                    // Delete guest item
                    await DeleteAsync(guestItem.Id);
                }
                else
                {
                    // Move guest item to user
                    guestItem.UserId = userId;
                    guestItem.SessionId = null;
                    guestItem.UpdatedAt = DateTime.UtcNow;
                    await UpdateAsync(guestItem);
                }
            }

            return true;
        }

        public async Task<bool> ValidateProductExistsAsync(Guid productId)
        {
            return await _context.Set<Product>().AnyAsync(p => p.Id == productId && !p.IsDeleted);
        }

        public async Task<bool> ValidateCustomDesignExistsAsync(Guid customDesignId)
        {
            return await _context.Set<CustomDesign>().AnyAsync(cd => cd.Id == customDesignId && !cd.IsDeleted);
        }

        public async Task<bool> ValidateProductVariantExistsAsync(Guid productVariantId)
        {
            return await _context.Set<ProductVariant>().AnyAsync(pv => pv.Id == productVariantId);
        }

        #region Private Helper Methods

        private static Expression<Func<CartItem, bool>>? BuildFilterPredicate(CartItemQueryDto query)
        {
            Expression<Func<CartItem, bool>>? predicate = null;

            if (query.UserId.HasValue)
                predicate = CombinePredicates(predicate, ci => ci.UserId == query.UserId.Value);

            if (!string.IsNullOrEmpty(query.SessionId))
                predicate = CombinePredicates(predicate, ci => ci.SessionId == query.SessionId);

            if (query.ProductId.HasValue)
                predicate = CombinePredicates(predicate, ci => ci.ProductId == query.ProductId.Value);

            if (query.CustomDesignId.HasValue)
                predicate = CombinePredicates(predicate, ci => ci.CustomDesignId == query.CustomDesignId.Value);

            if (query.ProductVariantId.HasValue)
                predicate = CombinePredicates(predicate, ci => ci.ProductVariantId == query.ProductVariantId.Value);

            if (!string.IsNullOrEmpty(query.SelectedColor))
                predicate = CombinePredicates(predicate, ci => ci.SelectedColor == query.SelectedColor);

            if (!string.IsNullOrEmpty(query.SelectedSize))
                predicate = CombinePredicates(predicate, ci => ci.SelectedSize == query.SelectedSize);

            if (query.MinQuantity.HasValue)
                predicate = CombinePredicates(predicate, ci => ci.Quantity >= query.MinQuantity.Value);

            if (query.MaxQuantity.HasValue)
                predicate = CombinePredicates(predicate, ci => ci.Quantity <= query.MaxQuantity.Value);

            if (query.MinUnitPrice.HasValue)
                predicate = CombinePredicates(predicate, ci => ci.UnitPrice >= query.MinUnitPrice.Value);

            if (query.MaxUnitPrice.HasValue)
                predicate = CombinePredicates(predicate, ci => ci.UnitPrice <= query.MaxUnitPrice.Value);

            if (query.FromDate.HasValue)
                predicate = CombinePredicates(predicate, ci => ci.CreatedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                predicate = CombinePredicates(predicate, ci => ci.CreatedAt <= query.ToDate.Value);

            if (!string.IsNullOrEmpty(query.Search))
            {
                predicate = CombinePredicates(predicate, ci =>
                    (ci.Product != null && ci.Product.Name.Contains(query.Search)) ||
                    (ci.CustomDesign != null && ci.CustomDesign.DesignName.Contains(query.Search)) ||
                    (ci.SelectedColor != null && ci.SelectedColor.Contains(query.Search)) ||
                    (ci.SelectedSize != null && ci.SelectedSize.Contains(query.Search)));
            }

            return predicate;
        }

        private static Func<IQueryable<CartItem>, IOrderedQueryable<CartItem>>? BuildOrderingFunction(CartItemQueryDto query)
        {
            if (string.IsNullOrEmpty(query.SortBy))
                return q => query.IsDescending ? q.OrderByDescending(ci => ci.CreatedAt) : q.OrderBy(ci => ci.CreatedAt);

            return query.SortBy?.ToLower() switch
            {
                "quantity" => q => query.IsDescending ? q.OrderByDescending(ci => ci.Quantity) : q.OrderBy(ci => ci.Quantity),
                "unitprice" => q => query.IsDescending ? q.OrderByDescending(ci => ci.UnitPrice) : q.OrderBy(ci => ci.UnitPrice),
                "totalprice" => q => query.IsDescending ? q.OrderByDescending(ci => ci.TotalPrice) : q.OrderBy(ci => ci.TotalPrice),
                "selectedcolor" => q => query.IsDescending ? q.OrderByDescending(ci => ci.SelectedColor) : q.OrderBy(ci => ci.SelectedColor),
                "selectedsize" => q => query.IsDescending ? q.OrderByDescending(ci => ci.SelectedSize) : q.OrderBy(ci => ci.SelectedSize),
                "createdat" => q => query.IsDescending ? q.OrderByDescending(ci => ci.CreatedAt) : q.OrderBy(ci => ci.CreatedAt),
                _ => q => query.IsDescending ? q.OrderByDescending(ci => ci.CreatedAt) : q.OrderBy(ci => ci.CreatedAt)
            };
        }

        private static Expression<Func<T, bool>>? CombinePredicates<T>(
            Expression<Func<T, bool>>? first,
            Expression<Func<T, bool>> second)
        {
            if (first == null)
                return second;

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.AndAlso(
                Expression.Invoke(first, parameter),
                Expression.Invoke(second, parameter));

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        #endregion
    }
}