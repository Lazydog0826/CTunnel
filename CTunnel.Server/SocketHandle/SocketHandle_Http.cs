using System.Net.Sockets;

namespace CTunnel.Server.SocketHandle
{
    public class SocketHandle_Http(TunnelContext _tunnelContext) : ISocketHandle
    {
        public async Task HandleAsync(Socket socket, int port)
        {
            await SocketHandle_Web.HandleAsync(socket, _tunnelContext, false);
        }
    }
}
