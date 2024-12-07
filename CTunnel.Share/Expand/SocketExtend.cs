using System.Buffers;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using CTunnel.Share.Enums;
using Newtonsoft.Json;

namespace CTunnel.Share.Expand
{
    public static class SocketExtend
    {
        private static readonly SemaphoreSlim slim = new(1);

        /// <summary>
        /// 尝试关闭
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static async Task TryCloseAsync(this Socket? socket)
        {
            if (socket != null)
            {
                try
                {
                    socket.Close();
                    await Task.CompletedTask;
                }
                catch { }
            }
        }

        /// <summary>
        /// 尝试关闭
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 处理数据流
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="useTLS"></param>
        /// <param name="isServer"></param>
        /// <param name="targetHost"></param>
        /// <returns></returns>
        public static async Task<Stream> GetStreamAsync(
            this Socket socket,
            bool useTLS,
            bool isServer,
            string targetHost
        )
        {
            var s = new NetworkStream(socket);
            if (useTLS)
            {
                var x509 = ServiceContainer.GetService<X509Certificate2>();
                var ssl = new SslStream(
                    s,
                    false,
                    new RemoteCertificateValidationCallback((_, _, _, _) => true)
                );
                if (isServer)
                {
                    try
                    {
                        await ssl.AuthenticateAsServerAsync(x509, false, SslProtocols.Tls13, true);
                    }
                    catch (AuthenticationException)
                    {
                        await socket.TryCloseAsync();
                        throw;
                    }
                }
                else
                {
                    await ssl.AuthenticateAsClientAsync(targetHost);
                }
                return ssl;
            }
            return s;
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        public static async Task<T> ReadMessageAsync<T>(
            this WebSocket webSocket,
            CancellationToken cancellationToken
        )
        {
            var buffer = ArrayPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
            try
            {
                var count = await webSocket.ReceiveAsync(
                    new Memory<byte>(buffer),
                    cancellationToken
                );
                var obj = buffer.AsSpan(0, count.Count).ConvertModel<T>();
                return obj;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static async Task SendMessageAsync(this WebSocket webSocket, object obj)
        {
            await slim.WaitAsync();
            try
            {
                await Task.CompletedTask;
                var json = JsonConvert.SerializeObject(obj);
                var bytes = Encoding.UTF8.GetBytes(json);
                await webSocket.SendAsync(
                    bytes,
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None
                );
            }
            finally
            {
                slim.Release();
            }
        }

        /// <summary>
        /// 转发逻辑
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="messageType"></param>
        /// <param name="requestId"></param>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static async Task ForwardAsync(
            this WebSocket webSocket,
            MessageTypeEnum messageType,
            string requestId,
            byte[] bytes,
            int offset,
            int count
        )
        {
            await slim.WaitAsync();
            try
            {
                using var ms = new MemoryStream();
                ms.Write([(byte)messageType]);
                ms.Write(Encoding.UTF8.GetBytes(requestId));
                ms.Write(bytes, offset, count);
                ms.Seek(0, SeekOrigin.Begin);
                await webSocket.SendAsync(
                    ms.ToArray(),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None
                );
            }
            finally
            {
                slim.Release();
            }
        }

        /// <summary>
        /// 解析WEB请求
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<(string, int)> ParseWebRequestAsync(
            this Stream stream,
            byte[] buffer
        )
        {
            var count = await stream.ReadAsync(new Memory<byte>(buffer));
            var message = Encoding.UTF8.GetString(buffer[..count]);
            var reg = new Regex("Host:\\s(.+)", RegexOptions.Multiline);
            var match = reg.Match(message);
            if (string.IsNullOrWhiteSpace(match.Value))
            {
                return (string.Empty, 0);
            }
            var uriBuilder = new UriBuilder(match.Value.Replace("Host: ", string.Empty));
            var host = uriBuilder.Host;
            return (host, count);
        }
    }
}
