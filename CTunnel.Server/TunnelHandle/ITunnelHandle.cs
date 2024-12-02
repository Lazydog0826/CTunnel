using System.Net.Sockets;
using CTunnel.Share.Model;

namespace CTunnel.Server.TunnelHandle
{
    public interface ITunnelHandle
    {
        public Task HandleAsync(TunnelModel tunnel, TcpClient requestClient);

        public Task<bool> IsCloseAsync(TunnelModel tunnel);
    }
}
