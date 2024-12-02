using System.Net.WebSockets;
using System.Text;
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
            // 验证TOKEN对不对
            var dataModel = JsonConvert.DeserializeObject<RegisterTunnelModel>(data)!;
            var appConfig = HostApp.GetConfig<AppConfig>();
            if (dataModel.Token != appConfig.Token)
            {
                throw new Exception();
            }

            // 新建隧道模型
            var newTunne = new TunnelModel
            {
                CancellationTokenSource = new CancellationTokenSource(),
                WebSocket = webSocket,
                FileSharingPath = dataModel.FileSharingPath,
                ListenPort = dataModel.ListenPort,
                Type = dataModel.Type,
                DomainName = dataModel.DomainName
            };

            // 创建心跳检查计时器
            newTunne.PulseCheck = new Timer(
                async _ =>
                {
                    try
                    {
                        // 发送心跳包
                        // 任何报错代表连接已经断开
                        var message = JsonConvert.SerializeObject(
                            new WebSocketMessageModel
                            {
                                JsonData = string.Empty,
                                MessageType = WebSocketMessageTypeEnum.PulseCheck
                            }
                        );
                        var bytes = Encoding.UTF8.GetBytes(message);
                        await webSocket.SendAsync(
                            bytes,
                            WebSocketMessageType.Binary,
                            true,
                            newTunne.CancellationTokenSource.Token
                        );
                    }
                    catch
                    {
                        // 标记取消和关闭计时器
                        await newTunne.CancellationTokenSource.CancelAsync();
                        await newTunne.PulseCheck.DisposeAsync();

                        // 主连接关闭
                        await webSocket.TryCloseAsync();

                        // 子链接全部断开
                        foreach (var item in newTunne.SubConnect)
                        {
                            await item.Value.WebSocket.TryCloseAsync();
                            await item.Value.Socket.TryCloseAsync();
                        }

                        // 最后从上下文中移除当前隧道
                        await _tunnelContext.RemoveAsync(newTunne);
                    }
                },
                null,
                GlobalStaticConfig.Interval,
                GlobalStaticConfig.Interval
            );

            // 添加到隧道上下文
            await _tunnelContext.AddTunnelAsync(newTunne);

            // 发送注册成功消息给客户端
            await webSocket.SendAsync(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new WebSocketMessageModel
                        {
                            MessageType = WebSocketMessageTypeEnum.RegisterTunnel,
                            JsonData = string.Empty
                        }
                    )
                ),
                WebSocketMessageType.Binary,
                true,
                CancellationToken.None
            );

            // 根据隧道类型处理
            await HostApp
                .ServiceProvider.GetRequiredKeyedService<ITunnelHandle>(dataModel.Type.ToString())
                .HandleAsync(newTunne);

            await Task.Delay(Timeout.InfiniteTimeSpan, newTunne.CancellationTokenSource.Token);
        }
    }
}
