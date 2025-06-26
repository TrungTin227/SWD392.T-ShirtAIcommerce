namespace Repositories.Interfaces
{
    public interface ICurrentTime
    {
        DateTime GetUtcNow();
        DateTime GetVietnamTime();
        DateOnly GetCurrentDate();
        TimeOnly GetCurrentTime();
    }
}