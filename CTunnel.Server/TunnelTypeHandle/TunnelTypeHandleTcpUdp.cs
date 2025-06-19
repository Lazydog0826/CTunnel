using CTunnel.Server.SocketHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.Extensions.DependencyInjection;
using MiniComp.Autofac;
using MiniComp.Core.App;

namespace CTunnel.Server.TunnelTypeHandle;

[AutofacDependency(typeof(ITunnelTypeHandle), ServiceKey = nameof(TunnelTypeEnum.Tcp))]
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
            await tunnel.WebSocket.SendMessageAsync(
                WebSocketMessageTypeEnum.ConnectionSuccessful,
                string.Empty
            );
            Output.Print($"{tunnel.Key} - 注册隧道成功");
        }
        else
        {
            const string msg = "端口重复";
            await tunnel.WebSocket.SendMessageAsync(WebSocketMessageTypeEnum.ConnectionFail, msg);
            throw new Exception(msg);
        }
    }
}
