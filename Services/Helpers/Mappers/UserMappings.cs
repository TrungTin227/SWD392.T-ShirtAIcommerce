using BusinessObjects.Identity;
using BusinessObjects.Products;
using DTOs.UserDTOs.Identities;
using DTOs.UserDTOs.Request;
using DTOs.UserDTOs.Response;
using Microsoft.AspNetCore.Identity;

namespace Services.Helpers.Mappers
{
    public static class UserMappings
    {
        // Request to Domain mappings
        public static ApplicationUser ToDomainUser(AdminCreateUserRequest request)
        {
            return new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                Gender = Enum.TryParse<Gender>(request.Gender, out var gender) ? gender : Gender.Other,
                EmailConfirmed = true, // Admin-created accounts are confirmed by default
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static ApplicationUser ToDomainUser(this ApplicationUser req)
        {
            return new ApplicationUser
            {
                FirstName = req.FirstName,
                LastName = req.LastName,
                Email = req.Email,
                Gender = req.Gender,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static ApplicationUser ToDomainUser(this GoogleUserInfo info)
        {
            return new ApplicationUser
            {
                UserName = info.Email,
                FirstName = info.FirstName,
                LastName = info.LastName,
                Email = info.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static void ApplyUpdate(this UpdateUserRequest req, ApplicationUser user)
        {
            if (!string.IsNullOrEmpty(req.FirstName)) user.FirstName = req.FirstName;
            if (!string.IsNullOrEmpty(req.LastName)) user.LastName = req.LastName;
            if (req.Gender != null) user.Gender = req.Gender;
            user.UpdatedAt = DateTime.UtcNow;
        }

        public static bool MergeGoogleInfo(this GoogleUserInfo info, ApplicationUser user)
        {
            bool changed = false;
            if (user.FirstName != info.FirstName)
            {
                user.FirstName = info.FirstName;
                changed = true;
            }
            if (user.LastName != info.LastName)
            {
                user.LastName = info.LastName;
                changed = true;
            }
            if (changed)
            {
                user.UpdatedAt = DateTime.UtcNow;
            }
            return changed;
        }

        // Domain to Response mappings
        public static async Task<UserResponse> ToUserResponseAsync(this ApplicationUser user, UserManager<ApplicationUser> userManager, string accessToken = null, string refreshToken = null)
        {
            var roles = await userManager.GetRolesAsync(user);
            return new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Gender = user.Gender.ToString(),
                PhoneNumbers = user.PhoneNumber,
                CreateAt = user.CreatedAt,
                UpdateAt = user.UpdatedAt,
                IsActive = user.LockoutEnabled,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Roles = roles.ToList()
            };
        }

        public static CurrentUserResponse ToCurrentUserResponse(this ApplicationUser user, string accessToken = null)
        {
            return new CurrentUserResponse
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Gender = user.Gender.ToString(),
                PhoneNumbers = user.PhoneNumber,
                CreateAt = user.CreatedAt,
                UpdateAt = user.UpdatedAt,
                AccessToken = accessToken
            };
        }
        public static ApplicationUser ToDomainUser(UserRegisterRequest request)
        {
            return new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                Gender = request.Gender, 
                EmailConfirmed = false, // Regular registration requires email confirmation
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}