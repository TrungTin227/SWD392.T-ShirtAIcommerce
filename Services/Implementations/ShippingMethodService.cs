using BusinessObjects.Shipping;
using DTOs.Common;
using DTOs.Shipping;
using Microsoft.Extensions.Logging;
using Repositories.Commons;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Extensions;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ShippingMethodService : BaseService<ShippingMethod, Guid>, IShippingMethodService
    {
        private readonly IShippingMethodRepository _shippingMethodRepository;
        private readonly ILogger<ShippingMethodService> _logger;

        public ShippingMethodService(
            IGenericRepository<ShippingMethod, Guid> repository,
            IShippingMethodRepository shippingMethodRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            ILogger<ShippingMethodService> logger)
            : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _shippingMethodRepository = shippingMethodRepository;
            _logger = logger;
        }

        public async Task<PagedResponse<ShippingMethodDTO>> GetShippingMethodsAsync(ShippingMethodFilterRequest filter)
        {
            try
            {
                var pagedShippingMethods = await _shippingMethodRepository.GetShippingMethodsAsync(filter);
                return pagedShippingMethods
                    .ToPagedResponse()
                    .WithSuccessMessage("Lấy danh sách phương thức vận chuyển thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping methods with filter {@Filter}", filter);
                return new PagedResponse<ShippingMethodDTO>()
                    .WithErrorMessage("Có lỗi xảy ra khi lấy danh sách phương thức vận chuyển");
            }
        }

        public async Task<ShippingMethodDTO?> GetShippingMethodByIdAsync(Guid id)
        {
            try
            {
                var shippingMethod = await _shippingMethodRepository.GetShippingMethodWithDetailsAsync(id);
                return shippingMethod?.ToDTO();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping method by id {Id}", id);
                return null;
            }
        }

        public async Task<ShippingMethodDTO?> CreateShippingMethodAsync(CreateShippingMethodRequest request)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Validate unique name
                if (await _shippingMethodRepository.IsNameExistsAsync(request.Name))
                {
                    _logger.LogWarning("Shipping method with name {Name} already exists", request.Name);
                    return null;
                }

                // Validate delivery days range
                if (request.MinDeliveryDays.HasValue && request.MaxDeliveryDays.HasValue &&
                    request.MinDeliveryDays.Value > request.MaxDeliveryDays.Value)
                {
                    _logger.LogWarning("Min delivery days cannot be greater than max delivery days");
                    return null;
                }

                var shippingMethod = request.ToEntity();
                var createdShippingMethod = await CreateAsync(shippingMethod);

                await _unitOfWork.CommitTransactionAsync();

                return createdShippingMethod.ToDTO();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating shipping method {@Request}", request);
                return null;
            }
        }

        public async Task<ShippingMethodDTO?> UpdateShippingMethodAsync(Guid id, UpdateShippingMethodRequest request)
        {
            // Bắt đầu transaction
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Lấy bản ghi hiện tại
                var existing = await _shippingMethodRepository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Shipping method with id {Id} not found", id);
                    return null;
                }

                // 2. Kiểm tra unique name (enum) nếu có thay đổi
                if (request.Name != existing.Name
                    && await _shippingMethodRepository.IsNameExistsAsync(request.Name.ToString(), id))
                {
                    _logger.LogWarning("Shipping method with name {Name} already exists", request.Name);
                    return null;
                }

                // 3. Cập nhật các field còn lại
                existing.UpdateFromRequest(request);

                // 4. Validate business rule về delivery days
                if (!existing.IsDeliveryDaysValid())
                {
                    _logger.LogWarning("Min delivery days cannot be greater than max delivery days for id {Id}", id);
                    return null;
                }

                // 5. Áp dụng thay đổi và commit
                var updated = await UpdateAsync(existing);
                await _unitOfWork.CommitTransactionAsync();

                // 6. Trả về DTO
                return updated.ToDTO();
            }
            catch (Exception ex)
            {
                // Rollback nếu có lỗi
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating shipping method {Id} with {@Request}", id, request);
                return null;
            }
        }


        public async Task<bool> DeleteShippingMethodAsync(Guid id)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Check if shipping method is used in orders
                if (await _shippingMethodRepository.IsShippingMethodUsedInOrdersAsync(id))
                {
                    _logger.LogWarning("Cannot delete shipping method {Id} because it's used in orders", id);
                    return false;
                }

                var result = await DeleteAsync(id);

                if (result)
                {
                    await _unitOfWork.CommitTransactionAsync();
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting shipping method {Id}", id);
                return false;
            }
        }

        public async Task<IEnumerable<ShippingMethodDTO>> GetActiveShippingMethodsAsync()
        {
            try
            {
                var activeShippingMethods = await _shippingMethodRepository.GetActiveShippingMethodsAsync();
                return activeShippingMethods.ToDTOs();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active shipping methods");
                return new List<ShippingMethodDTO>();
            }
        }

        public async Task<bool> ToggleActiveStatusAsync(Guid id, bool isActive)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var result = await _shippingMethodRepository.ToggleActiveStatusAsync(id, isActive, currentUserId);

                if (result)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error toggling active status for shipping method {Id}", id);
                return false;
            }
        }

        public async Task<bool> UpdateSortOrderAsync(Guid id, int newSortOrder)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var result = await _shippingMethodRepository.UpdateSortOrderAsync(id, newSortOrder);

                if (result)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating sort order for shipping method {Id}", id);
                return false;
            }
        }

        public async Task<decimal> CalculateShippingFeeAsync(Guid shippingMethodId, decimal orderAmount)
        {
            try
            {
                return await _shippingMethodRepository.CalculateShippingFeeAsync(shippingMethodId, orderAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping fee for method {Id} with amount {Amount}",
                    shippingMethodId, orderAmount);
                return 0;
            }
        }

        public async Task<ApiResult<string>> ValidateShippingMethodAsync(Guid id)
        {
            try
            {
                var shippingMethod = await _shippingMethodRepository.GetByIdAsync(id);

                if (shippingMethod == null)
                    return ApiResult<string>.Failure("Phương thức vận chuyển không tồn tại");

                if (!shippingMethod.IsActive)
                    return ApiResult<string>.Failure("Phương thức vận chuyển đã bị vô hiệu hóa");

                return ApiResult<string>.Success("Phương thức vận chuyển hợp lệ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating shipping method {Id}", id);
                return ApiResult<string>.Failure("Có lỗi xảy ra khi kiểm tra phương thức vận chuyển");
            }
        }

        public async Task<bool> BulkUpdateSortOrderAsync(Dictionary<Guid, int> sortOrders)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var kvp in sortOrders)
                {
                    await _shippingMethodRepository.UpdateSortOrderAsync(kvp.Key, kvp.Value);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error bulk updating sort orders {@SortOrders}", sortOrders);
                return false;
            }
        }

        public async Task<bool> BulkToggleActiveStatusAsync(List<Guid> ids, bool isActive)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var currentUserId = _currentUserService.GetUserId();

                foreach (var id in ids)
                {
                    await _shippingMethodRepository.ToggleActiveStatusAsync(id, isActive, currentUserId);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error bulk toggling active status for IDs {@Ids}", ids);
                return false;
            }
        }
        public async Task<ApiResult<ShippingMethodDTO>> GetByIdAsync(Guid id)
        {
            try
            {
                var shippingMethod = await GetShippingMethodByIdAsync(id);
                if (shippingMethod == null)
                {
                    return ApiResult<ShippingMethodDTO>.Failure("Không tìm thấy phương thức vận chuyển");
                }

                return ApiResult<ShippingMethodDTO>.Success(shippingMethod);
            }
            catch (Exception ex)
            {
                return ApiResult<ShippingMethodDTO>.Failure("Lỗi khi lấy thông tin phương thức vận chuyển", ex);
            }
        }
    }
}