using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniComp.Core.App;
using Newtonsoft.Json;

namespace CTunnel.Client;

public class MainBackgroundService(AppConfig appConfig) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var timeoutToken = new CancellationTokenSource(GlobalStaticConfig.Timeout);
            var masterSocket = new ClientWebSocket();
            masterSocket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            masterSocket.Options.SetRequestHeader(
                "RegisterTunnelParam",
                JsonConvert.SerializeObject(
                    new RegisterTunnel
                    {
                        Token = appConfig.Token,
                        DomainName = appConfig.DomainName,
                        ListenPort = appConfig.Port,
                        Type = appConfig.Type,
                    }
                )
            );
            await masterSocket.ConnectAsync(
                new Uri($"wss://{appConfig.Server.Host}:{appConfig.Server.Port}"),
                timeoutToken.Token
            );
            TargetSocket? targetSocket = null;
            while (true)
            {
                var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
                var readCount = await masterSocket.ReceiveAsync(
                    memory.Memory,
                    CancellationToken.None
                );
                if (readCount.MessageType == WebSocketMessageType.Close)
                {
                    Output.Print("服务端断开连接", OutputMessageTypeEnum.Error);
                    Environment.Exit(0);
                }

                try
                {
                    if (targetSocket == null)
                    {
                        await appConfig.SocketCreateSlim.WaitAsync(CancellationToken.None);
                        targetSocket =
                            HostApp.RootServiceProvider.GetRequiredService<TargetSocket>();
                        await targetSocket.ConnectAsync(
                            Encoding.Default.GetString(memory.Memory[..readCount.Count].Span)
                        );
                        var socket = targetSocket;
                        TaskExtend.NewTask(
                            async () =>
                            {
                                await socket.ReadAsync(masterSocket, appConfig.ForwardToServerSlim);
                            },
                            ex => throw ex
                        );
                    }
                    else
                    {
                        await targetSocket.WriteAsync(memory.Memory[..readCount.Count]);
                    }

                    if (readCount.EndOfMessage)
                    {
                        appConfig.SocketCreateSlim.Release();
                        await targetSocket.CloseAsync();
                        targetSocket = null;
                    }
                }
                catch
                {
                    if (targetSocket != null)
                    {
                        appConfig.SocketCreateSlim.Release();
                        await targetSocket.CloseAsync();
                    }
                    targetSocket = null;
                }
            }
        }
        catch (Exception ex)
        {
            Output.Print(ex.Message, OutputMessageTypeEnum.Error);
            Environment.Exit(0);
        }
    }
}
