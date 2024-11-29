using System.Collections.Concurrent;
using System.Net.Sockets;
using CTunnel.Core.Enums;

namespace CTunnel.Core.Model
{
    public class TunnelModel
    {
        public string Id { get; set; } = string.Empty;

        public string AuthCode { get; set; } = string.Empty;

        public int ListenPort { get; set; }

        public TunnelTypeEnum Type { get; set; }

        public BlockingCollection<TcpClient> ConnectionPool { get; set; } = [];

        public TcpClient LongConnection { get; set; } = null!;

        public TcpListener Listener { get; set; } = null!;

        public Timer Timer { get; set; } = null!;
    }
}
