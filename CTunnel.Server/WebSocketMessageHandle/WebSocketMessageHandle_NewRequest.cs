using System.Net.WebSockets;
using System.Text;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Server.WebSocketMessageHandle
{
    /// <summary>
    /// 类型是处理WEB请求的WebSocket
    /// </summary>
    /// <param name="_tunnelContext"></param>
    public class WebSocketMessageHandle_NewRequest(TunnelContext _tunnelContext)
        : IWebSocketMessageHandle
    {
        public async Task HandleAsync(WebSocket webSocket, string data)
        {
            // 使用绑定的域名获取隧道，然后使用RequestId获取到子链接
            var model = JsonConvert.DeserializeObject<NewRequestModel>(data)!;
            var tunnel = _tunnelContext.GetTunnel(model.DomainName);
            if (tunnel == null)
            {
                await webSocket.TryCloseAsync();
                return;
            }
            tunnel.SubConnect.TryGetValue(model.RequestId, out var sub);
            if (sub == null)
            {
                await webSocket.TryCloseAsync();
                return;
            }

            // 发送确定连接消息到客户端
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
                WebSocketMessageType.Binary,
                true,
                tunnel.CancellationTokenSource.Token
            );

            // 最后触发事件
            await sub.TriggerEventAsync(webSocket);
        }
    }
}
