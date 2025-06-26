using Microsoft.Extensions.DependencyInjection;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.WorkSeeds.Implements
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly T_ShirtAIcommerceContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, object> _repositories = new();

        public RepositoryFactory(T_ShirtAIcommerceContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : class
        {
            var type = typeof(IGenericRepository<TEntity, TKey>);
            if (_repositories.ContainsKey(type))
            {
                return (IGenericRepository<TEntity, TKey>)_repositories[type];
            }

            var repository = new GenericRepository<TEntity, TKey>(_context);
            _repositories.Add(type, repository);
            return repository;
        }

        public TRepository GetCustomRepository<TRepository>()
            where TRepository : class
        {
            var type = typeof(TRepository);
            if (_repositories.ContainsKey(type))
            {
                return (TRepository)_repositories[type];
            }

            // Use DI to resolve custom repository
            var repository = _serviceProvider.GetRequiredService<TRepository>();
            _repositories.Add(type, repository);
            return repository;
        }
    }
}