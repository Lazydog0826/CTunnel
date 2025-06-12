using System.Net.WebSockets;
using System.Text;

namespace CTunnel.Client.MessageHandle;

public class MessageHandle_CloseForward(AppConfig appConfig) : IMessageHandle
{
    public async Task HandleAsync(WebSocket webSocket, byte[] bytes, int bytesCount)
    {
        var requestId = Encoding.UTF8.GetString(bytes.AsSpan(1, 36));
        if (appConfig.ConcurrentDictionary.TryGetValue(requestId, out var ri))
        {
            await ri.CloseAsync(appConfig.ConcurrentDictionary);
        }
    }
}
