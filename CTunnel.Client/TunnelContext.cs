using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using CTunnel.Client.Request;
using CTunnel.Client.Response;
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

        public bool IsRepeat(string key)
        {
            return _tunnels.Any(x => x.Key == key);
        }

        public TunnelModel? GetTunnel(string domainName)
        {
            if (_tunnels.TryGetValue(domainName, out var tunnel))
            {
                return tunnel;
            }
            return null;
        }

        public async Task<bool> AddTunnelAsync(TunnelModel tunnelModel)
        {
            if (IsRepeat(tunnelModel.DomainName))
            {
                return false;
            }
            _tunnels.TryAdd(tunnelModel.DomainName, tunnelModel);
            await Task.CompletedTask;
            return true;
        }

        public async Task RemoveAsync(TunnelModel tunnelModel)
        {
            _tunnels.Remove(tunnelModel.DomainName, out var _);
            await Task.CompletedTask;
        }

        public async Task<string> NewWebSocketClientAsync(CreateTunnelRequest request)
        {
            var message = "成功";

            // 创建隧道主连接
            var socket = new ClientWebSocket();

            // 规定事件没完成信息交换直接关闭
            var timeout = new CancellationTokenSource(GlobalStaticConfig.Interval);

            await socket.ConnectAsync(new Uri(request.ServerHost), timeout.Token);

            // 发送注册隧道请求
            await socket.SendMessageAsync(
                WebSocketMessageTypeEnum.RegisterTunnel,
                new RegisterTunnelModel
                {
                    FileSharingPath = request.FileSharingPath,
                    ListenPort = request.ListenProt,
                    Token = request.Token,
                    Type = request.Type,
                    DomainName = request.DomainName,
                },
                timeout.Token
            );

            // 接收成功通知
            var wsMessage_rt = await socket.ReadModelAsync<WebSocketMessageModel>(timeout.Token);
            if (wsMessage_rt.MessageType != WebSocketMessageTypeEnum.RegisterTunnel)
            {
                message = "接受到的消息不是预期的";
                Log.Write(message);
                Log.Write(JsonConvert.SerializeObject(wsMessage_rt));
                await socket.TryCloseAsync();
                return message;
            }
            if (!SuccessFailureModel.IsSuccess(wsMessage_rt.JsonData, out var message2))
            {
                message = message2;
                Log.Write(message);
                await socket.TryCloseAsync();
                return message;
            }

            var newTunnel = new TunnelModel
            {
                DomainName = request.DomainName,
                CancellationTokenSource = new CancellationTokenSource(),
                FileSharingPath = string.Empty,
                ListenPort = request.ListenProt,
                Type = request.Type,
                WebSocket = socket,
                ServerHost = request.ServerHost,
                TargetIp = request.TargetIp,
                TargetPort = request.TargetPort
            };

            // 创建心跳检查计时任务
            newTunnel.PulseCheck = new Timer(
                async _ =>
                {
                    try
                    {
                        await newTunnel.WebSocket.PulseCheckAsync(
                            newTunnel.CancellationTokenSource.Token
                        );
                    }
                    catch
                    {
                        // 关闭所有相关信息
                        await newTunnel.CloseAllAsync();

                        // 从上下文删除
                        await RemoveAsync(newTunnel);
                    }
                },
                null,
                GlobalStaticConfig.Interval,
                GlobalStaticConfig.Interval
            );

            if (!await AddTunnelAsync(newTunnel))
            {
                message = "添加隧道失败，域名可能重复";
                Log.Write(message);
                await socket.TryCloseAsync();
                return message;
            }

            _ = Task.Run(async () =>
            {
                // 监听请求消息
                while (true)
                {
                    try
                    {
                        var wsMessage_nr = await socket.ReadModelAsync<WebSocketMessageModel>(
                            newTunnel.CancellationTokenSource.Token
                        );
                        // 主要是监听NewRequest，其他的不用管
                        if (wsMessage_nr.MessageType == WebSocketMessageTypeEnum.NewRequest)
                        {
                            // 接收到NewRequest代表外部请求了服务的443端口，服务端通知客户端新建一个WebSocket去做这个请求的转发工作
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await NewRequestAsync(newTunnel, wsMessage_nr.JsonData);
                                }
                                catch { }
                                finally { }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex.Message);
                    }
                }
            });
            return message;
        }

        public async Task NewRequestAsync(TunnelModel tunnelModel, string data)
        {
            var timeout = new CancellationTokenSource(GlobalStaticConfig.Interval);
            var newRequestModel = JsonConvert.DeserializeObject<NewRequestModel>(data)!;
            var requestSocket = new ClientWebSocket();
            await requestSocket.ConnectAsync(new Uri(tunnelModel.ServerHost), timeout.Token);

            // 发送NewRequest请求
            await requestSocket.SendMessageAsync(
                WebSocketMessageTypeEnum.NewRequest,
                newRequestModel,
                timeout.Token
            );
            Log.Write("NewRequest连接");
            // 接收NewRequest成功消息
            var wsMessage = await requestSocket.ReadModelAsync<WebSocketMessageModel>(
                timeout.Token
            );
            if (!SuccessFailureModel.IsSuccess(wsMessage.JsonData, out var message))
            {
                Log.Write(message);
                await requestSocket.TryCloseAsync();
                return;
            }

            var subConnect = new TunnelSubConnectModel()
            {
                RequestId = newRequestModel.RequestId,
                CancellationTokenSource = new CancellationTokenSource(),
                WebSocket = requestSocket
            };

            if (tunnelModel.SubConnect.TryAdd(newRequestModel.RequestId, subConnect))
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
                            // 关闭所有相关信息
                            await subConnect.CloaseAllAsync();

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

                Log.Write("NewRequest连接成功");

                // 等待任务完成
                await Task.WhenAll(t1, t2);
            }
            else
            {
                Log.Write("子链接未能添加");
                await requestSocket.TryCloseAsync();
            }
        }

        public List<GetTunneResponse> GetTunneListAsync()
        {
            var res = new List<GetTunneResponse>();

            _tunnels
                .ToList()
                .ForEach(x =>
                {
                    res.Add(
                        new GetTunneResponse
                        {
                            DomainName = x.Key,
                            TargetIp = x.Value.TargetIp,
                            TargetPort = x.Value.TargetPort,
                        }
                    );
                });

            return res;
        }
    }
}
