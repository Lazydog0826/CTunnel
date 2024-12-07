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
                await masterSocket.ConnectAsync(appConfig.Server.Uri, timeoutToken.Token);
                await masterSocket.SendMessageAsync(
                    new RegisterTunnel
                    {
                        Id = appConfig.Id,
                        Token = appConfig.Token,
                        DomainName = appConfig.DomainName,
                        ListenPort = appConfig.Port,
                        Type = appConfig.Type
                    }
                );
                var socketResult = await masterSocket.ReadMessageAsync<WebSocketResult>(
                    timeoutToken.Token
                );
                if (!socketResult.Success)
                {
                    Log.Write(socketResult.Message, LogType.Error);
                    return;
                }
                Log.Write("连接成功", LogType.Success);
                var buff = ArrayPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize + 37);
                var ms = new MemoryStream();
                while (true)
                {
                    var res = await masterSocket.ReceiveAsync(
                        new Memory<byte>(buff),
                        CancellationToken.None
                    );
                    await ms.WriteAsync(buff.AsMemory(0, res.Count));
                    if (res.EndOfMessage)
                    {
                        try
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            var completeBuffer = ms.ToArray();
                            ms.Close();
                            ms = new MemoryStream();
                            if (Enum.IsDefined(typeof(MessageTypeEnum), completeBuffer[0]))
                            {
                                var type = (MessageTypeEnum)completeBuffer[0];
                                TaskExtend.NewTask(
                                    async () =>
                                    {
                                        await ServiceContainer
                                            .GetService<IMessageHandle>(type.ToString())
                                            .HandleAsync(
                                                masterSocket,
                                                completeBuffer,
                                                completeBuffer.Length,
                                                appConfig,
                                                ConcurrentDictionary
                                            );
                                    },
                                    async ex =>
                                    {
                                        await Task.CompletedTask;
                                        Log.Write(ex.Message, LogType.Error);
                                    }
                                );
                            }
                            else
                            {
                                throw new Exception("非法数据");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Write(ex.Message, LogType.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message, LogType.Error);
            }
        }
    }
}
