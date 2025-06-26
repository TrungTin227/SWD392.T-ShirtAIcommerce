using BusinessObjects.Identity;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface IUserAddressRepository : IGenericRepository<UserAddress, Guid>
    {
        Task<IEnumerable<UserAddress>> GetUserAddressesAsync(Guid userId);
        Task<UserAddress?> GetDefaultAddressAsync(Guid userId);
        Task<bool> SetDefaultAddressAsync(Guid userId, Guid addressId);
        Task<bool> RemoveDefaultAddressesAsync(Guid userId);
        Task<bool> HasAddressesAsync(Guid userId);
        Task<int> GetUserAddressCountAsync(Guid userId);
    }
}