using System.Net.WebSockets;
using CTunnel.Server.TunnelHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Server.WebSocketMessageHandle
{
    /// <summary>
    /// 注册隧道消息处理
    /// </summary>
    /// <param name="_tunnelContext"></param>
    public class WebSocketMessageHandle_RegisterTunnel(TunnelContext _tunnelContext)
        : IWebSocketMessageHandle
    {
        public async Task HandleAsync(WebSocket webSocket, string data)
        {
            var registerTunnel = JsonConvert.DeserializeObject<RegisterTunnelModel>(data)!;

            // 新建隧道模型
            var newTunne = new TunnelModel
            {
                CancellationTokenSource = new CancellationTokenSource(),
                WebSocket = webSocket,
                FileSharingPath = registerTunnel.FileSharingPath,
                ListenPort = registerTunnel.ListenPort,
                Type = registerTunnel.Type,
                DomainName = registerTunnel.DomainName
            };

            var appConfig = HostApp.GetConfig<AppConfig>();
            if (registerTunnel.Token != appConfig.Token)
            {
                await webSocket.SendResponseMessageAsync(
                    "Token验证未通过",
                    false,
                    WebSocketMessageTypeEnum.RegisterTunnel,
                    newTunne.CancellationTokenSource.Token
                );
                Log.Write("新的请求连接Token验证未通过");
                await webSocket.TryCloseAsync();
                return;
            }

            // 创建心跳检查计时器
            newTunne.PulseCheck = new Timer(
                async _ =>
                {
                    try
                    {
                        // 发送心跳包，任何报错代表连接已经断开
                        await webSocket.PulseCheckAsync(newTunne.CancellationTokenSource.Token);
                    }
                    catch
                    {
                        // 关闭所有相关内容
                        await newTunne.CloseAllAsync();

                        // 最后从上下文中移除当前隧道
                        await _tunnelContext.RemoveAsync(newTunne);
                    }
                },
                null,
                GlobalStaticConfig.Interval,
                GlobalStaticConfig.Interval
            );

            // 添加到隧道上下文
            var isAdd = await _tunnelContext.AddTunnelAsync(newTunne);
            if (!isAdd)
            {
                await webSocket.SendResponseMessageAsync(
                    "添加隧道失败，域名可能重复",
                    false,
                    WebSocketMessageTypeEnum.RegisterTunnel,
                    newTunne.CancellationTokenSource.Token
                );
                Log.Write("添加隧道失败，域名可能重复");
                await webSocket.TryCloseAsync();
                return;
            }

            // 发送注册成功消息给客户端
            await webSocket.SendResponseMessageAsync(
                "成功",
                true,
                WebSocketMessageTypeEnum.RegisterTunnel,
                newTunne.CancellationTokenSource.Token
            );

            // 根据隧道类型处理
            await HostApp
                .ServiceProvider.GetRequiredKeyedService<ITunnelHandle>(
                    registerTunnel.Type.ToString()
                )
                .HandleAsync(newTunne);
        }
    }
}
