using CTunnel.Share.Enums;

namespace CTunnel.Client.Request
{
    public class CreateTunnelRequest
    {
        /// <summary>
        /// 服务器主机
        /// </summary>
        public string ServerHost { get; set; } = string.Empty;

        /// <summary>
        /// 域名
        /// </summary>
        public string DomainName { get; set; } = string.Empty;

        /// <summary>
        /// 服务端监听端口
        /// </summary>
        public int ListenProt { get; set; }

        /// <summary>
        /// 隧道类型
        /// </summary>
        public TunnelTypeEnum Type { get; set; }

        /// <summary>
        /// 令牌
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 目标IP
        /// </summary>
        public string TargetIp { get; set; } = string.Empty;

        /// <summary>
        /// 目标端口
        /// </summary>
        public int TargetPort { get; set; }

        /// <summary>
        /// 文件共享路径
        /// </summary>
        public string FileSharingPath { get; set; } = string.Empty;
    }
}
