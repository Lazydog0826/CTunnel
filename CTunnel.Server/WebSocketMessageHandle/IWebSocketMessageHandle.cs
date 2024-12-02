using System.Net.WebSockets;

namespace CTunnel.Server.WebSocketMessageHandle
{
    public interface IWebSocketMessageHandle
    {
        public Task HandleAsync(WebSocket webSocket, string data);
    }
}
