using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class CurrentTime : ICurrentTime
    {
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }

        public DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        public DateOnly GetCurrentDate()
        {
            return DateOnly.FromDateTime(GetVietnamTime());
        }

        public TimeOnly GetCurrentTime()
        {
            return TimeOnly.FromDateTime(GetVietnamTime());
        }
    }
}