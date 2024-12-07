using System.Net.Sockets;
using CTunnel.Share;

namespace CTunnel.Server.SocketHandle
{
    public class SocketHandle_Http(TunnelContext _tunnelContext) : ISocketHandle
    {
        public async Task HandleAsync(Socket socket)
        {
            await SocketHandle_Web.HandleAsync(socket, _tunnelContext, false);
        }
    }
}
