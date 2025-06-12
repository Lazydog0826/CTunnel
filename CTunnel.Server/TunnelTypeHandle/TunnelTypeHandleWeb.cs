using System.Buffers;
using System.Text;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server.TunnelTypeHandle;

public class TunnelTypeHandleWeb(TunnelContext tunnelContext) : ITunnelTypeHandle
{
    public async Task HandleAsync(TunnelModel tunnel)
    {
        // Web类型将域名作为KEY
        tunnel.Key = tunnel.DomainName;
        tunnel.IsAdd = tunnelContext.AddTunnel(tunnel);
        if (tunnel.IsAdd)
        {
            Output.Print($"{tunnel.Key} - 注册隧道成功");
            await using var ms = GlobalStaticConfig.MsManager.GetStream();
            using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize + 37);
            while (!tunnel.CancellationTokenSource.IsCancellationRequested)
            {
                var res = await tunnel.WebSocket.ReceiveAsync(
                    memory.Memory,
                    tunnel.CancellationTokenSource.Token
                );
                await ms.WriteAsync(memory.Memory);
                if (res.EndOfMessage)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    if (Enum.IsDefined(typeof(MessageTypeEnum), ms.GetMemory().Span[0]))
                    {
                        var requestId = Encoding.UTF8.GetString(ms.GetMemory()[1..37].Span);
                        var ri = tunnel.GetRequestItem(requestId);
                        if (ri != null)
                        {
                            await ri.TargetSocketStream.WriteAsync(ms.GetMemory()[37..]);
                        }
                        else
                        {
                            await tunnel.WebSocket.ForwardAsync(
                                MessageTypeEnum.CloseForward,
                                requestId.ToBytes(),
                                Memory<byte>.Empty,
                                tunnel.Slim
                            );
                        }
                    }
                    ms.Reset();
                }
            }
        }
        else
        {
            throw new Exception("注册失败，域名重复");
        }
    }
}
