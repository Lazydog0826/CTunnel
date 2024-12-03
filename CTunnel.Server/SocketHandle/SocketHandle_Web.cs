using System.Buffers;
using System.Net.Sockets;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Server.SocketHandle
{
    public static class SocketHandle_Web
    {
        public static async Task HandleAsync(
            Socket socket,
            TunnelContext tunnelContext,
            bool isHttps
        )
        {
            var socketStream = await socket.GetStreamAsync(isHttps, true, string.Empty);
            var buffer = ArrayPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
            string host;
            int count;
            try
            {
                (host, count) = await socketStream.ParseWebRequestAsync(
                    buffer,
                    CancellationToken.None
                );
                var tunnel = tunnelContext.GetTunnel(host);
                if (tunnel == null)
                {
                    await socketStream.ReturnTemplateHtmlAsync("隧道不存在");
                    await socket.TryCloseAsync();
                    return;
                }
                var tunnelSubConnect = new TunnelSubConnectModel
                {
                    RequestId = Guid.NewGuid().ToString(),
                    ListenSocket = socket,
                    ListenSocketStream = socketStream,
                    CancellationTokenSource = new CancellationTokenSource()
                };
                tunnelSubConnect.CreateHeartbeatCheck(tunnel);

                // 请求连接事件
                tunnelSubConnect.ClientRequestConnectEvent += async () =>
                {
                    await tunnelSubConnect.MasterSocketStream.WriteAsync(buffer.AsMemory(0, count));
                    TaskExtend.NewTask(
                        async () =>
                        {
                            await tunnelSubConnect.MasterSocketStream.ForwardAsync(
                                tunnelSubConnect.ListenSocketStream,
                                tunnelSubConnect.CancellationTokenSource.Token
                            );
                        },
                        async _ =>
                        {
                            await tunnelSubConnect.CancellationTokenSource.CancelAsync();
                            await tunnelSubConnect.MasterSocket.TryCloseAsync();
                        }
                    );
                    TaskExtend.NewTask(
                        async () =>
                        {
                            await tunnelSubConnect.ListenSocketStream.ForwardAsync(
                                tunnelSubConnect.MasterSocketStream,
                                tunnelSubConnect.CancellationTokenSource.Token
                            );
                        },
                        async _ =>
                        {
                            await tunnelSubConnect.CancellationTokenSource.CancelAsync();
                            await tunnelSubConnect.ListenSocket.TryCloseAsync();
                        }
                    );
                };
                tunnel.SubConnect.TryAdd(tunnelSubConnect.RequestId, tunnelSubConnect);
                await tunnel.MasterSocketStream.SendMessageAsync(
                    new SocketTypeMessage
                    {
                        MessageType = WebSocketMessageTypeEnum.NewRequest,
                        JsonData = JsonConvert.SerializeObject(
                            new NewRequest
                            {
                                DomainName = tunnel.DomainName,
                                RequestId = tunnelSubConnect.RequestId,
                                Token = string.Empty,
                                Host = host
                            }
                        )
                    },
                    tunnelSubConnect.CancellationTokenSource.Token
                );
                Log.Write($"{host} 监听到来自网络的请求，已发送通知给客户端", LogType.Success);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
