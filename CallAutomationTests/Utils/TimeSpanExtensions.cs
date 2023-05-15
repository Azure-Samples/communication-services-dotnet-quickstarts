namespace CallAutomation.Scenarios.Utils
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan? TryGetTimeSpan(this double? seconds)
        {
            if (!seconds.HasValue)
                return null;

            return TimeSpan.FromSeconds(seconds.Value);
        }

        public static double? TryGetSeconds(this TimeSpan? timeSpan)
        {
            if (!timeSpan.HasValue)
                return null;

            return (double?)timeSpan.Value.TotalSeconds;
        }
    }
}
