using System.Net.Sockets;
using MiniComp.Autofac;

namespace CTunnel.Server.SocketHandle;

[AutofacDependency(typeof(ISocketHandle), ServiceKey = "Https")]
public class SocketHandleHttps(TunnelContext tunnelContext) : ISocketHandle
{
    public async Task HandleAsync(Socket socket, int port)
    {
        await SocketHandleWeb.HandleAsync(socket, tunnelContext, true);
    }
}
