using System.Buffers;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using CTunnel.Share.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using MiniComp.Core.App;

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
                    await ssl.AuthenticateAsServerAsync(x509, false, SslProtocols.Tls13, true);
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
    /// 转发逻辑
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="messageType"></param>
    /// <param name="requestId"></param>
    /// <param name="bytes"></param>
    /// <param name="slim"></param>
    /// <returns></returns>
    public static async Task ForwardAsync(
        this WebSocket webSocket,
        MessageTypeEnum messageType,
        Memory<byte> requestId,
        Memory<byte> bytes,
        SemaphoreSlim slim
    )
    {
        await slim.WaitAsync();
        try
        {
            await using var ms = GlobalStaticConfig.MsManager.GetStream();
            ms.Write([(byte)messageType]);
            await ms.WriteAsync(requestId);
            await ms.WriteAsync(bytes);
            ms.Seek(0, SeekOrigin.Begin);
            var msCount = 1 + requestId.Length + bytes.Length;
            await webSocket.SendAsync(
                ms.GetMemory()[..msCount],
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
    /// <param name="memory"></param>
    /// <returns></returns>
    public static string ParseWebRequest(this Memory<byte> memory)
    {
        var message = Encoding.UTF8.GetString(memory.Span);
        var hostRegex = HostRegex();
        var match = hostRegex.Match(message);
        if (string.IsNullOrWhiteSpace(match.Value))
            return string.Empty;
        var replaceReg = HostReplaceRegex();
        var uriBuilder = new UriBuilder(replaceReg.Replace(match.Value, string.Empty));
        var host = uriBuilder.Host;
        return host;
    }

    [GeneratedRegex(@"Host:(\s?)(.+)", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex HostRegex();

    [GeneratedRegex(@"Host:(\s?)", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex HostReplaceRegex();

    /// <summary>
    /// 分片写入
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="stream2"></param>
    /// <param name="start"></param>
    public static async Task ShardWriteAsync(
        this Stream stream,
        RecyclableMemoryStream stream2,
        int start
    )
    {
        using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
        stream2.Seek(start, SeekOrigin.Begin);
        int readCount;
        while ((readCount = await stream2.ReadAsync(memory.Memory)) != 0)
        {
            await stream.WriteAsync(memory.Memory[..readCount]);
        }
    }
}
