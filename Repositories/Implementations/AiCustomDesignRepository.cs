using BusinessObjects.Common;
using BusinessObjects.CustomDesigns;
using Data.Repositories.CustomDesigns; // Interface
using DTOs.CustomDesigns;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class AiCustomDesignRepository : IAICustomDesignRepository
    {
        protected readonly T_ShirtAIcommerceContext _context;

        public AiCustomDesignRepository(T_ShirtAIcommerceContext context)
        {
            _context = context;
        }

        // ======== INTERFACE ========

        public async Task<PagedList<CustomDesign>> GetCustomDesignsAsync(CustomDesignFilterRequest filter)
        {
            var query = _context.CustomDesigns
                .Where(d => !d.IsDeleted)
                .AsQueryable();

            // Lọc theo UserId
            //if (filter.UserId.HasValue)
            //    query = query.Where(d => d.UserId == filter.UserId.Value);

            // Lọc theo Status
            if (filter.Status.HasValue)
                query = query.Where(d => d.Status == filter.Status.Value);

            // Tìm kiếm theo tên hoặc prompt
            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(d => d.DesignName.Contains(filter.Search) || (d.PromptText != null && d.PromptText.Contains(filter.Search)));

            // Sắp xếp
            query = filter.SortBy?.ToLower() switch
            {
                "designname" => filter.SortDescending
                    ? query.OrderByDescending(d => d.DesignName)
                    : query.OrderBy(d => d.DesignName),
                "createdat" => filter.SortDescending
                    ? query.OrderByDescending(d => d.CreatedAt)
                    : query.OrderBy(d => d.CreatedAt),
                "status" => filter.SortDescending
                    ? query.OrderByDescending(d => d.Status)
                    : query.OrderBy(d => d.Status),
                _ => query.OrderByDescending(d => d.CreatedAt)
            };


            // Lấy luôn User nếu cần hiển thị thông tin user
            query = query.Include(d => d.User);

            // Phân trang (dùng PagedList nếu đã có class này, như bên Order)
            return await PagedList<CustomDesign>.ToPagedListAsync(query, filter.PageNumber, filter.PageSize);
        }
        public async Task<CustomDesign> CreateAsync(CustomDesign entity)
        {
            _context.CustomDesigns.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        public async Task<PagedList<CustomDesign>> GetCustomDesignsByIDAsync(CustomDesignFilterRequest filter)
        {
            var query = _context.CustomDesigns
                .Where(d => !d.IsDeleted)
                .AsQueryable();

            // Lọc theo UserId
            if (filter.UserId.HasValue)
                query = query.Where(d => d.UserId == filter.UserId.Value);

            // Lọc theo Status
            if (filter.Status.HasValue)
                query = query.Where(d => d.Status == filter.Status.Value);

            // Tìm kiếm theo tên hoặc prompt
            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(d => d.DesignName.Contains(filter.Search) || (d.PromptText != null && d.PromptText.Contains(filter.Search)));

            // Sắp xếp
            query = filter.SortBy?.ToLower() switch
            {
                "designname" => filter.SortDescending
                    ? query.OrderByDescending(d => d.DesignName)
                    : query.OrderBy(d => d.DesignName),
                "createdat" => filter.SortDescending
                    ? query.OrderByDescending(d => d.CreatedAt)
                    : query.OrderBy(d => d.CreatedAt),
                "status" => filter.SortDescending
                    ? query.OrderByDescending(d => d.Status)
                    : query.OrderBy(d => d.Status),
                _ => query.OrderByDescending(d => d.CreatedAt)
            };


            // Lấy luôn User nếu cần hiển thị thông tin user
            query = query.Include(d => d.User);

            // Phân trang (dùng PagedList nếu đã có class này, như bên Order)
            return await PagedList<CustomDesign>.ToPagedListAsync(query, filter.PageNumber, filter.PageSize);
        }
        public async Task<CustomDesign?> GetByIdAsync(Guid id)
        {
            return await _context.CustomDesigns
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        }

        public async Task<IEnumerable<CustomDesign>> GetByUserIdAsync(Guid userId)
        {
            return await _context.CustomDesigns
                .Where(d => d.UserId == userId && !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(CustomDesign entity)
        {
            _context.CustomDesigns.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.CustomDesigns.FindAsync(id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ShowAsync(Guid id)
        {
            var entity = await _context.CustomDesigns.FindAsync(id);
            if (entity != null)
            {
                entity.IsDeleted = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task HideAsync(Guid id)
        {
            var entity = await _context.CustomDesigns.FindAsync(id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }
        public async Task<bool> UpdateStatusAsync(Guid id, CustomDesignStatus status)
        {
            var entity = await _context.CustomDesigns.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (entity == null) return false;

            entity.Status = status;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
