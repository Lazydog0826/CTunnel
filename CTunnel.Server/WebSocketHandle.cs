using System.Net.WebSockets;
using CTunnel.Server.TunnelTypeHandle;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server
{
    public class WebSocketHandle(AppConfig _appConfig)
    {
        public async Task HandleAsync(HttpListenerWebSocketContext socketContext)
        {
            var webSocket = socketContext.WebSocket;
            var timeout = new CancellationTokenSource(GlobalStaticConfig.Timeout);

            // 获取隧道注册信息
            var registerTunnelParam = await webSocket.ReadMessageAsync<RegisterTunnel>(
                timeout.Token
            );
            // 检查Token
            if (registerTunnelParam.Token != _appConfig.Token)
            {
                await webSocket.SendMessageAsync(
                    new SocketResult { Success = false, Message = "Token无效" }
                );
                await webSocket.TryCloseAsync();
                return;
            }
            var newTimmel = new TunnelModel
            {
                DomainName = registerTunnelParam.DomainName,
                Type = registerTunnelParam.Type,
                ListenPort = registerTunnelParam.ListenPort,
                WebSocket = webSocket
            };

            // 根据隧道类型调用服务
            var tunnelTypeHandle = ServiceContainer.GetService<ITunnelTypeHandle>(
                registerTunnelParam.Type.ToString()
            );
            await tunnelTypeHandle.HandleAsync(newTimmel);
        }
    }
}
