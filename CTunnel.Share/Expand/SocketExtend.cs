using System.Buffers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using CTunnel.Share.Enums;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Share.Expand
{
    public static class SocketExtend
    {
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
                        Log.Write("SSL 握手失败", LogType.Error);
                        throw;
                    }
                }
                else
                {
                    await ssl.AuthenticateAsClientAsync(targetHost);
                }
                Log.Write("证书握手成功", LogType.Success);
                return ssl;
            }
            return s;
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<T> ReadMessageAsync<T>(this Stream stream, CancellationToken token)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
            try
            {
                var count = await stream.ReadAsync(new Memory<byte>(buffer), token);
                var obj = buffer.AsSpan(0, count).ConvertModel<T>();
                return obj;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 循环接收
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task LoopReadMessageAsync<T>(
            this Stream stream,
            Func<T, Task> func,
            CancellationToken token
        )
        {
            var buffer = ArrayPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
            try
            {
                int count;
                while ((count = await stream.ReadAsync(new Memory<byte>(buffer), token)) != 0)
                {
                    if (buffer[0] != 0x00)
                    {
                        var obj = buffer.AsSpan(0, count).ConvertModel<T>();
                        _ = func.Invoke(obj);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static async Task SendMessageAsync(
            this Stream stream,
            object obj,
            CancellationToken token
        )
        {
            var json = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(bytes, token);
        }

        /// <summary>
        /// 发送心跳包
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task SendHeartbeatPacketAsync(
            this Stream stream,
            CancellationToken token
        )
        {
            await stream.WriteAsync(new byte[] { 0x00 }, token);
        }

        /// <summary>
        /// 转发逻辑
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static async Task ForwardAsync(
            this Stream source,
            Stream destination,
            CancellationToken cancellationToken
        )
        {
            var buffer = ArrayPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
            try
            {
                int bytesRead;
                while (
                    (
                        bytesRead = await source
                            .ReadAsync(new Memory<byte>(buffer), cancellationToken)
                            .ConfigureAwait(false)
                    ) != 0
                )
                {
                    if (buffer[0] != 0x00)
                    {
                        await destination
                            .WriteAsync(
                                new ReadOnlyMemory<byte>(buffer, 0, bytesRead),
                                cancellationToken
                            )
                            .ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 发送Socket操作结果
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="messageType"></param>
        /// <param name="success"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task SendSocketResultAsync(
            this Stream stream,
            WebSocketMessageTypeEnum messageType,
            bool success,
            string message,
            CancellationToken cancellationToken
        )
        {
            var typeMessage = new SocketTypeMessage
            {
                MessageType = messageType,
                JsonData = JsonConvert.SerializeObject(
                    new SocketResult { Success = success, Message = message }
                )
            };
            await stream.SendMessageAsync(typeMessage, cancellationToken);
        }

        /// <summary>
        /// 解析WEB请求
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<(string, int)> ParseWebRequestAsync(
            this Stream stream,
            byte[] buffer,
            CancellationToken cancellationToken
        )
        {
            var count = await stream.ReadAsync(new Memory<byte>(buffer), cancellationToken);
            var message = Encoding.UTF8.GetString(buffer[..count]);
            var reg = new Regex("Host:\\s(.+)", RegexOptions.Multiline);
            var match = reg.Match(message);
            if (string.IsNullOrWhiteSpace(match.Value))
            {
                return (string.Empty, 0);
            }
            var uriBuilder = new UriBuilder(match.Value.Replace("Host: ", string.Empty));
            var host = uriBuilder.Host;
            Log.Write($"外部请求 {host}", LogType.Important);
            return (host, count);
        }
    }
}
