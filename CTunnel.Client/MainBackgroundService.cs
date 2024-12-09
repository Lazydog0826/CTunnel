using System.Buffers;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, RequestItem> ConcurrentDictionary = [];

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
                            Id = appConfig.Id,
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
                                        await TaskExtend.NewTaskAsBeginFunc(
                                            async () =>
                                            {
                                                ms.Seek(0, SeekOrigin.Begin);
                                                var buffer2Count = await ms.ReadAsync(buffer2);
                                                await ms.DisposeAsync();
                                                ms = GlobalStaticConfig.MSManager.GetStream();
                                                return buffer2Count;
                                            },
                                            async buffer2Count =>
                                            {
                                                if (
                                                    Enum.IsDefined(
                                                        typeof(MessageTypeEnum),
                                                        buffer2.First()
                                                    )
                                                )
                                                {
                                                    var type = (MessageTypeEnum)buffer2.First();
                                                    await GlobalStaticConfig
                                                        .ServiceProvider.GetRequiredKeyedService<IMessageHandle>(
                                                            type.ToString()
                                                        )
                                                        .HandleAsync(
                                                            masterSocket,
                                                            buffer2,
                                                            buffer2Count,
                                                            appConfig,
                                                            ConcurrentDictionary,
                                                            slim
                                                        );
                                                }
                                            },
                                            null,
                                            async obj =>
                                            {
                                                ArrayPool<byte>.Shared.Return(buffer2);
                                                await Task.CompletedTask;
                                            }
                                        );
                                    },
                                    false
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
