using BusinessObjects.Wishlists;
using DTOs.Wishlists;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class WishlistRepository : GenericRepository<WishlistItem, Guid>, IWishlistRepository
    {
        public WishlistRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<IEnumerable<WishlistItem>> GetUserWishlistAsync(Guid userId)
        {
            return await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<WishlistItem?> GetWishlistItemAsync(Guid userId, Guid productId)
        {
            return await _context.WishlistItems
                .Include(w => w.Product)
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        }

        public async Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId)
        {
            return await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        }

        public async Task<bool> RemoveFromWishlistAsync(Guid userId, Guid productId)
        {
            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (item == null)
                return false;

            _context.WishlistItems.Remove(item);
            return true;
        }

        public async Task<IEnumerable<WishlistItem>> GetWishlistsByProductIdAsync(Guid productId)
        {
            return await _context.WishlistItems
                .Where(w => w.ProductId == productId)
                .Include(w => w.User)
                .Include(w => w.Product)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<WishlistStatsDto> GetWishlistStatsAsync()
        {
            var wishlistItems = await _context.WishlistItems
                .Include(w => w.Product)
                .ToListAsync();

            if (!wishlistItems.Any())
            {
                return new WishlistStatsDto();
            }

            var topProducts = await GetTopWishlistedProductsAsync(5);

            var trends = wishlistItems
                .GroupBy(w => w.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            return new WishlistStatsDto
            {
                TotalWishlistItems = wishlistItems.Count,
                UniqueUsers = wishlistItems.Select(w => w.UserId).Distinct().Count(),
                UniqueProducts = wishlistItems.Select(w => w.ProductId).Distinct().Count(),
                TotalWishlistValue = wishlistItems.Sum(w => w.Product?.Price ?? 0),
                TopWishlistedProducts = topProducts.ToList(),
                WishlistTrends = trends
            };
        }

        public async Task<UserWishlistSummaryDto> GetUserWishlistSummaryAsync(Guid userId)
        {
            var userWishlist = await GetUserWishlistAsync(userId);
            var user = await _context.Users.FindAsync(userId);

            if (!userWishlist.Any())
            {
                return new UserWishlistSummaryDto
                {
                    UserId = userId,
                    UserName = user?.UserName ?? "Unknown User",
                    TotalWishlistItems = 0,
                    TotalWishlistValue = 0,
                    LastWishlistActivity = DateTime.MinValue,
                    RecentItems = new List<WishlistItemDto>()
                };
            }

            var recentItems = userWishlist.Take(5).Select(w => new WishlistItemDto
            {
                Id = w.Id,
                UserId = w.UserId,
                UserName = w.User?.UserName ?? "Unknown User",
                ProductId = w.ProductId,
                ProductName = w.Product?.Name ?? "Unknown Product",
                ProductImageUrl = GetFirstImageFromJson(w.Product?.Images),
                ProductPrice = w.Product?.Price ?? 0,
                ProductDescription = w.Product?.Description,
                IsProductAvailable = w.Product?.Status == BusinessObjects.Products.ProductStatus.Active,
                ProductStock = w.Product?.Quantity ?? 0,
                CreatedAt = w.CreatedAt
            }).ToList();

            return new UserWishlistSummaryDto
            {
                UserId = userId,
                UserName = user?.UserName ?? "Unknown User",
                TotalWishlistItems = userWishlist.Count(),
                TotalWishlistValue = userWishlist.Sum(w => w.Product?.Price ?? 0),
                LastWishlistActivity = userWishlist.Max(w => w.CreatedAt),
                RecentItems = recentItems
            };
        }

        public async Task<IEnumerable<ProductWishlistStatsDto>> GetTopWishlistedProductsAsync(int count = 10)
        {
            return await _context.WishlistItems
                .Include(w => w.Product)
                .GroupBy(w => w.ProductId)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => new ProductWishlistStatsDto
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product!.Name,
                    ProductImageUrl = GetFirstImageFromJson(g.First().Product!.Images),
                    WishlistCount = g.Count(),
                    ProductPrice = g.First().Product!.Price
                })
                .ToListAsync();
        }

        public async Task<int> GetProductWishlistCountAsync(Guid productId)
        {
            return await _context.WishlistItems
                .CountAsync(w => w.ProductId == productId);
        }

        public async Task<bool> ClearUserWishlistAsync(Guid userId)
        {
            var userWishlistItems = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .ToListAsync();

            if (!userWishlistItems.Any())
                return false;

            _context.WishlistItems.RemoveRange(userWishlistItems);
            return true;
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