using System.Net.WebSockets;
using CTunnel.Share;
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
            Log.Write($"NewRequest:{data}");
            Log.Write(data);
            var tunnel = _tunnelContext.GetTunnel(model.DomainName);
            if (tunnel == null)
            {
                await webSocket.SendResponseMessageAsync(
                    "未找到隧道",
                    false,
                    WebSocketMessageTypeEnum.NewRequest,
                    CancellationToken.None
                );
                await webSocket.TryCloseAsync();
                Log.Write("NewRequest:未找到隧道");
                return;
            }
            tunnel.SubConnect.TryGetValue(model.RequestId, out var sub);
            if (sub == null)
            {
                await webSocket.SendResponseMessageAsync(
                    "请求未找到",
                    false,
                    WebSocketMessageTypeEnum.NewRequest,
                    CancellationToken.None
                );
                await webSocket.TryCloseAsync();
                Log.Write("NewRequest:请求未找到");
                return;
            }

            // 发送确定连接消息到客户端
            await webSocket.SendResponseMessageAsync(
                "成功",
                true,
                WebSocketMessageTypeEnum.NewRequest,
                CancellationToken.None
            );

            // 最后触发事件
            await sub.TriggerEventAsync(webSocket);
        }
    }
}
