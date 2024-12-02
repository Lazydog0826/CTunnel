using System.Collections.Concurrent;
using System.Net.WebSockets;
using CTunnel.Share.Enums;

namespace CTunnel.Share.Model
{
    public class TunnelModel
    {
        /// <summary>
        /// 监听端口
        /// </summary>
        public int ListenPort { get; set; }

        /// <summary>
        /// 隧道类型
        /// </summary>
        public TunnelTypeEnum Type { get; set; }

        /// <summary>
        /// 文件共享路径
        /// </summary>
        public string FileSharingPath { get; set; } = string.Empty;

        /// <summary>
        /// 域名
        /// </summary>
        public string DomainName { get; set; } = string.Empty;

        /// <summary>
        /// 主连接
        /// </summary>
        public WebSocket WebSocket { get; set; } = null!;

        /// <summary>
        /// 心跳计时器
        /// </summary>
        public Timer PulseCheck { get; set; } = null!;

        /// <summary>
        /// 连接取消状态
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = null!;

        /// <summary>
        /// 子连接
        /// </summary>
        public ConcurrentDictionary<string, TunnelSubConnectModel> SubConnect { get; set; } = [];

        #region 客户端使用

        /// <summary>
        /// 服务器IP
        /// </summary>
        public string ServerIp { get; set; } = string.Empty;

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// 目标IP
        /// </summary>
        public string TargetIp { get; set; } = string.Empty;

        /// <summary>
        /// 目标端口
        /// </summary>
        public int TargetPort { get; set; }

        #endregion 客户端使用
    }
}
