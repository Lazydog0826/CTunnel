using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using CTunnel.Client.MessageHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Client
{
    public static class MainHandle
    {
        public static readonly ConcurrentDictionary<string, RequestItem> ConcurrentDictionary = [];

        public static async Task HandleAsync(AppConfig appConfig)
        {
            try
            {
                var timeoutToken = new CancellationTokenSource(GlobalStaticConfig.Timeout);
                var masterSocket = new ClientWebSocket();
                var slim = new SemaphoreSlim(1);
                await masterSocket.ConnectAsync(appConfig.Server.Uri, timeoutToken.Token);
                // 注册隧道
                await masterSocket.SendMessageAsync(
                    new RegisterTunnel
                    {
                        Id = appConfig.Id,
                        Token = appConfig.Token,
                        DomainName = appConfig.DomainName,
                        ListenPort = appConfig.Port,
                        Type = appConfig.Type
                    },
                    slim
                );
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
                                try
                                {
                                    await BytesExpand.UseBufferAsync(
                                        (int)ms.Length,
                                        async temBuffer =>
                                        {
                                            await TaskExtend.NewTaskAsBeginFunc(
                                                async () =>
                                                {
                                                    ms.Seek(0, SeekOrigin.Begin);
                                                    await ms.ReadAsync(temBuffer);
                                                    await ms.DisposeAsync();
                                                    ms = GlobalStaticConfig.MSManager.GetStream();
                                                    return temBuffer;
                                                },
                                                async temBuffer2 =>
                                                {
                                                    if (
                                                        Enum.IsDefined(
                                                            typeof(MessageTypeEnum),
                                                            temBuffer2.First()
                                                        )
                                                    )
                                                    {
                                                        var type = (MessageTypeEnum)temBuffer2[0];
                                                        await ServiceContainer
                                                            .GetService<IMessageHandle>(
                                                                type.ToString()
                                                            )
                                                            .HandleAsync(
                                                                masterSocket,
                                                                temBuffer2,
                                                                temBuffer2.Length,
                                                                appConfig,
                                                                ConcurrentDictionary,
                                                                slim
                                                            );
                                                    }
                                                    else
                                                    {
                                                        Log.Write("非法数据格式", LogType.Error);
                                                    }
                                                },
                                                null,
                                                async temBuffer2 =>
                                                {
                                                    ArrayPool<byte>.Shared.Return(temBuffer2);
                                                    await Task.CompletedTask;
                                                }
                                            );
                                        },
                                        false
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Log.Write(ex.Message, LogType.Error);
                                }
                            }
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message, LogType.Error);
            }
        }
    }
}
