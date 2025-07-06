using Azure;
using BusinessObjects.Identity;
using DTOs.UserAddressDTOs.Request;
using DTOs.UserAddressDTOs.Response;
using DTOs.UserDTOs.Identities;
using DTOs.UserDTOs.Request;
using DTOs.UserDTOs.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Extensions;
using Services.Helpers.Mappers;
using Services.Interfaces;
using Services.Interfaces.Services.Commons.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserDTOs.DTOs.Response;

namespace Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;
        private readonly IUserEmailService _userEmailService;
        private readonly string _confirmEmailUri;
        private readonly string _resetPasswordUri;
        private readonly IUserRepository _userRepository;
        private readonly IUserAddressService _userAddressService;


        public UserService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<UserService> logger,
            IUserEmailService userEmailService,
            IConfiguration configuration,
            IUserRepository userRepository,
            IUserAddressService userAddressService)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userEmailService = userEmailService ?? throw new ArgumentNullException(nameof(userEmailService));
            _confirmEmailUri = configuration["Frontend:ConfirmEmailUri"] ?? throw new ArgumentNullException(nameof(configuration), "ConfirmEmailUri is missing");
            _resetPasswordUri = configuration["Frontend:ResetPasswordUri"] ?? throw new ArgumentNullException(nameof(configuration), "ResetPasswordUri is missing");
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userAddressService = userAddressService;
        }

        public async Task<ApiResult<UserResponse>> RegisterAsync(UserRegisterRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email))
                return ApiResult<UserResponse>.Failure("Invalid request");

            if (await _userRepository.ExistsByEmailAsync(req.Email))
                return ApiResult<UserResponse>.Failure("Email already in use");

            var result = await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var user = UserMappings.ToDomainUser(req);
                var createRes = await _userManager.CreateUserAsync(user, req.Password);
                if (!createRes.Succeeded)
                    return ApiResult<UserResponse>.Failure(createRes.ErrorMessage);

                await _userManager.AddDefaultRoleAsync(user);

                // Tạo địa chỉ mặc định cho user mới
                var defaultAddressRequest = new CreateUserAddressRequest
                {
                    ReceiverName = $"{user.FirstName} {user.LastName}".Trim(),
                    Phone = user.PhoneNumber ?? "",
                    DetailAddress = "Chưa cập nhật",
                    Ward = "Chưa cập nhật",
                    District = "Chưa cập nhật",
                    Province = "Chưa cập nhật",
                    IsDefault = true
                };

                await _userAddressService.CreateDefaultAddressForNewUserAsync(user.Id, defaultAddressRequest);

                return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(user, _userManager));
            });

            if (result.IsSuccess)
            {
                try
                {
                    await SendWelcomeEmailsAsync(req.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome emails for {Email}", req.Email);
                }
            }
            return result;
        }

        public async Task<ApiResult<UpdateUserResponse>> UpdateProfileAsync(Guid userId, UpdateUserRequest request)
        {
            // 1. Lấy user
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ApiResult<UpdateUserResponse>.Failure("User not found");

            // 2. Cập nhật thông tin cơ bản
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Gender = request.Gender;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = userId;

            var userUpdate = await _userManager.UpdateAsync(user);
            if (!userUpdate.Succeeded)
                return ApiResult<UpdateUserResponse>.Failure(userUpdate.Errors.First().Description);

            // 3. Xử lý addresses
            var seenAddressIds = new HashSet<Guid>();
            foreach (var addrDto in request.Addresses)
            {
                if (addrDto.Id == Guid.Empty)
                {
                    var createResult = await _userAddressService.CreateUserAddressAsync(
                        new CreateUserAddressRequest
                        {
                            ReceiverName = addrDto.ReceiverName,
                            Phone = addrDto.Phone,
                            DetailAddress = addrDto.DetailAddress,
                            Ward = addrDto.Ward,
                            District = addrDto.District,
                            Province = addrDto.Province,
                            IsDefault = addrDto.IsDefault
                        });
                    if (createResult.IsSuccess)
                        seenAddressIds.Add(createResult.Data.Id);
                    else
                        _logger.LogWarning("Failed to create address: {Message}", createResult.Message);
                }
                else
                {
                    var updateResult = await _userAddressService.UpdateUserAddressAsync(
                        addrDto.Id,
                        new UpdateUserAddressRequest
                        {
                            Id = addrDto.Id,
                            ReceiverName = addrDto.ReceiverName,
                            Phone = addrDto.Phone,
                            DetailAddress = addrDto.DetailAddress,
                            Ward = addrDto.Ward,
                            District = addrDto.District,
                            Province = addrDto.Province,
                            IsDefault = addrDto.IsDefault
                        });
                    if (updateResult.IsSuccess)
                        seenAddressIds.Add(addrDto.Id);
                    else
                        _logger.LogWarning("Failed to update address {AddressId}: {Message}", addrDto.Id, updateResult.Message);
                }
            }

            // 4. Xóa địa chỉ không còn trong request
            var allAddressesResult = await _userAddressService.GetUserAddressesAsync();
            var finalAddresses = new List<UserAddressResponse>();
            if (allAddressesResult.IsSuccess)
            {
                foreach (var addr in allAddressesResult.Data)
                {
                    if (!seenAddressIds.Contains(addr.Id))
                    {
                        var delResult = await _userAddressService.DeleteUserAddressAsync(addr.Id);
                        if (!delResult.IsSuccess)
                            _logger.LogWarning("Failed to delete address {AddressId}: {Message}", addr.Id, delResult.Message);
                    }
                    else
                    {
                        finalAddresses.Add(addr);
                    }
                }
            }

            // 5. Chuẩn bị UserDetailsDTO
            var roles = await _userManager.GetRolesAsync(user);
            var updatedUserDetailsDto = new UserDetailsDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                Gender = user.Gender.ToString(),
                CreateAt = user.CreatedAt,
                UpdateAt = user.UpdatedAt,
                Roles = roles.ToList(),
                IsActive = !user.LockoutEnabled
            };

            // 6. Trả về response
            var response = new UpdateUserResponse
            {
                User = updatedUserDetailsDto,
                Addresses = finalAddresses
            };

            return ApiResult<UpdateUserResponse>.Success(response);
        }
        private async Task SendWelcomeEmailsAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return;

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            await _userEmailService.SendWelcomeEmailAsync(email);
            await _userEmailService.SendEmailConfirmationAsync(email, user.Id, token, _confirmEmailUri);
        }

        public async Task<ApiResult<string>> ConfirmEmailAsync(Guid userId, string encodedToken)
        {
            if (string.IsNullOrWhiteSpace(encodedToken))
                return ApiResult<string>.Failure("Invalid token");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ApiResult<string>.Failure("User not found");

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode token for user {UserId}", userId);
                return ApiResult<string>.Failure("Invalid token format");
            }

            var res = await _userManager.ConfirmEmailAsync(user, token);
            if (!res.Succeeded)
            {
                _logger.LogWarning("Email confirmation failed for user {UserId}. Errors: {Errors}",
                    userId, string.Join(", ", res.Errors.Select(e => e.Description)));
                return ApiResult<string>.Failure("Email confirmation failed: " + string.Join(", ", res.Errors.Select(e => e.Description)));
            }

            return ApiResult<string>.Success("Email confirmed successfully");
        }

        public async Task<ApiResult<string>> ResendConfirmationEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ApiResult<string>.Failure("Invalid email");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return ApiResult<string>.Success("If the email is valid and unconfirmed, a confirmation email will be sent");

            if (await _userManager.IsEmailConfirmedAsync(user))
                return ApiResult<string>.Success("Email is already confirmed");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            await _userEmailService.SendEmailConfirmationAsync(email, user.Id, token, _confirmEmailUri);
            return ApiResult<string>.Success("Confirmation email resent");
        }

        public async Task<ApiResult<string>> InitiatePasswordResetAsync(ForgotPasswordRequestDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return ApiResult<string>.Failure("Invalid email");

            var genericResponse = ApiResult<string>.Success("If the email is valid, you'll receive password reset instructions");
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                return genericResponse;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            await _userEmailService.SendPasswordResetEmailAsync(request.Email, encodedToken, _resetPasswordUri);
            return genericResponse;
        }

        public async Task<ApiResult<string>> ResetPasswordAsync(ResetPasswordRequestDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
                return ApiResult<string>.Failure("Invalid request");

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return ApiResult<string>.Failure("Invalid request");

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            }
            catch
            {
                return ApiResult<string>.Failure("Invalid token");
            }

            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!result.Succeeded)
                return ApiResult<string>.Failure("Password reset failed. Please try again");

            await _userEmailService.SendPasswordChangedNotificationAsync(request.Email);
            return ApiResult<string>.Success("Password reset successfully");
        }

        public async Task<ApiResult<string>> Send2FACodeAsync()
        {
            var userId = _currentUserService.GetUserId();
            if (userId == null)
                return ApiResult<string>.Failure("User not found");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ApiResult<string>.Failure("User not found");

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            await _userEmailService.Send2FACodeAsync(user.Email, code);
            return ApiResult<string>.Success("2FA code sent");
        }

        public async Task<ApiResult<UserResponse>> AdminRegisterAsync(AdminCreateUserRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email))
                return ApiResult<UserResponse>.Failure("Invalid request");

            if (!_currentUserService.IsAdmin())
                return ApiResult<UserResponse>.Failure("Forbidden: Only admins can register users");

            if (await _userRepository.ExistsByEmailAsync(req.Email))
                return ApiResult<UserResponse>.Failure("Email already in use");

            _logger.LogInformation("Admin registering user: {Email}", req.Email);

            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var user = UserMappings.ToDomainUser(req);
                var create = await _userManager.CreateUserAsync(user, req.Password);
                if (!create.Succeeded)
                    return ApiResult<UserResponse>.Failure(create.ErrorMessage);

                await _userManager.AddRolesAsync(user, req.Roles);
                var token = await _tokenService.GenerateToken(user);
                return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(user, _userManager, token.Data));
            });
        }

        public async Task<ApiResult<UserResponse>> LoginAsync(UserLoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return ApiResult<UserResponse>.Failure("Invalid request");

            _logger.LogInformation("Login attempt: {Email}", req.Email);

            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, req.Password))
            {
                if (user != null) await _userManager.AccessFailedAsync(user);
                return ApiResult<UserResponse>.Failure("Invalid email or password");
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
                return ApiResult<UserResponse>.Failure("Please confirm your email before logging in");

            if (await _userManager.IsLockedOutAsync(user))
                return ApiResult<UserResponse>.Failure("Account is locked");

            await _userManager.ResetAccessFailedAsync(user);

            var token = await _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            await _userManager.SetAuthenticationTokenAsync(user, "T-ShirtAIcommerce", "RefreshToken", refreshToken.Token);

            return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(user, _userManager, token.Data, refreshToken.Token));
        }

        public async Task<ApiResult<UserResponse>> GetByIdAsync(Guid id)
        {
            var userDetails = await _userRepository.GetUserDetailsByIdAsync(id);
            if (userDetails == null)
                return ApiResult<UserResponse>.Failure("User not found");

            return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(userDetails, _userManager));
        }

        public async Task<ApiResult<CurrentUserResponse>> GetCurrentUserAsync()
        {
            var uid = _currentUserService.GetUserId();
            if (uid == null)
                return ApiResult<CurrentUserResponse>.Failure("User not found");

            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user == null)
                return ApiResult<CurrentUserResponse>.Failure("User not found");

            var token = await _tokenService.GenerateToken(user);
            return ApiResult<CurrentUserResponse>.Success(UserMappings.ToCurrentUserResponse(user, token.Data));
        }

        public async Task<ApiResult<CurrentUserResponse>> RefreshTokenAsync(RefreshTokenRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RefreshToken))
                return ApiResult<CurrentUserResponse>.Failure("Invalid request");

            var uid = _currentUserService.GetUserId();
            if (uid == null)
                return ApiResult<CurrentUserResponse>.Failure("User not found");

            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user == null || !await _userManager.ValidateRefreshTokenAsync(user, req.RefreshToken))
                return ApiResult<CurrentUserResponse>.Failure("Invalid refresh token");

            var token = await _tokenService.GenerateToken(user);
            return ApiResult<CurrentUserResponse>.Success(UserMappings.ToCurrentUserResponse(user, token.Data));
        }

        public async Task<ApiResult<RevokeRefreshTokenResponse>> RevokeRefreshTokenAsync(RefreshTokenRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RefreshToken))
                return ApiResult<RevokeRefreshTokenResponse>.Failure("Invalid request");

            var uid = _currentUserService.GetUserId();
            if (uid == null)
                return ApiResult<RevokeRefreshTokenResponse>.Failure("User not found");

            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user == null || !await _userManager.ValidateRefreshTokenAsync(user, req.RefreshToken))
                return ApiResult<RevokeRefreshTokenResponse>.Failure("Invalid refresh token");

            var rem = await _userManager.RemoveRefreshTokenAsync(user);
            if (!rem.Succeeded)
                return ApiResult<RevokeRefreshTokenResponse>.Failure(rem.ErrorMessage);

            return ApiResult<RevokeRefreshTokenResponse>.Success(new RevokeRefreshTokenResponse { Message = "Revoked" });
        }

        public async Task<ApiResult<string>> ChangePasswordAsync(ChangePasswordRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.OldPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                return ApiResult<string>.Failure("Invalid request");

            var uid = _currentUserService.GetUserId();
            if (uid == null)
                return ApiResult<string>.Failure("User not found");

            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user == null)
                return ApiResult<string>.Failure("User not found");

            var res = await _userManager.ChangeUserPasswordAsync(user, req.OldPassword, req.NewPassword);
            if (!res.Succeeded)
                return ApiResult<string>.Failure(res.ErrorMessage);

            await _userManager.UpdateSecurityStampAsync(user);
            await _userEmailService.SendPasswordChangedNotificationAsync(user.Email);
            return ApiResult<string>.Success("Password changed successfully");
        }

        public async Task<ApiResult<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest req)
        {
            if (req == null)
                return ApiResult<UserResponse>.Failure("Invalid request");

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return ApiResult<UserResponse>.Failure("User not found");

            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                UserMappings.ApplyUpdate(req, user);
                user.UpdatedAt = DateTime.UtcNow;

                if (req.Roles?.Any() == true && _currentUserService.IsAdmin())
                    await _userManager.UpdateRolesAsync(user, req.Roles);

                var upd = await _userManager.UpdateAsync(user);
                if (!upd.Succeeded)
                    return ApiResult<UserResponse>.Failure(string.Join(", ", upd.Errors.Select(e => e.Description)));

                return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(user, _userManager));
            });
        }

        public async Task<ApiResult<UserResponse>> UpdateCurrentUserAsync(UpdateUserRequest req)
        {
            if (req == null)
                return ApiResult<UserResponse>.Failure("Invalid request");

            var uid = _currentUserService.GetUserId();
            if (uid == null)
                return ApiResult<UserResponse>.Failure("User not found");

            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user == null)
                return ApiResult<UserResponse>.Failure("User not found");

            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                UserMappings.ApplyUpdate(req, user);
                user.UpdatedAt = DateTime.UtcNow;

                var upd = await _userManager.UpdateAsync(user);
                if (!upd.Succeeded)
                    return ApiResult<UserResponse>.Failure(string.Join(", ", upd.Errors.Select(e => e.Description)));

                return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(user, _userManager));
            });
        }

        public Task<ApiResult<UserResponse>> LockUserAsync(Guid id) =>
            ChangeLockoutAsync(id, true, DateTimeOffset.MaxValue);

        public Task<ApiResult<UserResponse>> UnlockUserAsync(Guid id) =>
            ChangeLockoutAsync(id, false, DateTimeOffset.UtcNow);

        private async Task<ApiResult<UserResponse>> ChangeLockoutAsync(Guid id, bool enable, DateTimeOffset until)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return ApiResult<UserResponse>.Failure("User not found");

            var res = await _userManager.SetLockoutAsync(user, enable, until);
            if (!res.Succeeded)
                return ApiResult<UserResponse>.Failure(res.ErrorMessage);

            return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(user, _userManager));
        }

        public async Task<ApiResult<object>> DeleteUsersAsync(List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                return ApiResult<object>.Success(null);

            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                foreach (var id in ids)
                {
                    var user = await _userManager.FindByIdAsync(id.ToString());
                    if (user == null)
                        return ApiResult<object>.Failure($"User {id} not found");

                    var del = await _userManager.DeleteAsync(user);
                    if (!del.Succeeded)
                        return ApiResult<object>.Failure(string.Join(", ", del.Errors.Select(e => e.Description)));
                }
                return ApiResult<object>.Success(null);
            });
        }

        public async Task<UserResponse> CreateOrUpdateGoogleUserAsync(GoogleUserInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.Email))
                return null;

            var user = await _userManager.FindByEmailAsync(info.Email);
            var isNew = user == null;

            if (isNew)
            {
                user = UserMappings.ToDomainUser(info);
                var createRes = await _userManager.CreateUserAsync(user);
                if (!createRes.Succeeded)
                    return null;

                await _userManager.AddDefaultRoleAsync(user);
            }
            else
            {
                if (!await _userManager.IsInRoleAsync(user, "USER"))
                    await _userManager.AddDefaultRoleAsync(user);

                if (UserMappings.MergeGoogleInfo(info, user))
                    await _userManager.UpdateAsync(user);
            }

            var tokenResult = await _tokenService.GenerateToken(user);
            var refresh = _tokenService.GenerateRefreshToken();
            await _userManager.SetRefreshTokenAsync(user, refresh.Token);

            return await UserMappings.ToUserResponseAsync(user, _userManager, tokenResult.Data, refresh.Token);
        }

        public async Task<ApiResult<PagedList<UserDetailsDTO>>> GetUsersAsync(int page, int size)
        {
            if (page < 1 || size < 1)
                return ApiResult<PagedList<UserDetailsDTO>>.Failure("Invalid pagination parameters");

            var list = await _userRepository.GetUserDetailsAsync(page, size);
            return ApiResult<PagedList<UserDetailsDTO>>.Success(list);
        }

        // ================ THÊM CÁC METHOD MỚI ================

        // Logout method
        public async Task<ApiResult<string>> LogoutAsync(LogoutRequest request)
        {
            if (request?.RefreshToken != null)
            {
                var revokeRequest = new RefreshTokenRequest { RefreshToken = request.RefreshToken };
                await RevokeRefreshTokenAsync(revokeRequest);
            }
            return ApiResult<string>.Success("Logout successful");
        }

        // Validate token method
        public async Task<ApiResult<ValidateTokenResponse>> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ApiResult<ValidateTokenResponse>.Failure("Token is required");

            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(token))
                return ApiResult<ValidateTokenResponse>.Failure("Invalid token format");

            var jwtToken = tokenHandler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                return ApiResult<ValidateTokenResponse>.Success(new ValidateTokenResponse
                {
                    IsValid = false
                });
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            var emailClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email);
            var roleClaims = jwtToken.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList();

            // Kiểm tra user vẫn tồn tại và active
            if (userIdClaim != null)
            {
                var user = await _userManager.FindByIdAsync(userIdClaim.Value);
                if (user == null || await _userManager.IsLockedOutAsync(user))
                {
                    return ApiResult<ValidateTokenResponse>.Success(new ValidateTokenResponse
                    {
                        IsValid = false
                    });
                }
            }

            return ApiResult<ValidateTokenResponse>.Success(new ValidateTokenResponse
            {
                IsValid = true,
                UserId = userIdClaim?.Value,
                Email = emailClaim?.Value,
                Roles = roleClaims,
                ExpiresAt = jwtToken.ValidTo
            });
        }

        // Verify 2FA method
        public async Task<ApiResult<string>> Verify2FAAsync(Verify2FARequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
                return ApiResult<string>.Failure("2FA code is required");

            var userId = _currentUserService.GetUserId();
            if (userId == null)
                return ApiResult<string>.Failure("User not found");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ApiResult<string>.Failure("User not found");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider,
                request.Code);

            if (!isValid)
                return ApiResult<string>.Failure("Invalid 2FA code");

            return ApiResult<string>.Success("2FA verification successful");
        }
    }
}