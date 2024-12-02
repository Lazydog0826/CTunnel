using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server
{
    public partial class WebRequestHandle(TunnelContext _tunnelContext)
    {
        public async Task HandleAsync(Socket socket, X509Certificate2 x509Certificate2)
        {
            // 启用安全套接字
            var sslStream = new SslStream(new NetworkStream(socket), false);
            await sslStream.AuthenticateAsServerAsync(
                x509Certificate2,
                false,
                SslProtocols.Tls13,
                true
            );
            // 读取请求数据匹配HOST分配给不同的隧道
            var memory = new Memory<byte>(new byte[1024 * 1024]);
            var count = await sslStream.ReadAsync(memory);
            var message = Encoding.UTF8.GetString(memory[..count].ToArray());
            var match = HostMatchRegex().Match(message);
            if (string.IsNullOrWhiteSpace(match.Value))
            {
                await socket.TryCloseAsync();
                return;
            }
            var host = match
                .Value.Replace("Host: ", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty);
            Log.Write($"请求Host:{host}");
            var tunnel = _tunnelContext.GetTunnel(host);
            if (tunnel == null)
            {
                await sslStream.NotEnablAsync();
                await socket.TryCloseAsync();
                Log.Write("隧道未找到");
                return;
            }

            // 定义隧道子连接
            var subConnect = new TunnelSubConnectModel
            {
                RequestId = Guid.NewGuid().ToString(),
                CancellationTokenSource = new CancellationTokenSource(),
                Socket = socket
            };

            // 客户端子连接事件
            subConnect.ClientRequestConnectEvent += async (webSocket) =>
            {
                subConnect.WebSocket = webSocket;

                // 把初始请求先转发给客户端
                await webSocket.SendAsync(
                    memory[..count].ToArray(),
                    WebSocketMessageType.Binary,
                    true,
                    subConnect.CancellationTokenSource.Token
                );

                // 然后开启两个任务用来转发后面的数据
                var t1 = Task.Run(async () =>
                {
                    try
                    {
                        await webSocket.ForwardAsync(
                            sslStream,
                            subConnect.CancellationTokenSource.Token
                        );
                    }
                    catch { }
                    finally
                    {
                        await webSocket.TryCloseAsync();
                        await subConnect.CancellationTokenSource.CancelAsync();
                    }
                });
                var t2 = Task.Run(async () =>
                {
                    try
                    {
                        await sslStream.ForwardAsync(
                            webSocket,
                            subConnect.CancellationTokenSource.Token
                        );
                    }
                    catch { }
                    finally
                    {
                        await socket.TryCloseAsync();
                        await subConnect.CancellationTokenSource.CancelAsync();
                    }
                });

                // 等待转发完成
                await Task.WhenAll(t1, t2);
            };

            // 定义心跳检查计时器
            subConnect.PulseCheck = new Timer(
                async _ =>
                {
                    try
                    {
                        // 五秒内客户端没有完成请求连接，代表失败
                        if (subConnect.WebSocket == null)
                        {
                            Log.Write("NewRequest:指定时间内客户端未连接");
                            throw new Exception();
                        }
                        // 发送一个0作为心跳检查
                        await subConnect.WebSocket.SendAsync(
                            new byte[1] { 0 },
                            WebSocketMessageType.Binary,
                            true,
                            subConnect.CancellationTokenSource.Token
                        );
                    }
                    catch
                    {
                        // 关闭所有相关内容
                        await subConnect.CloaseAllAsync();

                        // 最后从隧道子连接中移除
                        tunnel.SubConnect.Remove(subConnect.RequestId, out var _);
                    }
                },
                null,
                GlobalStaticConfig.Interval,
                GlobalStaticConfig.Interval
            );

            // 把子连接添加到隧道
            if (tunnel.SubConnect.TryAdd(subConnect.RequestId, subConnect))
            {
                // 发送新请求到客户端，让客户端新建一个WebSocket连接用来转发这个请求
                await tunnel.WebSocket.SendMessageAsync(
                    WebSocketMessageTypeEnum.NewRequest,
                    new NewRequestModel { RequestId = subConnect.RequestId, DomainName = host },
                    subConnect.CancellationTokenSource.Token
                );

                Log.Write("发送新请求给客户端");

                // 一直等待，直到这个子连接取消
                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    subConnect.CancellationTokenSource.Token
                );
            }
        }

        [GeneratedRegex("Host:\\s(.+)", RegexOptions.Multiline)]
        private static partial Regex HostMatchRegex();
    }
}
