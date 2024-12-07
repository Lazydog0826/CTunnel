namespace CTunnel.Share
{
    public readonly struct GlobalStaticConfig
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        public static readonly int BufferSize = 81920;
    }
}
