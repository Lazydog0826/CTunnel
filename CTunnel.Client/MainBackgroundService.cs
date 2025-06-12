using System.Buffers;
using System.Net.WebSockets;
using CTunnel.Client.MessageHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
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
        await Task.Delay(3000, stoppingToken);
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
            await masterSocket.ConnectAsync(appConfig.Server.Uri, timeoutToken.Token);
            Output.Print("已连接");
            await using var ms = GlobalStaticConfig.MsManager.GetStream();
            using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);

            while (true)
            {
                var res = await masterSocket.ReceiveAsync(memory.Memory, CancellationToken.None);
                await ms.WriteAsync(memory.Memory[..res.Count], CancellationToken.None);
                if (res.EndOfMessage)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var messageTypeEnum = ms.GetMemory()[..1].Span[0];
                    if (Enum.IsDefined(typeof(MessageTypeEnum), messageTypeEnum))
                    {
                        var type = (MessageTypeEnum)messageTypeEnum;
                        var messageHandle =
                            HostApp.RootServiceProvider.GetRequiredKeyedService<IMessageHandle>(
                                type.ToString()
                            );
                        await messageHandle.HandleAsync(masterSocket, ms);
                    }
                    ms.Reset();
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
