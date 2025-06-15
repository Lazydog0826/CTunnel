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
                CancellationToken.None
            );

            using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
            await using var ms = GlobalStaticConfig.MsManager.GetStream();
            while (true)
            {
                var readCount = await masterSocket.ReceiveAsync(
                    memory.Memory,
                    CancellationToken.None
                );
                if (readCount.MessageType == WebSocketMessageType.Close)
                {
                    Output.Print("服务端断开连接", OutputMessageTypeEnum.Error);
                    Environment.Exit(0);
                }
                await ms.WriteAsync(memory.Memory, CancellationToken.None);
                if (readCount.EndOfMessage)
                {
                    try
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        var registerRequest = ms.GetMemory().ConvertModel<RegisterRequest>();
                        registerRequest.Token = appConfig.Token;
                        TaskExtend.NewTask(async () =>
                        {
                            await ForwardSocket.CreateForwardSocketAsync(registerRequest);
                        });
                    }
                    catch
                    {
                        // ignored
                    }
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
