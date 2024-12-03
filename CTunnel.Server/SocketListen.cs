using System.Net;
using System.Net.Sockets;
using CTunnel.Server.SocketHandle;
using CTunnel.Share;
using CTunnel.Share.Expand;

namespace CTunnel.Server
{
    public static class SocketListen
    {
        public static void CreateSocketListen(Socket socket, int port, ISocketHandle socketHandle)
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen();
            TaskExtend.NewTask(async () =>
            {
                while (true)
                {
                    var newConnect = await socket.AcceptAsync();
                    TaskExtend.NewTask(
                        async () => await socketHandle.HandleAsync(newConnect),
                        async _ =>
                        {
                            await newConnect.TryCloseAsync();
                        }
                    );
                }
            });
            Log.Write($"监听端口 {port}", LogType.Success);
        }
    }
}
