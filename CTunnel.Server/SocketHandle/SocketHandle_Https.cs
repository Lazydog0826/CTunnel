using System.Net.Sockets;
using CTunnel.Share;

namespace CTunnel.Server.SocketHandle
{
    public class SocketHandle_Https(TunnelContext _tunnelContext) : ISocketHandle
    {
        public async Task HandleAsync(Socket socket)
        {
            Log.Write("Https 请求", LogType.Important);
            await SocketHandle_Web.HandleAsync(socket, _tunnelContext, true);
        }
    }
}
