using System.Net.WebSockets;

namespace CTunnel.Share.Expand
{
    public static class StreamExtend
    {
        public static async Task ForwardAsync(
            this Stream stream,
            WebSocket webSocket,
            CancellationToken token
        )
        {
            var memory = new Memory<byte>(new byte[1024 * 1024]);
            int count;
            while ((count = await stream.ReadAsync(memory, token)) > 0)
            {
                var bytes = memory[..count].ToArray();
                await webSocket.SendAsync(bytes, WebSocketMessageType.Binary, true, token);
            }
        }

        public static async Task ForwardAsync(
            this WebSocket webSocket,
            Stream stream,
            CancellationToken token
        )
        {
            var memory = new Memory<byte>(new byte[1024 * 1024]);
            while (true)
            {
                var res = await webSocket.ReceiveAsync(memory, token);
                var bytes = memory[..res.Count].ToArray();
                if (res.Count > 1 || bytes[0] != 0)
                {
                    await stream.WriteAsync(bytes, token);
                }
            }
        }
    }
}
