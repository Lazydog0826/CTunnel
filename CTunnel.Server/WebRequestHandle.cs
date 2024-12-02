using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Server
{
    public partial class WebRequestHandle(TunnelContext _tunnelContext)
    {
        public async Task HandleAsync(Socket socket, X509Certificate2 x509Certificate2)
        {
            var sslStream = new SslStream(new NetworkStream(socket), false);
            await sslStream.AuthenticateAsServerAsync(
                x509Certificate2,
                false,
                SslProtocols.Tls13,
                true
            );
            var memory = new Memory<byte>(new byte[1024 * 1024]);
            int count;
            while ((count = await sslStream.ReadAsync(memory)) > 0)
            {
                var message = Encoding.UTF8.GetString(memory[..count].ToArray());
                var match = HostMatchRegex().Match(message);
                if (string.IsNullOrWhiteSpace(match.Value))
                {
                    socket.Close();
                    return;
                }
                var host = match.Value.Replace("Host: ", string.Empty);
                var tunnel = _tunnelContext.GetTunnel(host);
                if (tunnel == null)
                {
                    await sslStream.NotEnablAsync();
                    socket.Close();
                    return;
                }
                var subConnect = new TunnelSubConnectModel
                {
                    RequestId = Guid.NewGuid().ToString(),
                    CancellationTokenSource = new CancellationTokenSource(),
                    Socket = socket
                };
                subConnect.ClientRequestConnectEvent += async (webSocket) =>
                {
                    await webSocket.SendAsync(
                        memory[..count].ToArray(),
                        WebSocketMessageType.Binary,
                        true,
                        subConnect.CancellationTokenSource.Token
                    );
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
                            await webSocket.CloseAsync(
                                WebSocketCloseStatus.Empty,
                                string.Empty,
                                CancellationToken.None
                            );
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
                            sslStream.Close();
                            await subConnect.CancellationTokenSource.CancelAsync();
                        }
                    });

                    await Task.WhenAll(t1, t2);
                };
                subConnect.PulseCheck = new Timer(
                    async _ =>
                    {
                        try
                        {
                            if (subConnect.WebSocket == null)
                            {
                                throw new Exception();
                            }
                        }
                        catch
                        {
                            socket.Close();
                            await subConnect.PulseCheck.DisposeAsync();
                            await subConnect.CancellationTokenSource.CancelAsync();
                            if (subConnect != null)
                            {
                                await subConnect.WebSocket.CloseAsync(
                                    WebSocketCloseStatus.Empty,
                                    string.Empty,
                                    CancellationToken.None
                                );
                            }
                        }
                    },
                    null,
                    GlobalStaticConfig.Interval,
                    GlobalStaticConfig.Interval
                );
                if (tunnel.SubConnect.TryAdd(subConnect.RequestId, subConnect))
                {
                    await tunnel.WebSocket.SendAsync(
                        Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(
                                new WebSocketMessageModel
                                {
                                    MessageType = Share.Enums.WebSocketMessageTypeEnum.NewRequest,
                                    JsonData = JsonConvert.SerializeObject(
                                        new NewRequestModel
                                        {
                                            RequestId = subConnect.RequestId,
                                            DomainName = host
                                        }
                                    )
                                }
                            )
                        ),
                        WebSocketMessageType.Text,
                        true,
                        subConnect.CancellationTokenSource.Token
                    );

                    await Task.Delay(
                        GlobalStaticConfig.TenYears,
                        subConnect.CancellationTokenSource.Token
                    );
                }
            }
        }

        [GeneratedRegex("Host:\\s(.+)", RegexOptions.Multiline)]
        private static partial Regex HostMatchRegex();
    }
}
