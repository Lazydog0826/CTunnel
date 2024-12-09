using System.Net.Sockets;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

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
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            var socketStream = await socket.GetStreamAsync(isHttps, true, string.Empty);

            await BytesExpand.UseBufferAsync(
                GlobalStaticConfig.BufferSize,
                async buffer =>
                {
                    string host;
                    int count;

                    var requestItem = new RequestItem()
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        TargetSocket = socket,
                        TargetSocketStream = socketStream
                    };

                    // 解析HOST
                    (host, count) = await socketStream.ParseWebRequestAsync(buffer);

                    // 根据HOST获取隧道
                    var tunnel = tunnelContext.GetTunnel(host);

                    // 如果隧道不存在直接返回404
                    if (tunnel == null)
                    {
                        await socketStream.ReturnNotFoundAsync();
                        return;
                    }
                    tunnel.ConcurrentDictionary.TryAdd(requestItem.RequestId, requestItem);
                    try
                    {
                        async Task ForwardToTunnelAsync()
                        {
                            await tunnel.WebSocket.ForwardAsync(
                                MessageTypeEnum.Forward,
                                requestItem.RequestId,
                                buffer,
                                0,
                                count,
                                tunnel.Slim
                            );
                        }
                        await ForwardToTunnelAsync();
                        while ((count = await socketStream.ReadAsync(buffer)) != 0)
                        {
                            await ForwardToTunnelAsync();
                        }
                    }
                    finally
                    {
                        await requestItem.CloseAsync(tunnel.ConcurrentDictionary);
                    }
                }
            );
        }
    }
}
