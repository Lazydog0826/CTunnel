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
                await tunnelModel.WebSocket.TryCloseAsync();
            }
        }

        public async Task RemoveAsync(TunnelModel tunnelModel)
        {
            _tunnels.Remove(tunnelModel.DomainName, out var _);
            await Task.CompletedTask;
        }

        public async Task NewWebSocketClientAsync(CreateTunnelRequest request)
        {
            // 创建隧道主连接
            var socket = new ClientWebSocket();
            // 规定事件没完成信息交换直接关闭
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
                                    Type = request.Type,
                                    DomainName = request.DomainName,
                                }
                            )
                        }
                    )
                ),
                WebSocketMessageType.Binary,
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
                await socket.TryCloseAsync();
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
                ServerPort = request.ServerProt,
                TargetIp = request.TargetIp,
                TargetPort = request.TargetPort
            };
            // 创建心跳检查计时任务
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
                            WebSocketMessageType.Binary,
                            true,
                            tunnelModel.CancellationTokenSource.Token
                        );
                    }
                    catch
                    {
                        // 标记取消和关闭计时器
                        await tunnelModel.CancellationTokenSource.CancelAsync();
                        await tunnelModel.PulseCheck.DisposeAsync();

                        // 关闭主连接
                        await socket.TryCloseAsync();

                        // 关闭子连接
                        foreach (var item in tunnelModel.SubConnect)
                        {
                            await item.Value.WebSocket.TryCloseAsync();
                            await item.Value.Socket.TryCloseAsync();
                        }

                        // 从上下文删除
                        await RemoveAsync(tunnelModel);
                    }
                },
                null,
                GlobalStaticConfig.Interval,
                GlobalStaticConfig.Interval
            );
            if (!_tunnels.TryAdd(tunnelModel.DomainName, tunnelModel))
            {
                await socket.TryCloseAsync();
            }

            // 监听请求消息
            while (true)
            {
                try
                {
                    res = await socket.ReceiveAsync(
                        memory,
                        tunnelModel.CancellationTokenSource.Token
                    );
                    mode = JsonConvert.DeserializeObject<WebSocketMessageModel>(
                        Encoding.UTF8.GetString(memory[..res.Count].ToArray())
                    )!;
                    Log.Write("接收到请求" + JsonConvert.SerializeObject(mode));

                    // 主要是监听NewRequest，其他的不用管
                    if (mode.MessageType == WebSocketMessageTypeEnum.NewRequest)
                    {
                        // 接收到NewRequest代表外部请求了服务的443端口，服务端通知客户端新建一个WebSocket去做这个请求的转发工作
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await NewRequestAsync(tunnelModel, mode.JsonData);
                            }
                            catch { }
                            finally { }
                        });
                    }
                }
                catch (Exception ex) { }
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
            // 发送NewRequest请求
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
                WebSocketMessageType.Binary,
                true,
                timeout.Token
            );

            // 接收NewRequest成功消息
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
                    subConnect.PulseCheck = new Timer(
                        async _ =>
                        {
                            try
                            {
                                await subConnect.WebSocket.SendAsync(
                                    new byte[1] { 0 },
                                    WebSocketMessageType.Binary,
                                    true,
                                    subConnect.CancellationTokenSource.Token
                                );
                            }
                            catch
                            {
                                // 标记取消和关闭计时器
                                await subConnect.CancellationTokenSource.CancelAsync();
                                await subConnect.PulseCheck.DisposeAsync();

                                // 关闭socket连接
                                await subConnect.WebSocket.TryCloseAsync();
                                await subConnect.Socket.TryCloseAsync();

                                // 从隧道删除子链接
                                tunnelModel.SubConnect.Remove(subConnect.RequestId, out var _);
                            }
                        },
                        null,
                        GlobalStaticConfig.Interval,
                        GlobalStaticConfig.Interval
                    );

                    // 连接目标内网服务
                    subConnect.Socket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp
                    );
                    await subConnect.Socket.ConnectAsync(
                        new DnsEndPoint(tunnelModel.TargetIp, tunnelModel.TargetPort)
                    );
                    var targetSocketStream = new NetworkStream(subConnect.Socket);

                    // 转发
                    var t1 = Task.Run(async () =>
                    {
                        try
                        {
                            await requestSocket.ForwardAsync(
                                targetSocketStream,
                                subConnect.CancellationTokenSource.Token
                            );
                        }
                        catch { }
                        finally
                        {
                            await subConnect.CancellationTokenSource.CancelAsync();
                            await requestSocket.TryCloseAsync();
                        }
                    });
                    var t2 = Task.Run(async () =>
                    {
                        try
                        {
                            await targetSocketStream.ForwardAsync(
                                requestSocket,
                                subConnect.CancellationTokenSource.Token
                            );
                        }
                        catch { }
                        finally
                        {
                            await subConnect.CancellationTokenSource.CancelAsync();
                            await subConnect.Socket.TryCloseAsync();
                        }
                    });

                    // 等待任务完成
                    await Task.WhenAll(t1, t2);
                }
                else
                {
                    await requestSocket.TryCloseAsync();
                }
            }
            else
            {
                await requestSocket.TryCloseAsync();
            }
        }
    }
}
