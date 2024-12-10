﻿using System.Net.Sockets;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server.SocketHandle
{
    public class SocketHandle_TcpUdp(TunnelContext tunnelContext) : ISocketHandle
    {
        public async Task HandleAsync(Socket socket, int port)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            var socketStream = await socket.GetStreamAsync(false, true, string.Empty);

            await BytesExpand.UseBufferAsync(
                GlobalStaticConfig.BufferSize,
                async buffer =>
                {
                    var requestItem = new RequestItem()
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        TargetSocket = socket,
                        TargetSocketStream = socketStream
                    };

                    var tunnel = tunnelContext.GetTunnel(port.ToString());
                    if (tunnel == null)
                        return;
                    tunnel.ConcurrentDictionary.TryAdd(requestItem.RequestId, requestItem);
                    try
                    {
                        int count;
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
                    finally
                    {
                        await requestItem.CloseAsync(tunnel.ConcurrentDictionary);
                    }
                }
            );
        }
    }
}
