using System.Net.WebSockets;
using System.Text;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Server.WebSocketMessageHandle
{
    public class WebSocketMessageHandle_RegisterTunnel(TunnelContext _tunnelContext)
        : IWebSocketMessageHandle
    {
        public async Task HandleAsync(WebSocket webSocket, string data)
        {
            var dataModel = JsonConvert.DeserializeObject<RegisterTunnelModel>(data)!;
            var appConfig = HostApp.GetConfig<AppConfig>();
            if (dataModel.Token != appConfig.Token)
            {
                throw new Exception();
            }
            var newTunne = new TunnelModel
            {
                CancellationTokenSource = new CancellationTokenSource(),
                WebSocket = webSocket,
                FileSharingPath = dataModel.FileSharingPath,
                ListenPort = dataModel.ListenPort,
                Type = dataModel.Type
            };
            newTunne.PulseCheck = new Timer(
                async _ =>
                {
                    try
                    {
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
                            WebSocketMessageType.Text,
                            true,
                            newTunne.CancellationTokenSource.Token
                        );
                    }
                    catch
                    {
                        await newTunne.CancellationTokenSource.CancelAsync();
                        await newTunne.PulseCheck.DisposeAsync();
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.Empty,
                            string.Empty,
                            CancellationToken.None
                        );
                        foreach (var item in newTunne.SubConnect)
                        {
                            await item.Value.CloseConnectAsync();
                        }
                        await _tunnelContext.RemoveAsync(newTunne);
                    }
                },
                null,
                GlobalStaticConfig.Interval,
                GlobalStaticConfig.Interval
            );
            await _tunnelContext.AddTunnelAsync(newTunne);
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
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            await Task.Delay(GlobalStaticConfig.TenYears, newTunne.CancellationTokenSource.Token);
        }
    }
}
