using System.Net.Sockets;
using MiniComp.Autofac;

namespace CTunnel.Server.SocketHandle;

[AutofacDependency(typeof(ISocketHandle), ServiceKey = "Http")]
public class SocketHandleHttp(TunnelContext tunnelContext) : ISocketHandle
{
    public async Task HandleAsync(Socket socket, int port)
    {
        await SocketHandleWeb.HandleAsync(socket, tunnelContext, false);
    }
}
