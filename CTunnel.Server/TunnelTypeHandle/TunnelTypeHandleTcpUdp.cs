using CTunnel.Server.SocketHandle;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.Extensions.DependencyInjection;
using MiniComp.Core.App;

namespace CTunnel.Server.TunnelTypeHandle;

public class TunnelTypeHandleTcpUdp(TunnelContext tunnelContext) : ITunnelTypeHandle
{
    public Task HandleAsync(TunnelModel tunnel)
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
        }
        else
        {
            throw new Exception("注册失败，端口重复");
        }
        return Task.CompletedTask;
    }
}
