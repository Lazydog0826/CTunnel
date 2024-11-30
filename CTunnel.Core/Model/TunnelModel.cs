using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using CTunnel.Core.Enums;

namespace CTunnel.Core.Model
{
    public class TunnelModel
    {
        public string Id { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public int ListenPort { get; set; }

        public TunnelTypeEnum Type { get; set; }

        public WebSocket WebSocket { get; set; } = null!;

        //public BlockingCollection<TcpClient> ConnectionPool { get; set; } = [];

        //public TcpClient LongConnection { get; set; } = null!;

        //public TcpListener Listener { get; set; } = null!;

        //public Timer Timer { get; set; } = null!;
    }
}
