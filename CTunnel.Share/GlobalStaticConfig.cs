namespace CTunnel.Share
{
    public readonly struct GlobalStaticConfig
    {
        public static TimeSpan Interval = TimeSpan.FromSeconds(5);

        public static TimeSpan TenYears = TimeSpan.FromDays(365 * 10);
    }
}
