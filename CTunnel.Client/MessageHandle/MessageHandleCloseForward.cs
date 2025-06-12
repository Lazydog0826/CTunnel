using System.Net.WebSockets;
using System.Text;
using Microsoft.IO;

namespace CTunnel.Client.MessageHandle;

public class MessageHandleCloseForward(AppConfig appConfig) : IMessageHandle
{
    public async Task HandleAsync(WebSocket webSocket, RecyclableMemoryStream stream)
    {
        var requestId = Encoding.UTF8.GetString(stream.GetMemory()[1..37].Span);
        if (appConfig.ConcurrentDictionary.TryGetValue(requestId, out var ri))
        {
            await ri.CloseAsync(appConfig.ConcurrentDictionary);
        }
    }
}
