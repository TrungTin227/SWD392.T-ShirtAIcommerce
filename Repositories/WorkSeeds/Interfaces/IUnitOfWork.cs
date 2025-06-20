using Repositories.Interfaces;

namespace Repositories.WorkSeeds.Interfaces
{

    public interface IUnitOfWork : IGenericUnitOfWork
    {
        IUserRepository UserRepository { get; }
    }
}
