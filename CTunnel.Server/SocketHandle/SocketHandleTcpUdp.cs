using System.Buffers;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server.SocketHandle;

public class SocketHandleTcpUdp(TunnelContext tunnelContext) : ISocketHandle
{
    public async Task HandleAsync(Socket socket, int port)
    {
        // Console.WriteLine("新连接");
        // socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        // var socketStream = await socket.GetStreamAsync(false, true, string.Empty);
        // using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
        // var tunnel = tunnelContext.GetTunnel(port.ToString());
        // if (tunnel == null)
        //     return;
        // var requestItem = new RequestItem()
        // {
        //     RequestId = [],
        //     TargetSocket = socket,
        //     TargetSocketStream = socketStream,
        // };
        // tunnel.ConcurrentDictionary.TryAdd(requestItem.Id, requestItem);
        // try
        // {
        //     var newRequestId = Guid.NewGuid().ToString();
        //     requestItem.RequestId.TryAdd(newRequestId, 0);
        //     // 请求的第一波数据
        //     await tunnel.ForwardToClientSlim.WaitAsync();
        //     await tunnel.WebSocket.SendAsync(
        //         newRequestId.ToBytes(),
        //         WebSocketMessageType.Binary,
        //         false,
        //         CancellationToken.None
        //     );
        //     await tunnel.WebSocket.SendAsync(
        //         ArraySegment<byte>.Empty,
        //         WebSocketMessageType.Binary,
        //         false,
        //         CancellationToken.None
        //     );
        //     int readCount;
        //     TaskExtend.NewTask(async () =>
        //     {
        //         await Task.Delay(10000);
        //         await socket.TryCloseAsync();
        //     });
        //     while ((readCount = await socketStream.ReadAsync(memory.Memory)) != 0)
        //     {
        //         await tunnel.WebSocket.SendAsync(
        //             memory.Memory[..readCount],
        //             WebSocketMessageType.Binary,
        //             false,
        //             CancellationToken.None
        //         );
        //     }
        // }
        // finally
        // {
        //     await tunnel.WebSocket.SendAsync(
        //         ArraySegment<byte>.Empty,
        //         WebSocketMessageType.Binary,
        //         true,
        //         CancellationToken.None
        //     );
        //     tunnel.ForwardToClientSlim.Release();
        //     Console.WriteLine("连接结束");
        //     await requestItem.CloseAsync(tunnel.ConcurrentDictionary);
        // }
    }
}
