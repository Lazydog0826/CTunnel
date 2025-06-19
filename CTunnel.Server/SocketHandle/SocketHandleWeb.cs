using System.Buffers;
using System.Net.Sockets;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server.SocketHandle;

public static class SocketHandleWeb
{
    public static async Task HandleAsync(Socket socket, TunnelContext tunnelContext, bool isHttps)
    {
        // 新的Socket连接
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        var socketStream = await socket.GetStreamAsync(isHttps, true, string.Empty);
        var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
        var readCount = await socketStream.ReadAsync(memory.Memory);

        // 解析HOST
        var host = memory.Memory.ParseWebRequest();
        // 根据HOST获取隧道
        var tunnel = tunnelContext.GetTunnel(host);
        if (tunnel == null)
        {
            await socketStream.ReturnNotFoundAsync();
            await socket.TryCloseAsync();
            return;
        }
        var requestItem = new RequestItem()
        {
            TargetSocket = socket,
            TargetSocketStream = socketStream,
            ToBeSent = memory,
            ToBeSentCount = readCount
        };
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
