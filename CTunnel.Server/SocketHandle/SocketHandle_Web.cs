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

                    (host, count) = await socketStream.ParseWebRequestAsync(buffer);
                    var tunnel = tunnelContext.GetTunnel(host);
                    if (tunnel == null)
                    {
                        var message = "Tunnel does not exist or no connection is available ";
                        await socketStream.ReturnTemplateHtmlAsync(message);
                        return;
                    }
                    tunnel.ConcurrentDictionary.TryAdd(requestItem.RequestId, requestItem);

                    await tunnel.WebSocket.ForwardAsync(
                        MessageTypeEnum.Forward,
                        requestItem.RequestId,
                        buffer,
                        0,
                        count,
                        tunnel.Slim
                    );
                    while ((count = await socketStream.ReadAsync(buffer)) != 0)
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
                }
            );
        }
    }
}
