using System.Net.WebSockets;
using System.Text;
using CTunnel.Share.Enums;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Server.WebSocketMessageHandle
{
    public class WebSocketMessageHandle_NewRequest(TunnelContext _tunnelContext)
        : IWebSocketMessageHandle
    {
        public async Task HandleAsync(WebSocket webSocket, string data)
        {
            var model = JsonConvert.DeserializeObject<NewRequestModel>(data)!;
            var tunnel = _tunnelContext.GetTunnel(model.DomainName);
            if (tunnel == null)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.Empty,
                    string.Empty,
                    CancellationToken.None
                );
                return;
            }
            tunnel.SubConnect.TryGetValue(model.RequestId, out var sub);
            if (sub == null)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.Empty,
                    string.Empty,
                    CancellationToken.None
                );
                return;
            }
            await webSocket.SendAsync(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new WebSocketMessageModel
                        {
                            MessageType = WebSocketMessageTypeEnum.NewRequest,
                            JsonData = string.Empty
                        }
                    )
                ),
                WebSocketMessageType.Text,
                true,
                tunnel.CancellationTokenSource.Token
            );
            await sub.TriggerEventAsync(webSocket);
        }
    }
}
