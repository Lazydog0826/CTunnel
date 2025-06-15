using System.Buffers;
using System.Text;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Dm.util;
using MiniComp.Core.Extension;

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
            var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
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
        throw new Exception("注册失败，域名重复");
    }
}
