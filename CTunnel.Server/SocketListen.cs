using System.Net;
using System.Net.Sockets;
using CTunnel.Server.SocketHandle;
using CTunnel.Share.Expand;

namespace CTunnel.Server
{
    public static class SocketListen
    {
        public static async Task CreateSocketListenAsync(
            ProtocolType protocolType,
            int port,
            ISocketHandle socketHandle
        )
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, protocolType);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen();
            TaskExtend.NewTask(async () =>
            {
                while (true)
                {
                    var newConnect = await socket.AcceptAsync();
                    TaskExtend.NewTask(
                        async () =>
                        {
                            await socketHandle.HandleAsync(newConnect);
                            await newConnect.TryCloseAsync();
                        },
                        async _ =>
                        {
                            await newConnect.TryCloseAsync();
                        }
                    );
                }
            });
            await Task.Delay(Timeout.InfiniteTimeSpan);
        }
    }
}
