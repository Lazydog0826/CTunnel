using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
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

        public static void CreateWebSocketListen(AppConfig appConfig)
        {
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://{appConfig.ServerIp}:{appConfig.ServerPort}/");
            httpListener.Start();

            TaskExtend.NewTask(async () =>
            {
                var webSocketHandle = ServiceContainer.GetService<WebSocketHandle>();
                while (true)
                {
                    var context = await httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        var webSocket = await context.AcceptWebSocketAsync(null);
                        TaskExtend.NewTask(
                            async () => await webSocketHandle.HandleAsync(webSocket),
                            async _ =>
                            {
                                await webSocket.WebSocket.CloseAsync(
                                    WebSocketCloseStatus.Empty,
                                    string.Empty,
                                    CancellationToken.None
                                );
                                context.Response.Close();
                            }
                        );
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            });
            Log.Write($"开始监听 {appConfig.ServerIp}:{appConfig.ServerPort}", LogType.Success);
        }
    }
}
