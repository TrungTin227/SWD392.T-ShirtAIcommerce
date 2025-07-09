using BusinessObjects.Common;
using BusinessObjects.CustomDesigns;
using DTOs.CustomDesigns;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class CustomDesignRepository : GenericRepository<CustomDesign, Guid>, ICustomDesignRepository
    {
        public CustomDesignRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CustomDesign>> GetDesignsByUserIdAsync(Guid userId)
        {
            return await _context.CustomDesigns
                .Where(d => d.UserId == userId)
                .Include(d => d.User)
                .Include(d => d.Staff)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomDesign>> GetDesignsByStaffIdAsync(Guid staffId)
        {
            return await _context.CustomDesigns
                .Where(d => d.StaffId == staffId)
                .Include(d => d.User)
                .Include(d => d.Staff)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomDesign>> GetPendingDesignsAsync()
        {
            return await _context.CustomDesigns
                .Where(d => d.Status == DesignStatus.Submitted || d.Status == DesignStatus.UnderReview)
                .Include(d => d.User)
                .OrderBy(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<CustomDesignStatsDto> GetDesignStatsAsync()
        {
            var designs = await _context.CustomDesigns.ToListAsync();

            if (!designs.Any())
            {
                return new CustomDesignStatsDto();
            }

            var completedDesigns = designs.Where(d => d.Status == DesignStatus.Completed).ToList();
            var avgProcessingDays = completedDesigns.Any() && completedDesigns.All(d => d.CompletedAt.HasValue)
                ? completedDesigns.Average(d => (d.CompletedAt!.Value - d.CreatedAt).TotalDays)
                : 0;

            return new CustomDesignStatsDto
            {
                TotalDesigns = designs.Count,
                PendingDesigns = designs.Count(d => d.Status == DesignStatus.Submitted || d.Status == DesignStatus.UnderReview),
                ApprovedDesigns = designs.Count(d => d.Status == DesignStatus.Approved),
                InProductionDesigns = designs.Count(d => d.Status == DesignStatus.InProduction),
                CompletedDesigns = designs.Count(d => d.Status == DesignStatus.Completed),
                TotalRevenue = completedDesigns.Sum(d => d.TotalPrice * d.Quantity),
                AverageProcessingDays = avgProcessingDays,
                StatusDistribution = designs
                    .GroupBy(d => d.Status)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<CustomDesignStatsDto> GetUserDesignStatsAsync(Guid userId)
        {
            var designs = await _context.CustomDesigns
                .Where(d => d.UserId == userId)
                .ToListAsync();

            if (!designs.Any())
            {
                return new CustomDesignStatsDto();
            }

            var completedDesigns = designs.Where(d => d.Status == DesignStatus.Completed).ToList();
            var avgProcessingDays = completedDesigns.Any() && completedDesigns.All(d => d.CompletedAt.HasValue)
                ? completedDesigns.Average(d => (d.CompletedAt!.Value - d.CreatedAt).TotalDays)
                : 0;

            return new CustomDesignStatsDto
            {
                TotalDesigns = designs.Count,
                PendingDesigns = designs.Count(d => d.Status == DesignStatus.Submitted || d.Status == DesignStatus.UnderReview),
                ApprovedDesigns = designs.Count(d => d.Status == DesignStatus.Approved),
                InProductionDesigns = designs.Count(d => d.Status == DesignStatus.InProduction),
                CompletedDesigns = designs.Count(d => d.Status == DesignStatus.Completed),
                TotalRevenue = completedDesigns.Sum(d => d.TotalPrice * d.Quantity),
                AverageProcessingDays = avgProcessingDays,
                StatusDistribution = designs
                    .GroupBy(d => d.Status)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<CustomDesignStatsDto> GetStaffDesignStatsAsync(Guid staffId)
        {
            var designs = await _context.CustomDesigns
                .Where(d => d.StaffId == staffId)
                .ToListAsync();

            if (!designs.Any())
            {
                return new CustomDesignStatsDto();
            }

            var completedDesigns = designs.Where(d => d.Status == DesignStatus.Completed).ToList();
            var avgProcessingDays = completedDesigns.Any() && completedDesigns.All(d => d.CompletedAt.HasValue)
                ? completedDesigns.Average(d => (d.CompletedAt!.Value - d.CreatedAt).TotalDays)
                : 0;

            return new CustomDesignStatsDto
            {
                TotalDesigns = designs.Count,
                PendingDesigns = designs.Count(d => d.Status == DesignStatus.Submitted || d.Status == DesignStatus.UnderReview),
                ApprovedDesigns = designs.Count(d => d.Status == DesignStatus.Approved),
                InProductionDesigns = designs.Count(d => d.Status == DesignStatus.InProduction),
                CompletedDesigns = designs.Count(d => d.Status == DesignStatus.Completed),
                TotalRevenue = completedDesigns.Sum(d => d.TotalPrice * d.Quantity),
                AverageProcessingDays = avgProcessingDays,
                StatusDistribution = designs
                    .GroupBy(d => d.Status)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<IEnumerable<CustomDesign>> GetDesignsByStatusAsync(DesignStatus status)
        {
            return await _context.CustomDesigns
                .Where(d => d.Status == status)
                .Include(d => d.User)
                .Include(d => d.Staff)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> CalculateDesignPriceAsync(CreateCustomDesignDto designDto)
        {
            // Base pricing logic - this could be made configurable
            decimal basePrice = designDto.ShirtType switch
            {
                GarmentType.TShirt => 150000m,
                GarmentType.Hoodie => 250000m,
                GarmentType.Sweatshirt => 200000m,
                GarmentType.TankTop => 120000m,
                GarmentType.LongSleeve => 180000m,
                GarmentType.Jacket => 300000m,
                _ => 150000m
            };

            // Size premium
            decimal sizePremium = designDto.Size switch
            {
                TShirtSize.XXL => 20000m,
                TShirtSize.XXXL => 30000m,
                _ => 0m
            };

            // Customization fee
            decimal customizationFee = 50000m;

            // Logo fee
            decimal logoFee = !string.IsNullOrEmpty(designDto.LogoText) ? 30000m : 0m;

            // Quantity discount
            decimal quantityDiscount = designDto.Quantity switch
            {
                >= 10 => 0.15m,
                >= 5 => 0.10m,
                >= 3 => 0.05m,
                _ => 0m
            };

            decimal totalBeforeDiscount = (basePrice + sizePremium + customizationFee + logoFee) * designDto.Quantity;
            decimal discount = totalBeforeDiscount * quantityDiscount;

            return await Task.FromResult(totalBeforeDiscount - discount);
        }

        public async Task<bool> AssignStaffToDesignAsync(Guid designId, Guid staffId)
        {
            var design = await GetByIdAsync(designId);
            if (design == null)
                return false;

            design.StaffId = staffId;
            await UpdateAsync(design);
            return true;
        }
    }
}