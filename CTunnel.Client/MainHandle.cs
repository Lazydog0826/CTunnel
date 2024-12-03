using System.Net;
using System.Net.Sockets;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Client
{
    public static class MainHandle
    {
        public static async Task HandleAsync(AppConfig appConfig)
        {
            var tunnel = new TunnelModel
            {
                DomainName = appConfig.DomainName,
                ListenPort = appConfig.Port,
                Type = appConfig.Type,
                CancellationTokenSource = new CancellationTokenSource(),
                MasterSocket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp
                )
            };
            await tunnel.MasterSocket.ConnectAsync(
                new DnsEndPoint(appConfig.ServerHost, appConfig.ServerPort)
            );
            tunnel.MasterSocketStream = await tunnel.MasterSocket.GetStreamAsync(
                true,
                false,
                appConfig.ServerHost
            );
            var timeout = new CancellationTokenSource(GlobalStaticConfig.Timeout);
            await tunnel.MasterSocketStream.SendMessageAsync(
                new SocketTypeMessage
                {
                    MessageType = WebSocketMessageTypeEnum.RegisterTunnel,
                    JsonData = JsonConvert.SerializeObject(
                        new RegisterTunnel
                        {
                            Token = appConfig.Token,
                            DomainName = appConfig.DomainName,
                            ListenPort = appConfig.Port,
                            Type = appConfig.Type
                        }
                    )
                },
                timeout.Token
            );
            var registerRes = await tunnel.MasterSocketStream.ReadMessageAsync<SocketTypeMessage>(
                timeout.Token
            );
            if (registerRes.MessageType != WebSocketMessageTypeEnum.RegisterTunnel)
            {
                await tunnel.MasterSocket.TryCloseAsync();
                throw new Exception("消息类型不是预期的");
            }
            if (!SocketResult.IsSuccess(registerRes.JsonData, out var message))
            {
                await tunnel.MasterSocket.TryCloseAsync();
                throw new Exception(message);
            }
            tunnel.CreateHeartbeatCheck();
            Log.Write($"连接成功 {tunnel.DomainName}", LogType.Success);
            await tunnel.MasterSocketStream.LoopReadMessageAsync<SocketTypeMessage>(
                async t =>
                {
                    await Task.CompletedTask;
                    if (t.MessageType == WebSocketMessageTypeEnum.NewRequest)
                    {
                        TaskExtend.NewTask(async () =>
                        {
                            await NewRequestAsync(t.JsonData, appConfig, tunnel);
                        });
                    }
                },
                CancellationToken.None
            );
        }

        public static async Task NewRequestAsync(
            string jsonData,
            AppConfig appConfig,
            TunnelModel tunnel
        )
        {
            var requestModel = JsonConvert.DeserializeObject<NewRequest>(jsonData)!;
            Log.Write($"{requestModel} 接收到服务端通知", LogType.Success);
            requestModel.Token = appConfig.Token;
            var masterSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
            await masterSocket.ConnectAsync(
                new DnsEndPoint(appConfig.ServerHost, appConfig.ServerPort)
            );
            var masterSocketStream = await masterSocket.GetStreamAsync(
                true,
                false,
                appConfig.ServerHost
            );
            var timeout = new CancellationTokenSource(GlobalStaticConfig.Timeout);
            await masterSocketStream.SendMessageAsync(
                new SocketTypeMessage
                {
                    MessageType = WebSocketMessageTypeEnum.NewRequest,
                    JsonData = JsonConvert.SerializeObject(requestModel)
                },
                timeout.Token
            );
            var registerRes = await masterSocketStream.ReadMessageAsync<SocketTypeMessage>(
                timeout.Token
            );
            if (registerRes.MessageType != WebSocketMessageTypeEnum.NewRequest)
            {
                await masterSocket.TryCloseAsync();
                throw new Exception("消息类型不是预期的");
            }
            if (!SocketResult.IsSuccess(registerRes.JsonData, out var message))
            {
                await masterSocket.TryCloseAsync();
                throw new Exception(message);
            }
            var tunnelSubConnect = new TunnelSubConnectModel
            {
                RequestId = requestModel.RequestId,
                MasterSocket = masterSocket,
                MasterSocketStream = masterSocketStream,
                CancellationTokenSource = new CancellationTokenSource(),
                ListenSocket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp
                )
            };
            tunnel.SubConnect.TryAdd(tunnelSubConnect.RequestId, tunnelSubConnect);
            Log.Write($"{requestModel} 请求Socket已连接", LogType.Success);
            await tunnelSubConnect.ListenSocket.ConnectAsync(
                new DnsEndPoint(appConfig.TargetIp, appConfig.TargetPort)
            );
            tunnelSubConnect.ListenSocketStream =
                await tunnelSubConnect.ListenSocket.GetStreamAsync(false, false, string.Empty);
            tunnelSubConnect.CreateHeartbeatCheck(tunnel);
            TaskExtend.NewTask(
                async () =>
                {
                    await masterSocketStream.ForwardAsync(
                        tunnelSubConnect.ListenSocketStream,
                        tunnelSubConnect.CancellationTokenSource.Token
                    );
                },
                async _ =>
                {
                    await tunnelSubConnect.CancellationTokenSource.CancelAsync();
                    await masterSocket.TryCloseAsync();
                }
            );
            TaskExtend.NewTask(
                async () =>
                {
                    await tunnelSubConnect.ListenSocketStream.ForwardAsync(
                        masterSocketStream,
                        tunnelSubConnect.CancellationTokenSource.Token
                    );
                },
                async _ =>
                {
                    await tunnelSubConnect.CancellationTokenSource.CancelAsync();
                    await tunnelSubConnect.ListenSocket.TryCloseAsync();
                }
            );
        }
    }
}
