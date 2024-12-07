using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using CTunnel.Share.Model;

namespace CTunnel.Client.MessageHandle
{
    public class MessageHandle_CloseForward : IMessageHandle
    {
        public async Task HandleAsync(
            WebSocket webSocket,
            byte[] bytes,
            int bytesCount,
            AppConfig appConfig,
            ConcurrentDictionary<string, RequestItem> pairs
        )
        {
            var requestId = Encoding.UTF8.GetString(bytes.AsSpan(1, 36));
            if (pairs.TryGetValue(requestId, out var ri))
            {
                await ri.CloseAllAsync(pairs);
            }
        }
    }
}
