using System.Buffers;
using System.Text;
using CTunnel.Server.SocketHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.Extensions.DependencyInjection;
using MiniComp.Core.App;

namespace CTunnel.Server.TunnelTypeHandle;

public class TunnelTypeHandleTcpUdp(TunnelContext tunnelContext) : ITunnelTypeHandle
{
    public async Task HandleAsync(TunnelModel tunnel)
    {
        // // Tcp和Udp的key为监听的端口
        // tunnel.Key = tunnel.ListenPort.ToString();
        // tunnel.IsAdd = tunnelContext.AddTunnel(tunnel);
        // if (tunnel.IsAdd)
        // {
        //     // 开启端口监听
        //     var socketHandle = HostApp.RootServiceProvider.GetRequiredKeyedService<ISocketHandle>(
        //         "TcpUdp"
        //     );
        //     tunnel.ListenSocket = SocketListen.CreateSocketListen(
        //         tunnel.Type.ToProtocolType(),
        //         tunnel.ListenPort,
        //         socketHandle
        //     );
        //     Output.Print($"{tunnel.Key} - 注册隧道成功");
        //     await using var ms = GlobalStaticConfig.MsManager.GetStream();
        //     using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize + 37);
        //     while (true)
        //     {
        //         var readCount = await tunnel.WebSocket.ReceiveAsync(
        //             memory.Memory,
        //             CancellationToken.None
        //         );
        //         await ms.WriteAsync(memory.Memory[..readCount.Count]);
        //         if (readCount.EndOfMessage)
        //         {
        //             ms.Seek(0, SeekOrigin.Begin);
        //             var requestId = Encoding.UTF8.GetString(ms.GetMemory()[..36].Span);
        //             var ri = tunnel.GetRequestItem(requestId);
        //             if (ri != null)
        //             {
        //                 // 转发给访问者
        //                 await ri.TargetSocketStream.ShardWriteAsync(ms, 36, ri.ForwardToTargetSlim);
        //             }
        //             else
        //             {
        //                 // 找不到通知客户端关闭请求
        //                 await tunnel.WebSocket.ForwardAsync(
        //                     requestId.ToBytes(),
        //                     Memory<byte>.Empty,
        //                     tunnel.ForwardToClientSlim
        //                 );
        //             }
        //             ms.Reset();
        //         }
        //     }
        // }
        // throw new Exception("注册失败，端口重复");
    }
}
