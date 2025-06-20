using BusinessObjects.Identity;
using Microsoft.AspNetCore.Identity;
using Services.Helpers;

namespace Services.Extensions
{
    public static class UserManagerExtensions
    {
        public static async Task<bool> ExistsByEmailAsync(
            this UserManager<ApplicationUser> mgr, string email)
            => await mgr.FindByEmailAsync(email) != null;

        public static async Task<IdentityResultWrapper> CreateUserAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user, string password = null)
        {
            // Đảm bảo UserName được đặt bằng email trước khi tạo người dùng
            if (string.IsNullOrEmpty(user.UserName))
                user.UserName = user.Email;

            var res = password != null
                ? await mgr.CreateAsync(user, password)
                : await mgr.CreateAsync(user);
            return new IdentityResultWrapper(res);
        }

        public static Task AddDefaultRoleAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user)
            => mgr.AddToRoleAsync(user, "Customer");

        public static Task AddRolesAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user, IEnumerable<string> roles)
            => mgr.AddToRolesAsync(user, roles ?? new[] { "Customer" });

        public static Task SetRefreshTokenAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user, string token)
            => mgr.SetAuthenticationTokenAsync(user, "T-ShirtAIcommerce", "RefreshToken", token);

        public static async Task<bool> ValidateRefreshTokenAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user, string token)
            => await mgr.GetAuthenticationTokenAsync(user, "T-ShirtAIcommerce", "RefreshToken") == token;

        public static Task ResetAccessFailedAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user)
            => mgr.ResetAccessFailedCountAsync(user);

        public static async Task<IdentityResultWrapper> RemoveRefreshTokenAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user)
        {
            var res = await mgr.RemoveAuthenticationTokenAsync(
                user, "MyApp", "RefreshToken");
            return new IdentityResultWrapper(res);
        }

        public static async Task<IdentityResultWrapper> ChangeUserPasswordAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user, string oldPwd, string newPwd)
        {
            var res = await mgr.ChangePasswordAsync(user, oldPwd, newPwd);
            return new IdentityResultWrapper(res);
        }

        public static async Task<IdentityResultWrapper> SetLockoutAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user, bool enable, DateTimeOffset until)
        {
            await mgr.SetLockoutEnabledAsync(user, enable);
            var res = await mgr.SetLockoutEndDateAsync(user, until);
            return new IdentityResultWrapper(res);
        }

        public static Task UpdateSecurityStampAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user)
            => mgr.UpdateSecurityStampAsync(user);

        public static async Task UpdateRolesAsync(
            this UserManager<ApplicationUser> mgr, ApplicationUser user, IEnumerable<string> roles)
        {
            var oldRoles = await mgr.GetRolesAsync(user);
            await mgr.RemoveFromRolesAsync(user, oldRoles);
            await mgr.AddToRolesAsync(user, roles ?? new[] { "Customer" });
        }
    }
}