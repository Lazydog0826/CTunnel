using System.Net.WebSockets;

namespace CTunnel.Server.WebSocketMessageHandle
{
    /// <summary>
    /// 心跳包处理空方法
    /// </summary>
    public class WebSocketMessageHandle_PulseCheck : IWebSocketMessageHandle
    {
        public async Task HandleAsync(WebSocket webSocket, string data)
        {
            await Task.CompletedTask;
        }
    }
}
