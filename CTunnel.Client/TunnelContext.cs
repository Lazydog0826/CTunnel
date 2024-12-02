using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using CTunnel.Client.Request;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Client
{
    public class TunnelContext
    {
        private readonly ConcurrentDictionary<string, TunnelModel> _tunnels = [];

        public TunnelModel? GetTunnel(string domainName)
        {
            if (_tunnels.TryGetValue(domainName, out var tunnel))
            {
                return tunnel;
            }
            return null;
        }

        public async Task AddTunnelAsync(TunnelModel tunnelModel)
        {
            if (!_tunnels.TryAdd(tunnelModel.DomainName, tunnelModel))
            {
                await tunnelModel.WebSocket.CloseAsync(
                    WebSocketCloseStatus.Empty,
                    string.Empty,
                    CancellationToken.None
                );
            }
        }

        public async Task RemoveAsync(TunnelModel tunnelModel)
        {
            _tunnels.Remove(tunnelModel.DomainName, out var _);
            await Task.CompletedTask;
        }

        public async Task NewWebSocketClientAsync(CreateTunnelRequest request)
        {
            var socket = new ClientWebSocket();
            var timeout = new CancellationTokenSource(GlobalStaticConfig.Interval);
            await socket.ConnectAsync(
                new Uri($"ws://{request.ServerIp}:{request.ServerProt}"),
                timeout.Token
            );
            // 发送注册隧道请求
            await socket.SendAsync(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new WebSocketMessageModel
                        {
                            MessageType = WebSocketMessageTypeEnum.RegisterTunnel,
                            JsonData = JsonConvert.SerializeObject(
                                new RegisterTunnelModel
                                {
                                    FileSharingPath = string.Empty,
                                    ListenPort = request.ListenProt,
                                    Token = "123",
                                    Type = request.Type
                                }
                            )
                        }
                    )
                ),
                WebSocketMessageType.Text,
                true,
                timeout.Token
            );

            // 接收成功通知
            var memory = new Memory<byte>(new byte[1024 * 1024]);
            var res = await socket.ReceiveAsync(memory, timeout.Token);
            var mode = JsonConvert.DeserializeObject<WebSocketMessageModel>(
                Encoding.UTF8.GetString(memory[..res.Count].ToArray())
            )!;
            if (mode.MessageType != WebSocketMessageTypeEnum.RegisterTunnel)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.Empty,
                    string.Empty,
                    CancellationToken.None
                );
            }
            var tunnelModel = new TunnelModel
            {
                DomainName = request.DomainName,
                CancellationTokenSource = new CancellationTokenSource(),
                FileSharingPath = string.Empty,
                ListenPort = request.ListenProt,
                Type = request.Type,
                WebSocket = socket,
                ServerIp = request.ServerIp,
                ServerPort = request.ServerProt
            };
            tunnelModel.PulseCheck = new Timer(
                async _ =>
                {
                    try
                    {
                        var message = JsonConvert.SerializeObject(
                            new WebSocketMessageModel
                            {
                                JsonData = string.Empty,
                                MessageType = WebSocketMessageTypeEnum.PulseCheck
                            }
                        );
                        var bytes = Encoding.UTF8.GetBytes(message);
                        await socket.SendAsync(
                            bytes,
                            WebSocketMessageType.Text,
                            true,
                            tunnelModel.CancellationTokenSource.Token
                        );
                    }
                    catch
                    {
                        await tunnelModel.CancellationTokenSource.CancelAsync();
                        await tunnelModel.PulseCheck.DisposeAsync();
                        await socket.CloseAsync(
                            WebSocketCloseStatus.Empty,
                            string.Empty,
                            CancellationToken.None
                        );
                        foreach (var item in tunnelModel.SubConnect)
                        {
                            await item.Value.CloseConnectAsync();
                        }
                        await RemoveAsync(tunnelModel);
                    }
                },
                null,
                GlobalStaticConfig.Interval,
                GlobalStaticConfig.Interval
            );
            if (!_tunnels.TryAdd(tunnelModel.DomainName, tunnelModel))
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.Empty,
                    string.Empty,
                    CancellationToken.None
                );
            }

            while (true)
            {
                res = await socket.ReceiveAsync(memory, tunnelModel.CancellationTokenSource.Token);
                mode = JsonConvert.DeserializeObject<WebSocketMessageModel>(
                    Encoding.UTF8.GetString(memory[..res.Count].ToArray())
                )!;
                if (mode.MessageType == WebSocketMessageTypeEnum.NewRequest)
                {
                    _ = Task.Run(async () =>
                    {
                        await NewRequestAsync(tunnelModel, mode.JsonData);
                    });
                }
            }
        }

        public async Task NewRequestAsync(TunnelModel tunnelModel, string data)
        {
            var timeout = new CancellationTokenSource(GlobalStaticConfig.Interval);
            var model = JsonConvert.DeserializeObject<NewRequestModel>(data)!;
            var requestSocket = new ClientWebSocket();
            await requestSocket.ConnectAsync(
                new Uri($"ws://{tunnelModel.ServerIp}:{tunnelModel.ServerPort}"),
                timeout.Token
            );
            await requestSocket.SendAsync(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new WebSocketMessageModel
                        {
                            MessageType = WebSocketMessageTypeEnum.NewRequest,
                            JsonData = data
                        }
                    )
                ),
                WebSocketMessageType.Text,
                true,
                timeout.Token
            );
            var memory = new Memory<byte>(new byte[1024 * 1024]);
            var res = await requestSocket.ReceiveAsync(memory, timeout.Token);
            var model2 = JsonConvert.DeserializeObject<WebSocketMessageModel>(
                Encoding.UTF8.GetString(memory[..res.Count].ToArray())
            )!;
            if (model2.MessageType == WebSocketMessageTypeEnum.NewRequest)
            {
                var subConnect = new TunnelSubConnectModel()
                {
                    RequestId = model.RequestId,
                    CancellationTokenSource = new CancellationTokenSource(),
                    WebSocket = requestSocket
                };
                if (tunnelModel.SubConnect.TryAdd(model.RequestId, subConnect))
                {
                    var targetSocket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp
                    );
                    await targetSocket.ConnectAsync(
                        new DnsEndPoint(tunnelModel.TargetIp, tunnelModel.TargetPort)
                    );
                    var targetSocketStream = new NetworkStream(targetSocket);
                    var t1 = Task.Run(async () =>
                    {
                        try
                        {
                            await requestSocket.ForwardAsync(
                                targetSocketStream,
                                tunnelModel.CancellationTokenSource.Token
                            );
                        }
                        catch { }
                        finally
                        {
                            await tunnelModel.CancellationTokenSource.CancelAsync();
                            await requestSocket.CloseAsync(
                                WebSocketCloseStatus.Empty,
                                string.Empty,
                                CancellationToken.None
                            );
                        }
                    });
                    var t2 = Task.Run(async () =>
                    {
                        try
                        {
                            await targetSocketStream.ForwardAsync(
                                requestSocket,
                                tunnelModel.CancellationTokenSource.Token
                            );
                        }
                        catch { }
                        finally
                        {
                            await tunnelModel.CancellationTokenSource.CancelAsync();
                            targetSocketStream.Close();
                        }
                    });

                    await Task.WhenAll(t1, t2);
                }
                else
                {
                    await requestSocket.CloseAsync(
                        WebSocketCloseStatus.Empty,
                        string.Empty,
                        CancellationToken.None
                    );
                }
            }
            else
            {
                await requestSocket.CloseAsync(
                    WebSocketCloseStatus.Empty,
                    string.Empty,
                    CancellationToken.None
                );
            }
        }
    }
}
