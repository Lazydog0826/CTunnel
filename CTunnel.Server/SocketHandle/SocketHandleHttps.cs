using System.Net.Sockets;

namespace CTunnel.Server.SocketHandle;

public class SocketHandleHttps(TunnelContext tunnelContext) : ISocketHandle
{
    public async Task HandleAsync(Socket socket, int port)
    {
        await SocketHandleWeb.HandleAsync(socket, tunnelContext, true);
    }
}
