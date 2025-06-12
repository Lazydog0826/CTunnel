using System.Net.WebSockets;
using Microsoft.IO;

namespace CTunnel.Client.MessageHandle;

public interface IMessageHandle
{
    public Task HandleAsync(WebSocket webSocket, RecyclableMemoryStream stream);
}
