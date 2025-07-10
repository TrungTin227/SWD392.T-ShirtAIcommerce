using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using System.Data;

namespace Repositories.WorkSeeds.Implements
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly T_ShirtAIcommerceContext _context;
        private readonly IRepositoryFactory _repositoryFactory;
        private IDbContextTransaction? _transaction;

        // Specific repositories
        private IUserRepository? _userRepository;
        private IOrderRepository? _orderRepository;
        private ICouponRepository? _couponRepository;
        private IOrderItemRepository? _orderItemRepository;
        private IUserAddressRepository? _userAddressRepository;
        private IShippingMethodRepository? _shippingMethodRepository;
        private ICartItemRepository? _cartItemRepository;
        private ICategoryRepository? _categoryRepository;
        private IProductRepository? _productRepository;
        private IUserCouponRepository? _userCouponRepository;


        public UnitOfWork(T_ShirtAIcommerceContext context, IRepositoryFactory repositoryFactory)
        {
            _context = context;
            _repositoryFactory = repositoryFactory;
        }

        // Add this property to implement the interface
        public T_ShirtAIcommerceContext Context => _context;

        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : class
        {
            return _repositoryFactory.GetRepository<TEntity, TKey>();
        }

        public IUserRepository UserRepository =>
            _userRepository ??= _repositoryFactory.GetCustomRepository<IUserRepository>();

        public IOrderRepository OrderRepository => 
            _orderRepository ??= _repositoryFactory.GetCustomRepository<IOrderRepository>();
            
        public ICouponRepository CouponRepository => 
            _couponRepository ??= _repositoryFactory.GetCustomRepository<ICouponRepository>();

        public IOrderItemRepository OrderItemRepository => _orderItemRepository ??= _repositoryFactory.GetCustomRepository<IOrderItemRepository>();
        public IUserAddressRepository UserAddressRepository =>  _userAddressRepository ??= _repositoryFactory.GetCustomRepository<IUserAddressRepository>();
        public IShippingMethodRepository ShippingMethodRepository => _shippingMethodRepository ??= _repositoryFactory.GetCustomRepository<IShippingMethodRepository>();
        public ICartItemRepository CartItemRepository => _cartItemRepository ??= _repositoryFactory.GetCustomRepository<ICartItemRepository>();
        public ICategoryRepository CategoryRepository =>
          _categoryRepository ??= _repositoryFactory.GetCustomRepository<ICategoryRepository>();
        public IProductRepository ProductRepository =>
         _productRepository ??= _repositoryFactory.GetCustomRepository<IProductRepository>();
        public IUserCouponRepository UserCouponRepository => 
            _userCouponRepository ??= _repositoryFactory.GetCustomRepository<IUserCouponRepository>();
        public bool HasActiveTransaction => _transaction != null;

        public async Task<IDbContextTransaction> BeginTransactionAsync(
    IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
    CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
                throw new InvalidOperationException("A transaction is already active.");

            _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return _transaction;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("No active transaction to commit.");

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("No active transaction to rollback.");

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();

            if (_context != null)
                await _context.DisposeAsync();
        }
    }
}