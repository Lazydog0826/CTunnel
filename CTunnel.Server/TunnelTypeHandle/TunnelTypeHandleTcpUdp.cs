using System.Buffers;
using System.Text;
using CTunnel.Server.SocketHandle;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.Extensions.DependencyInjection;
using MiniComp.Core.App;

namespace CTunnel.Server.TunnelTypeHandle;

public class TunnelTypeHandleTcpUdp(TunnelContext tunnelContext) : ITunnelTypeHandle
{
    public async Task HandleAsync(TunnelModel tunnel)
    {
        // Tcp和Udp的key为监听的端口
        tunnel.Key = tunnel.ListenPort.ToString();
        tunnel.IsAdd = tunnelContext.AddTunnel(tunnel);
        if (tunnel.IsAdd)
        {
            // 开启端口监听
            var socketHandle = HostApp.RootServiceProvider.GetRequiredKeyedService<ISocketHandle>(
                "TcpUdp"
            );
            tunnel.ListenSocket = SocketListen.CreateSocketListen(
                tunnel.Type.ToProtocolType(),
                tunnel.ListenPort,
                socketHandle
            );
            Output.Print($"{tunnel.Key} - 注册隧道成功");
            await using var ms = GlobalStaticConfig.MsManager.GetStream();
            using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize + 37);
            RequestItem? requestItem = null;
            var requestId = string.Empty;
            while (true)
            {
                var readCount = await tunnel.WebSocket.ReceiveAsync(
                    memory.Memory,
                    CancellationToken.None
                );
                if (requestItem == null)
                {
                    if (string.IsNullOrWhiteSpace(requestId))
                    {
                        requestId = Encoding.Default.GetString(
                            memory.Memory[..readCount.Count].Span
                        );
                        requestItem = tunnel.GetRequestItem(requestId);
                    }
                }
                else
                {
                    await requestItem.TargetSocketStream.WriteAsync(
                        memory.Memory[..readCount.Count]
                    );
                }
                if (readCount.EndOfMessage)
                {
                    requestItem = null;
                    requestId = string.Empty;
                }
            }
        }
        throw new Exception("注册失败，端口重复");
    }
}
