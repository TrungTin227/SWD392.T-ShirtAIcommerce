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
            var addresses = await _dbSet.Where(ua => ua.UserId == userId).ToListAsync();
            var addressToSetDefault = addresses.FirstOrDefault(ua => ua.Id == addressId);

            if (addressToSetDefault == null)
                return false;

            foreach (var addr in addresses)
            {
                bool shouldBeDefault = addr.Id == addressId;
                if (addr.IsDefault != shouldBeDefault)
                {
                    addr.IsDefault = shouldBeDefault;
                    addr.UpdatedAt = DateTime.UtcNow;

                    // Mark entity as modified
                    _context.Entry(addr).State = EntityState.Modified;
                }
            }

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