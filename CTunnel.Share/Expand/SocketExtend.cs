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
using MiniComp.Core.App;
using Newtonsoft.Json;

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
        if (socket != null)
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
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<T> ReadMessageAsync<T>(
        this WebSocket webSocket,
        CancellationToken cancellationToken
    )
    {
        // 源Stream
        await using var sourceStream = GlobalStaticConfig.MsManager.GetStream();
        using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
        while (true)
        {
            var receiveRes = await webSocket.ReceiveAsync(memory.Memory, cancellationToken);
            await sourceStream.WriteAsync(memory.Memory[..receiveRes.Count], cancellationToken);
            if (receiveRes.EndOfMessage)
                break;
        }
        sourceStream.Seek(0, SeekOrigin.Begin);
        // 解压缩后的Stream
        await using var decompressStream = await sourceStream.GetMemory().DecompressAsync();
        return decompressStream.GetMemory().ConvertModel<T>();
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="obj"></param>
    /// <param name="slim"></param>
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
            await using var compressStream = await bytes.AsMemory(0, bytes.Length).CompressAsync();
            await webSocket.SendAsync(
                compressStream.GetMemory(),
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
            ms.Write(requestId.Span);
            ms.Write(bytes.Span);
            ms.Seek(0, SeekOrigin.Begin);
            await using var compressStream = await ms.GetMemory().CompressAsync();
            await webSocket.SendAsync(
                compressStream.GetMemory(),
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
    /// <returns></returns>
    public static async Task<string> ParseWebRequestAsync(this Stream stream)
    {
        await using var ms = GlobalStaticConfig.MsManager.GetStream();
        await stream.CopyToAsync(ms);
        var message = Encoding.UTF8.GetString(ms.GetMemory().Span);
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
}
