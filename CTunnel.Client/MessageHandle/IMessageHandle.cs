using System.Collections.Concurrent;
using System.Net.WebSockets;
using CTunnel.Share.Model;

namespace CTunnel.Client.MessageHandle
{
    public interface IMessageHandle
    {
        public Task HandleAsync(
            WebSocket webSocket,
            byte[] bytes,
            int bytesCount,
            AppConfig appConfig,
            ConcurrentDictionary<string, RequestItem> pairs,
            SemaphoreSlim slim
        );
    }
}
