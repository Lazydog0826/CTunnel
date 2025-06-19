using System.Net.WebSockets;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.Extensions.Hosting;
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
                "Authorization",
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

            await masterSocket.ReceiveMessageAsync(
                async (type, data) =>
                {
                    switch (type)
                    {
                        case WebSocketMessageTypeEnum.ConnectionSuccessful:
                            Output.Print("连接成功");
                            break;
                        case WebSocketMessageTypeEnum.ConnectionFail:
                            var msg = JsonConvert.DeserializeObject<string>(data);
                            Output.Print(msg ?? "连接失败");
                            break;
                        case WebSocketMessageTypeEnum.NewRequest:
                            try
                            {
                                var registerRequest =
                                    JsonConvert.DeserializeObject<RegisterRequest>(data);
                                if (registerRequest != null)
                                {
                                    TaskExtend.NewTask(async () =>
                                    {
                                        await ForwardSocket.CreateForwardSocketAsync(
                                            registerRequest
                                        );
                                    });
                                }
                            }
                            catch
                            {
                                // ignored
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(type), type, null);
                    }
                    await Task.CompletedTask;
                }
            );
        }
        catch (Exception ex)
        {
            Output.Print(ex.Message, OutputMessageTypeEnum.Error);
            Environment.Exit(0);
        }
    }
}
