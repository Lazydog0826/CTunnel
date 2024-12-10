using System.Net.WebSockets;

namespace CTunnel.Client.MessageHandle
{
    public interface IMessageHandle
    {
        public Task HandleAsync(WebSocket webSocket, byte[] bytes, int bytesCount);
    }
}
