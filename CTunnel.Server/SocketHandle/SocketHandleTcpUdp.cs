using System.Buffers;
using System.Net.Sockets;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server.SocketHandle;

public class SocketHandleTcpUdp(TunnelContext tunnelContext) : ISocketHandle
{
    public async Task HandleAsync(Socket socket, int port)
    {
        // socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        // var socketStream = await socket.GetStreamAsync(false, true, string.Empty);
        // using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
        // var requestItem = new RequestItem()
        // {
        //     RequestId = Guid.NewGuid().ToString(),
        //     TargetSocket = socket,
        //     TargetSocketStream = socketStream,
        // };
        // var tunnel = tunnelContext.GetTunnel(port.ToString());
        // if (tunnel == null)
        //     return;
        // tunnel.ConcurrentDictionary.TryAdd(requestItem.RequestId, requestItem);
        // try
        // {
        //     await ForwardToTunnelAsync(Memory<byte>.Empty);
        //     int readCount;
        //     while ((readCount = await socketStream.ReadAsync(memory.Memory)) != 0)
        //     {
        //         await ForwardToTunnelAsync(memory.Memory[..readCount]);
        //     }
        // }
        // finally
        // {
        //     await requestItem.CloseAsync(tunnel.ConcurrentDictionary);
        // }
        //
        // return;
        //
        // async Task ForwardToTunnelAsync(Memory<byte> temMemory)
        // {
        //     await tunnel.WebSocket.ForwardAsync(
        //         requestItem.RequestId.ToBytes(),
        //         temMemory,
        //         tunnel.ForwardToClientSlim
        //     );
        // }
    }
}
