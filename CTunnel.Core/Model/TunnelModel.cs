using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using CTunnel.Core.Enums;

namespace CTunnel.Core.Model
{
    public class TunnelModel
    {
        public string Id { get; set; } = string.Empty;

        public string AuthCode { get; set; } = string.Empty;

        public int ListenPort { get; set; }

        public string DomainName { get; set; } = string.Empty;

        public string TargetIp { get; set; } = string.Empty;

        public int TargePort { get; set; }

        public TunnelTypeEnum Type { get; set; }

        public BlockingCollection<WebSocket> ConnectionPool { get; set; } = [];

        public ClientWebSocket LongConnection { get; set; } = null!;

        public TcpListener Listener { get; set; } = null!;
    }
}
