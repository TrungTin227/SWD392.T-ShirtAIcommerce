namespace Services.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? GetUserId();
        string? GetCurrentUserEmail();
        bool IsAuthenticated();
        bool IsAdmin();
        bool IsCustomer();
        bool IsStaff();
        bool CanManageProducts();
        bool CanProcessOrders();
    }
}