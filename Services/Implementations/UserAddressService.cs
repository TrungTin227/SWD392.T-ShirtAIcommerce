﻿using DTOs.UserAddressDTOs.Request;
using DTOs.UserAddressDTOs.Response;
using Microsoft.Extensions.Logging;
using Repositories.Commons;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Extensions;
using Services.Interfaces;

namespace Services.Implementations
{
    public class UserAddressService : IUserAddressService
    {
        private readonly IUserAddressRepository _userAddressRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserAddressService> _logger;

        public UserAddressService(
            IUserAddressRepository userAddressRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<UserAddressService> logger)
        {
            _userAddressRepository = userAddressRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResult<UserAddressResponse>> CreateUserAddressAsync(CreateUserAddressRequest request)
        {
            if (request == null)
                return ApiResult<UserAddressResponse>.Failure("Invalid request");

            var currentUserId = _currentUserService.GetUserId();
            if (currentUserId == null)
                return ApiResult<UserAddressResponse>.Failure("User not authenticated");

            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                // Nếu là địa chỉ default, bỏ default của tất cả địa chỉ khác
                if (request.IsDefault)
                {
                    await _userAddressRepository.RemoveDefaultAddressesAsync(currentUserId.Value);
                }

                // Nếu chưa có địa chỉ nào, tự động set làm default
                var hasExistingAddresses = await _userAddressRepository.HasAddressesAsync(currentUserId.Value);
                if (!hasExistingAddresses)
                {
                    request.IsDefault = true;
                }

                var userAddress = request.ToUserAddress(currentUserId.Value);
                await _userAddressRepository.AddAsync(userAddress);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created user address {AddressId} for user {UserId}", userAddress.Id, currentUserId.Value);

                return ApiResult<UserAddressResponse>.Success(userAddress.ToUserAddressResponse());
            });
        }

        public async Task<ApiResult<UserAddressResponse>> UpdateUserAddressAsync(Guid addressId, UpdateUserAddressRequest request)
        {
            if (request == null)
                return ApiResult<UserAddressResponse>.Failure("Invalid request");

            var currentUserId = _currentUserService.GetUserId();
            if (currentUserId == null)
                return ApiResult<UserAddressResponse>.Failure("User not authenticated");

            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var existingAddress = await _userAddressRepository.FirstOrDefaultAsync(
                    ua => ua.Id == addressId && ua.UserId == currentUserId.Value);

                if (existingAddress == null)
                    return ApiResult<UserAddressResponse>.Failure("Address not found");

                // Nếu set làm default, bỏ default của tất cả địa chỉ khác
                if (request.IsDefault && !existingAddress.IsDefault)
                {
                    await _userAddressRepository.RemoveDefaultAddressesAsync(currentUserId.Value);
                }

                request.ApplyToUserAddress(existingAddress);
                await _userAddressRepository.UpdateAsync(existingAddress);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated user address {AddressId} for user {UserId}", addressId, currentUserId.Value);

                return ApiResult<UserAddressResponse>.Success(existingAddress.ToUserAddressResponse());
            });
        }

        public async Task<ApiResult<bool>> DeleteUserAddressAsync(Guid addressId)
        {
            var userId = _currentUserService.GetUserId();
            if (userId == null)
                return ApiResult<bool>.Failure("User not authenticated");

            // Bắt đầu transaction
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var address = await _userAddressRepository
                    .FirstOrDefaultAsync(ua => ua.Id == addressId && ua.UserId == userId.Value);

                if (address == null)
                    return ApiResult<bool>.Failure("Address not found");

                bool wasDefault = address.IsDefault;
                await _userAddressRepository.DeleteAsync(addressId);

                if (wasDefault)
                {
                    var others = await _userAddressRepository
                        .GetAllAsync(ua => ua.UserId == userId.Value && ua.Id != addressId);

                    if (others.Any())
                    {
                        var newDefault = others.First();
                        newDefault.IsDefault = true;
                        await _userAddressRepository.UpdateAsync(newDefault);
                    }
                }

                // Chỉ gọi SaveChanges một lần
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Deleted address {AddressId} of user {UserId}", addressId, userId.Value);
                return ApiResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting address {AddressId}", addressId);
                return ApiResult<bool>.Failure("Internal server error: " + ex.Message);
            }
        }
        public async Task<ApiResult<IEnumerable<UserAddressResponse>>> GetUserAddressesAsync()
        {
            var currentUserId = _currentUserService.GetUserId();
            if (currentUserId == null)
                return ApiResult<IEnumerable<UserAddressResponse>>.Failure("User not authenticated");

            var addresses = await _userAddressRepository.GetUserAddressesAsync(currentUserId.Value);
            var responses = addresses.Select(a => a.ToUserAddressResponse());

            return ApiResult<IEnumerable<UserAddressResponse>>.Success(responses);
        }

        public async Task<ApiResult<UserAddressResponse>> GetUserAddressByIdAsync(Guid addressId)
        {
            var currentUserId = _currentUserService.GetUserId();
            if (currentUserId == null)
                return ApiResult<UserAddressResponse>.Failure("User not authenticated");

            var address = await _userAddressRepository.FirstOrDefaultAsync(
                ua => ua.Id == addressId && ua.UserId == currentUserId.Value);

            if (address == null)
                return ApiResult<UserAddressResponse>.Failure("Address not found");

            return ApiResult<UserAddressResponse>.Success(address.ToUserAddressResponse());
        }

        public async Task<ApiResult<UserAddressResponse>> GetDefaultAddressAsync()
        {
            var currentUserId = _currentUserService.GetUserId();
            if (currentUserId == null)
                return ApiResult<UserAddressResponse>.Failure("User not authenticated");

            var defaultAddress = await _userAddressRepository.GetDefaultAddressAsync(currentUserId.Value);

            if (defaultAddress == null)
                return ApiResult<UserAddressResponse>.Failure("No default address found");

            return ApiResult<UserAddressResponse>.Success(defaultAddress.ToUserAddressResponse());
        }

        public async Task<ApiResult<bool>> SetDefaultAddressAsync(Guid addressId)
        {
            var currentUserId = _currentUserService.GetUserId();
            if (currentUserId == null)
                return ApiResult<bool>.Failure("User not authenticated");

            try
            {
                using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();

                var success = await _userAddressRepository.SetDefaultAddressAsync(currentUserId.Value, addressId);
                if (!success)
                    return ApiResult<bool>.Failure("Address not found or doesn't belong to user");

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Set default address {AddressId} for user {UserId}", addressId, currentUserId.Value);

                return ApiResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default address {AddressId} for user {UserId}", addressId, currentUserId.Value);
                return ApiResult<bool>.Failure("An error occurred while setting default address");
            }
        }
        public async Task<ApiResult<UserAddressResponse>> CreateDefaultAddressForNewUserAsync(Guid userId, CreateUserAddressRequest? request = null)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                // Tạo địa chỉ mặc định cho user mới
                var defaultRequest = request ?? new CreateUserAddressRequest
                {
                    ReceiverName = "Địa chỉ mặc định",
                    Phone = "",
                    DetailAddress = "",
                    Ward = "",
                    District = "",
                    Province = "",
                    IsDefault = true
                };

                var userAddress = defaultRequest.ToUserAddress(userId);
                await _userAddressRepository.AddAsync(userAddress);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created default address for new user {UserId}", userId);

                return ApiResult<UserAddressResponse>.Success(userAddress.ToUserAddressResponse());
            });
        }
    }
}