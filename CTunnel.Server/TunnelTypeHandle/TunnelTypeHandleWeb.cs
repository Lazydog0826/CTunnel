using CTunnel.Share;
using CTunnel.Share.Model;

namespace CTunnel.Server.TunnelTypeHandle;

public class TunnelTypeHandleWeb(TunnelContext tunnelContext) : ITunnelTypeHandle
{
    public Task HandleAsync(TunnelModel tunnel)
    {
        // Web类型将域名作为KEY
        tunnel.Key = tunnel.DomainName;
        tunnel.IsAdd = tunnelContext.AddTunnel(tunnel);
        if (tunnel.IsAdd)
        {
            Output.Print($"{tunnel.Key} - 注册隧道成功");
        }
        else
        {
            throw new Exception("注册失败，域名重复");
        }
        return Task.CompletedTask;
    }
}
