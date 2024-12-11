using Microsoft.IO;

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
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 流读取缓冲限制
        /// </summary>
        public static readonly int BufferSize = 81920;

        /// <summary>
        /// MemoryStream对象提供池化
        /// </summary>
        public static readonly RecyclableMemoryStreamManager MSManager = new();

        /// <summary>
        /// 服务容器
        /// </summary>
        public static IServiceProvider ServiceProvider { get; set; } = null!;
    }
}
