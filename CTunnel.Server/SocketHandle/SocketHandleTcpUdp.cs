﻿using System.Buffers;
using System.Net.Sockets;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using MiniComp.Autofac;

namespace CTunnel.Server.SocketHandle;

[AutofacDependency(typeof(ISocketHandle), ServiceKey = "TcpUdp")]
public class SocketHandleTcpUdp(TunnelContext tunnelContext) : ISocketHandle
{
    public async Task HandleAsync(Socket socket, int port)
    {
        // 新的Socket连接
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        var socketStream = await socket.GetStreamAsync(false, true, string.Empty);
        var memory = MemoryPool<byte>.Shared.Rent(0);
        var tunnel = tunnelContext.GetTunnel(port.ToString());
        if (tunnel == null)
            return;
        var requestItem = new RequestItem()
        {
            TargetSocket = socket,
            TargetSocketStream = socketStream,
            ToBeSent = memory,
            ToBeSentCount = 0
        };
        requestItem.TokenSource.Token.Register(() =>
        {
            requestItem.TargetSocket.TryCloseAsync().Wait();
            requestItem.ForwardSocket.TryCloseAsync().Wait();
        });
        tunnel.ConcurrentDictionary.TryAdd(requestItem.Id, requestItem);

        // 发送新连接通知
        var registerRequest = new RegisterRequest
        {
            TunnelKey = tunnel.Key,
            RequestId = requestItem.Id
        };
        await tunnel.WebSocket.SendMessageAsync(
            WebSocketMessageTypeEnum.NewRequest,
            registerRequest
        );
        await Task.Delay(Timeout.InfiniteTimeSpan, requestItem.TokenSource.Token);
    }
}
