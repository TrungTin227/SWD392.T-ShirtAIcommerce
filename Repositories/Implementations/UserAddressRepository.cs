using BusinessObjects.Identity;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class UserAddressRepository : GenericRepository<UserAddress, Guid>, IUserAddressRepository
    {
        public UserAddressRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<IEnumerable<UserAddress>> GetUserAddressesAsync(Guid userId)
        {
            return await _dbSet
                .Where(ua => ua.UserId == userId)
                .OrderByDescending(ua => ua.IsDefault)
                .ThenByDescending(ua => ua.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserAddress?> GetDefaultAddressAsync(Guid userId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.IsDefault);
        }

        public async Task<bool> SetDefaultAddressAsync(Guid userId, Guid addressId)
        {
            // Bỏ default của tất cả địa chỉ khác
            await RemoveDefaultAddressesAsync(userId);

            // Set địa chỉ mới làm default
            var address = await _dbSet.FirstOrDefaultAsync(ua => ua.Id == addressId && ua.UserId == userId);
            if (address == null) return false;

            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public async Task<bool> RemoveDefaultAddressesAsync(Guid userId)
        {
            var defaultAddresses = await _dbSet
                .Where(ua => ua.UserId == userId && ua.IsDefault)
                .ToListAsync();

            foreach (var address in defaultAddresses)
            {
                address.IsDefault = false;
                address.UpdatedAt = DateTime.UtcNow;
            }

            return defaultAddresses.Any();
        }

        public async Task<bool> HasAddressesAsync(Guid userId)
        {
            return await _dbSet.AnyAsync(ua => ua.UserId == userId);
        }

        public async Task<int> GetUserAddressCountAsync(Guid userId)
        {
            return await _dbSet.CountAsync(ua => ua.UserId == userId);
        }
    }
}