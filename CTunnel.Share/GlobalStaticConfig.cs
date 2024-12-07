namespace CTunnel.Share
{
    /// <summary>
    /// 静态配置
    /// </summary>
    public readonly struct GlobalStaticConfig
    {
        /// <summary>
        /// 超时时间
        /// </summary>
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// 流读取缓冲限制
        /// </summary>
        public static readonly int BufferSize = 81920;
    }
}
