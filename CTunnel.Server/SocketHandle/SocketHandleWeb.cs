using System.Buffers;
using System.Net.Sockets;
using CTunnel.Share;
using CTunnel.Share.Enums;
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
        var requestItem = new RequestItem()
        {
            RequestId = Guid.NewGuid().ToString(),
            TargetSocket = socket,
            TargetSocketStream = socketStream,
        };

        using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
        TunnelModel? tunnel = null;

        try
        {
            int readCount;
            while ((readCount = await socketStream.ReadAsync(memory.Memory)) != 0)
            {
                // tunnel=null需要解析一遍host
                if (tunnel == null)
                {
                    // 解析HOST
                    var host = memory.Memory.ParseWebRequest();
                    // 根据HOST获取隧道
                    tunnel = tunnelContext.GetTunnel(host);
                }

                // 如果隧道不存在直接返回404
                if (tunnel == null)
                {
                    await socketStream.ReturnNotFoundAsync();
                }
                else
                {
                    tunnel.ConcurrentDictionary.TryAdd(requestItem.RequestId, requestItem);
                    await tunnel.WebSocket.ForwardAsync(
                        MessageTypeEnum.Forward,
                        requestItem.RequestId.ToBytes(),
                        memory.Memory[..readCount],
                        tunnel.ForwardToClientSlim
                    );
                }
            }
        }
        finally
        {
            await requestItem.CloseAsync(tunnel?.ConcurrentDictionary ?? []);
        }
    }
}
