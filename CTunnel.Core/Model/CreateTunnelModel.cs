using CTunnel.Core.Enums;

namespace CTunnel.Core.Model
{
    public class CreateTunnelModel
    {
        public string Id { get; set; } = string.Empty;

        public string AuthCode { get; set; } = string.Empty;

        public string ServerIp { get; set; } = string.Empty;

        public int ServerPort { get; set; }

        public int ListenPort { get; set; }

        public string TargetIp { get; set; } = string.Empty;

        public int TargePort { get; set; }

        public TunnelTypeEnum Type { get; set; }
    }
}
