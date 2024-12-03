using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server.TunnelTypeHandle
{
    public class TunnelTypeHandle_Web(TunnelContext _tunnelContext) : ITunnelTypeHandle
    {
        public async Task HandleAsync(TunnelModel tunnel)
        {
            if (!await _tunnelContext.AddTunnelAsync(tunnel))
            {
                await tunnel.MasterSocketStream.SendSocketResultAsync(
                    WebSocketMessageTypeEnum.RegisterTunnel,
                    false,
                    "域名重复",
                    CancellationToken.None
                );
                await tunnel.MasterSocket.TryCloseAsync();
                return;
            }
            await tunnel.MasterSocketStream.SendSocketResultAsync(
                WebSocketMessageTypeEnum.RegisterTunnel,
                true,
                "成功",
                CancellationToken.None
            );
            Log.Write($"隧道已添加 {tunnel.DomainName}", LogType.Success);
            await tunnel.MasterSocketStream.LoopReadMessageAsync<object>(
                _ => Task.CompletedTask,
                tunnel.CancellationTokenSource.Token
            );
        }
    }
}
