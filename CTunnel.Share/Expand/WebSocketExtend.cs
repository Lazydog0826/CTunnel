using System.Net.WebSockets;

namespace CTunnel.Share.Expand
{
    public static class WebSocketExtend
    {
        public static async Task TryCloseAsync(this WebSocket? webSocket)
        {
            if (webSocket != null)
            {
                try
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.Empty,
                        string.Empty,
                        CancellationToken.None
                    );
                }
                catch { }
            }
        }
    }
}
