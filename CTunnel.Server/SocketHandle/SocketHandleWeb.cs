using System.Buffers;
using System.Net.Sockets;
using System.Net.WebSockets;
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
        // 新的Socket连接
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        var socketStream = await socket.GetStreamAsync(isHttps, true, string.Empty);
        using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
        TunnelModel? tunnel = null;
        var requestItem = new RequestItem()
        {
            RequestId = [],
            TargetSocket = socket,
            TargetSocketStream = socketStream,
        };
        try
        {
            int readCount;
            var isHaveSlim = false;
            while ((readCount = await socketStream.ReadAsync(memory.Memory)) != 0)
            {
                // tunnel=null需要解析一遍host
                if (tunnel == null)
                {
                    // 解析HOST
                    var host = memory.Memory.ParseWebRequest();
                    // 根据HOST获取隧道
                    tunnel = tunnelContext.GetTunnel(host);
                    tunnel?.ConcurrentDictionary.TryAdd(requestItem.Id, requestItem);
                }

                // 如果隧道不存在直接返回404
                if (tunnel == null)
                {
                    await socketStream.ReturnNotFoundAsync();
                }
                else
                {
                    if (isHaveSlim == false)
                    {
                        // 请求的第一波数据
                        await tunnel.ForwardToClientSlim.WaitAsync();
                        isHaveSlim = true;
                        var newRequestId = Guid.NewGuid().ToString();
                        requestItem.RequestId.TryAdd(newRequestId, 0);
                        await tunnel.WebSocket.SendAsync(
                            newRequestId.ToBytes(),
                            WebSocketMessageType.Binary,
                            false,
                            CancellationToken.None
                        );
                    }
                    var isEnd = socket.Available == 0;
                    await tunnel.WebSocket.SendAsync(
                        memory.Memory[..readCount],
                        WebSocketMessageType.Binary,
                        isEnd,
                        CancellationToken.None
                    );
                    if (isEnd && isHaveSlim)
                    {
                        isHaveSlim = false;
                        tunnel.ForwardToClientSlim.Release();
                    }
                }
            }
        }
        finally
        {
            await requestItem.CloseAsync();
        }
    }
}
