using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Services.Interfaces;

namespace Services.Implementations
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? GetUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return null;

            if (Guid.TryParse(userIdString, out var userId))
                return userId;

            return null;
        }

        public string? GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        public bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;
        }

        public bool IsCustomer()
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole("Customer") ?? false;
        }

        public bool IsStaff()
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole("Staff") ?? false;
        }

        public bool CanManageProducts()
        {
            return IsAdmin() || _httpContextAccessor.HttpContext?.User?.IsInRole("PRODUCT_MANAGER") == true;
        }

        public bool CanProcessOrders()
        {
            return IsAdmin() || _httpContextAccessor.HttpContext?.User?.IsInRole("ORDER_PROCESSOR") == true;
        }
    }
}