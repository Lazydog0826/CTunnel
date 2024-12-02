using System.Net.WebSockets;
using System.Text;
using CTunnel.Share.Enums;
using CTunnel.Share.Model;
using Newtonsoft.Json;

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
                    await webSocket.CloseOutputAsync(
                        WebSocketCloseStatus.Empty,
                        string.Empty,
                        CancellationToken.None
                    );
                }
                catch { }
            }
        }

        public static async Task SendResponseMessageAsync(
            this WebSocket webSocket,
            string message,
            bool success,
            WebSocketMessageTypeEnum webSocketMessageType,
            CancellationToken token
        )
        {
            var model = new WebSocketMessageModel
            {
                MessageType = webSocketMessageType,
                JsonData = JsonConvert.SerializeObject(
                    new SuccessFailureModel { Success = success, Message = message, }
                )
            };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
            await webSocket.SendAsync(bytes, WebSocketMessageType.Binary, true, token);
        }

        public static async Task<T> ReadModelAsync<T>(
            this WebSocket webSocket,
            CancellationToken token
        )
        {
            var memory = new Memory<byte>(new byte[1024 * 1024]);
            var readRes = await webSocket.ReceiveAsync(memory, token);
            return memory.ConvertModel<T>(readRes.Count);
        }

        public static async Task PulseCheckAsync(this WebSocket webSocket, CancellationToken token)
        {
            var model = new WebSocketMessageModel
            {
                MessageType = WebSocketMessageTypeEnum.PulseCheck,
                JsonData = string.Empty,
            };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
            await webSocket.SendAsync(bytes, WebSocketMessageType.Binary, true, token);
        }

        public static async Task SendMessageAsync(
            this WebSocket webSocket,
            WebSocketMessageTypeEnum webSocketMessageType,
            object obj,
            CancellationToken token
        )
        {
            var model = new WebSocketMessageModel
            {
                MessageType = webSocketMessageType,
                JsonData = JsonConvert.SerializeObject(obj)
            };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
            await webSocket.SendAsync(bytes, WebSocketMessageType.Binary, true, token);
        }
    }
}
