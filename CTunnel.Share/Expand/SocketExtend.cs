﻿using System.Buffers;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using MiniComp.Core.App;
using MiniComp.Core.Extension;

namespace CTunnel.Share.Expand;

public static partial class SocketExtend
{
    /// <summary>
    /// 尝试关闭
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    public static async Task TryCloseAsync(this Socket? socket)
    {
        if (socket is { Connected: true })
        {
            try
            {
                socket.Close();
                await Task.CompletedTask;
            }
            catch
            {
                // ignored
            }
        }
    }

    /// <summary>
    /// 尝试关闭
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static async Task TryCloseAsync(this WebSocket? webSocket, string? msg = null)
    {
        if (webSocket is { State: WebSocketState.Open })
        {
            try
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.InternalServerError,
                    msg,
                    CancellationToken.None
                );
                webSocket.Abort();
            }
            catch
            {
                // ignored
            }
        }
    }

    /// <summary>
    /// 处理数据流
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="useTls"></param>
    /// <param name="isServer"></param>
    /// <param name="targetHost"></param>
    /// <returns></returns>
    public static async Task<Stream> GetStreamAsync(
        this Socket socket,
        bool useTls,
        bool isServer,
        string targetHost
    )
    {
        var stream = new NetworkStream(socket);
        if (useTls)
        {
            // 使用安全套接字层（SSL）安全协议，忽略证书验证
            var ssl = new SslStream(stream, false, (_, _, _, _) => true);
            try
            {
                if (isServer)
                {
                    var x509 = HostApp.RootServiceProvider.GetRequiredService<X509Certificate2>();
                    await ssl.AuthenticateAsServerAsync(
                        x509,
                        false,
                        SslProtocols.Tls12 | SslProtocols.Tls13,
                        false
                    );
                }
                else
                {
                    await ssl.AuthenticateAsClientAsync(targetHost);
                }
            }
            catch (AuthenticationException ex)
            {
                Output.Print(ex.Message, OutputMessageTypeEnum.Error);
                await socket.TryCloseAsync();
                throw;
            }

            return ssl;
        }
        return stream;
    }

    /// <summary>
    /// 解析WEB请求
    /// </summary>
    /// <param name="memory"></param>
    /// <returns></returns>
    public static string ParseWebRequest(this Memory<byte> memory)
    {
        var message = Encoding.Default.GetString(memory.Span);
        var hostRegex = HostRegex();
        var match = hostRegex.Match(message);
        if (string.IsNullOrWhiteSpace(match.Value))
            return string.Empty;
        var replaceReg = HostReplaceRegex();
        var uriBuilder = new UriBuilder(replaceReg.Replace(match.Value, string.Empty));
        var host = uriBuilder.Host.IsNullOrGivenValue(uriBuilder.Scheme);
        return host;
    }

    [GeneratedRegex(@"Host:(\s?)(.+)", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex HostRegex();

    [GeneratedRegex(@"Host:(\s?)", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex HostReplaceRegex();

    /// <summary>
    /// 转发
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="cancellation"></param>
    /// =
    public static async Task ForwardAsync(
        Stream source,
        Stream target,
        CancellationToken cancellation
    )
    {
        using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
        int readCount;
        while ((readCount = await source.ReadAsync(memory.Memory, cancellation)) > 0)
        {
            await target.WriteAsync(memory.Memory[..readCount], cancellation);
        }
    }
}
