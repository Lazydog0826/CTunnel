using System.Buffers;
using System.Net.Sockets;
using System.Text;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server.SocketHandle;

public static class SocketHandleWeb
{
    public static async Task HandleAsync(Socket socket, TunnelContext tunnelContext, bool isHttps)
    {
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        var socketStream = await socket.GetStreamAsync(isHttps, true, string.Empty);
        using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
        var readCount = await socketStream.ReadAsync(memory.Memory);
        var requestItem = new RequestItem()
        {
            RequestId = Guid.NewGuid().ToString(),
            TargetSocket = socket,
            TargetSocketStream = socketStream,
        };

        // 解析HOST
        var host = memory.Memory.ParseWebRequest();

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
            await ForwardToTunnelAsync(memory.Memory[..readCount]);
            while ((readCount = await socketStream.ReadAsync(memory.Memory)) != 0)
            {
                await ForwardToTunnelAsync(memory.Memory[..readCount]);
            }
        }
        finally
        {
            await requestItem.CloseAsync(tunnel.ConcurrentDictionary);
        }

        return;

        async Task ForwardToTunnelAsync(Memory<byte> temMemory)
        {
            await tunnel.WebSocket.ForwardAsync(
                MessageTypeEnum.Forward,
                requestItem.RequestId.ToBytes(),
                temMemory,
                tunnel.Slim
            );
        }
    }
}
