using CTunnel.Share.Enums;

namespace CTunnel.Client
{
    public class AppConfig
    {
        /// <summary>
        /// 服务器IP
        /// </summary>
        public string ServerHost { get; set; } = string.Empty;

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// Token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 域名
        /// </summary>
        public string DomainName { get; set; } = string.Empty;

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public TunnelTypeEnum Type { get; set; }

        /// <summary>
        /// 目标IP
        /// </summary>
        public string TargetIp { get; set; } = string.Empty;

        /// <summary>
        /// 目标端口
        /// </summary>
        public int TargetPort { get; set; }
    }
}
