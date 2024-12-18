﻿using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using CTunnel.Share.Enums;
using Microsoft.Extensions.DependencyInjection;
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
                    webSocket.Abort();
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
            var stream = new NetworkStream(socket);
            if (useTLS)
            {
                // 使用安全套接字层（SSL）安全协议，忽略证书验证
                var ssl = new SslStream(
                    stream,
                    false,
                    new RemoteCertificateValidationCallback((_, _, _, _) => true)
                );
                try
                {
                    if (isServer)
                    {
                        var x509 =
                            GlobalStaticConfig.ServiceProvider.GetRequiredService<X509Certificate2>();
                        await ssl.AuthenticateAsServerAsync(x509, false, SslProtocols.Tls13, true);
                    }
                    else
                    {
                        await ssl.AuthenticateAsClientAsync(targetHost);
                    }
                }
                catch (AuthenticationException)
                {
                    await socket.TryCloseAsync();
                    throw;
                }

                return ssl;
            }
            return stream;
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
            using var ms = GlobalStaticConfig.MSManager.GetStream();
            T t = default!;
            await BytesExpand.UseBufferAsync(
                GlobalStaticConfig.BufferSize,
                async buffer =>
                {
                    while (true)
                    {
                        var receiveRes = await webSocket.ReceiveAsync(
                            new Memory<byte>(buffer),
                            cancellationToken
                        );
                        await ms.WriteAsync(
                            buffer.AsMemory(0, receiveRes.Count),
                            cancellationToken
                        );
                        if (receiveRes.EndOfMessage)
                        {
                            try
                            {
                                await BytesExpand.UseBufferAsync(
                                    (int)ms.Length,
                                    async buffer2 =>
                                    {
                                        ms.Seek(0, SeekOrigin.Begin);
                                        var buffer2Count = await ms.ReadAsync(buffer2);
                                        await buffer2
                                            .AsMemory(0, buffer2Count)
                                            .DecompressAsync(
                                                async (decompressBuffer, decompressBufferCount) =>
                                                {
                                                    t = decompressBuffer
                                                        .AsMemory(0, decompressBufferCount)
                                                        .ConvertModel<T>();
                                                    await Task.CompletedTask;
                                                }
                                            );
                                    }
                                );
                                break;
                            }
                            catch { }
                        }
                    }
                }
            );
            return t;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static async Task SendMessageAsync(
            this WebSocket webSocket,
            object obj,
            SemaphoreSlim slim
        )
        {
            await slim.WaitAsync();
            try
            {
                var json = JsonConvert.SerializeObject(obj);
                var bytes = Encoding.UTF8.GetBytes(json);
                await bytes
                    .AsMemory(0, bytes.Length)
                    .CompressAsync(
                        async (compressBuffer, compressBufferCount) =>
                        {
                            await webSocket.SendAsync(
                                compressBuffer.AsMemory(0, compressBufferCount),
                                WebSocketMessageType.Binary,
                                true,
                                CancellationToken.None
                            );
                        }
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
            int count,
            SemaphoreSlim slim
        )
        {
            await slim.WaitAsync();
            try
            {
                await BytesExpand.UseBufferAsync(
                    count + 37,
                    async buffer =>
                    {
                        using var ms = GlobalStaticConfig.MSManager.GetStream();
                        ms.Write([(byte)messageType]);
                        ms.Write(Encoding.UTF8.GetBytes(requestId));
                        ms.Write(bytes, offset, count);
                        ms.Seek(0, SeekOrigin.Begin);
                        var readCount = await ms.ReadAsync(new Memory<byte>(buffer));
                        await buffer
                            .AsMemory(0, readCount)
                            .CompressAsync(
                                async (compressBuffer, compressBufferCount) =>
                                {
                                    await webSocket.SendAsync(
                                        compressBuffer.AsMemory(0, compressBufferCount),
                                        WebSocketMessageType.Binary,
                                        true,
                                        CancellationToken.None
                                    );
                                }
                            );
                    }
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

            var reg = new Regex(@"Host:(\s?)(.+)", RegexOptions.IgnoreCase);
            var match = reg.Match(message);
            if (string.IsNullOrWhiteSpace(match.Value))
            {
                return (string.Empty, default);
            }
            var replaceReg = new Regex(@"Host:(\s?)", RegexOptions.IgnoreCase);
            var uriBuilder = new UriBuilder(replaceReg.Replace(match.Value, string.Empty));
            var host = uriBuilder.Host;
            return (host, count);
        }
    }
}
