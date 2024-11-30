using System.Collections.Concurrent;
using System.Net.WebSockets;
using CTunnel.Core.Model;
using Microsoft.AspNetCore.Http;

namespace CTunnel.Core
{
    public class TunnelContext
    {
        private readonly ConcurrentDictionary<string, TunnelModel> _tunnels = [];

        public async Task NewWebSocketClientAsync(WebSocketManager webSocketManager)
        {
            var socket = await webSocketManager.AcceptWebSocketAsync();
            var newTunnel = new TunnelModel { Id = Guid.NewGuid().ToString(), WebSocket = socket };
            if (_tunnels.TryAdd(newTunnel.Id, newTunnel)) 
            {

            }
            else
            {
                await socket.CloseOutputAsync(
                    WebSocketCloseStatus.Empty,
                    string.Empty,
                    CancellationToken.None
                );
            }
        }
    }
}
