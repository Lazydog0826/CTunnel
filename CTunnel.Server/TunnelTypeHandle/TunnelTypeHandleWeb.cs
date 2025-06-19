using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using MiniComp.Autofac;

namespace CTunnel.Server.TunnelTypeHandle;

[AutofacDependency(typeof(ITunnelTypeHandle), ServiceKey = nameof(TunnelTypeEnum.Web))]
public class TunnelTypeHandleWeb(TunnelContext tunnelContext) : ITunnelTypeHandle
{
    public async Task HandleAsync(TunnelModel tunnel)
    {
        // Web类型将域名作为KEY
        tunnel.Key = tunnel.DomainName;
        tunnel.IsAdd = tunnelContext.AddTunnel(tunnel);
        if (tunnel.IsAdd)
        {
            await tunnel.WebSocket.SendMessageAsync(
                WebSocketMessageTypeEnum.ConnectionSuccessful,
                string.Empty
            );
        }
        else
        {
            throw new Exception("域名重复");
        }
    }
}
