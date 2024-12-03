using System.Net.Sockets;
using CTunnel.Server.ServerSocketHandle;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server.SocketHandle
{
    public class SocketHandle_Server : ISocketHandle
    {
        public async Task HandleAsync(Socket socket)
        {
            var timeout = new CancellationTokenSource(GlobalStaticConfig.Timeout);
            var socketStream = await socket.GetStreamAsync(true, true, string.Empty);
            var typeMessage = await socketStream.ReadMessageAsync<SocketTypeMessage>(timeout.Token);
            var serverSocketHandle = ServiceContainer.GetService<IServerSocketHandle>(
                typeMessage.MessageType.ToString()
            );
            await serverSocketHandle.HandleAsync(socket, socketStream, typeMessage.JsonData);
        }
    }
}
