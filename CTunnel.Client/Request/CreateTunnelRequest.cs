using CTunnel.Share.Enums;

namespace CTunnel.Client.Request
{
    public class CreateTunnelRequest
    {
        public string ServerIp { get; set; } = string.Empty;

        public int ServerProt { get; set; }

        public string DomainName { get; set; } = string.Empty;

        public int ListenProt { get; set; }

        public TunnelTypeEnum Type { get; set; }

        public string Token { get; set; } = string.Empty;

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
