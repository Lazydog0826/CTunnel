using System.Net.Sockets;
using CTunnel.Core.Model;

namespace CTunnel.Core.TunnelHandle
{
    public interface ITunnelHandle
    {
        public Task HandleAsync(TunnelModel tunnel, TcpClient tcpClient);

        public Task<bool> IsCloseAsync(TunnelModel tunnel);
    }
}
