using System.Net.WebSockets;
using CTunnel.Client.MessageHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace CTunnel.Client
{
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
                            Type = appConfig.Type
                        }
                    )
                );
                var slim = new SemaphoreSlim(1);
                await masterSocket.ConnectAsync(appConfig.Server.Uri, timeoutToken.Token);
                // 接受注册结果
                var socketResult = await masterSocket.ReadMessageAsync<WebSocketResult>(
                    timeoutToken.Token
                );
                if (!socketResult.Success)
                {
                    Log.Write(socketResult.Message, LogType.Error);
                    return;
                }
                Log.Write("连接成功", LogType.Success);
                // 这里手动释放不加 using
                var ms = GlobalStaticConfig.MSManager.GetStream();
                await BytesExpand.UseBufferAsync(
                    GlobalStaticConfig.BufferSize + 37,
                    async buffer =>
                    {
                        while (true)
                        {
                            var res = await masterSocket.ReceiveAsync(
                                new Memory<byte>(buffer),
                                CancellationToken.None
                            );
                            await ms.WriteAsync(buffer.AsMemory(0, res.Count));
                            if (res.EndOfMessage)
                            {
                                await BytesExpand.UseBufferAsync(
                                    (int)ms.Length,
                                    async buffer2 =>
                                    {
                                        ms.Seek(0, SeekOrigin.Begin);
                                        var buffer2Count = await ms.ReadAsync(buffer2);
                                        await ms.DisposeAsync();
                                        ms = GlobalStaticConfig.MSManager.GetStream();
                                        await buffer2
                                            .AsMemory(0, buffer2Count)
                                            .DecompressAsync(
                                                async (decompressBuffer, decompressBufferCount) =>
                                                {
                                                    if (
                                                        Enum.IsDefined(
                                                            typeof(MessageTypeEnum),
                                                            decompressBuffer.First()
                                                        )
                                                    )
                                                    {
                                                        var type = (MessageTypeEnum)
                                                            decompressBuffer.First();
                                                        var messageHandle =
                                                            GlobalStaticConfig.ServiceProvider.GetRequiredKeyedService<IMessageHandle>(
                                                                type.ToString()
                                                            );
                                                        await messageHandle.HandleAsync(
                                                            masterSocket,
                                                            decompressBuffer,
                                                            decompressBufferCount
                                                        );
                                                    }
                                                }
                                            );
                                    }
                                );
                            }
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message, LogType.Error);
                Environment.Exit(0);
            }
        }
    }
}
