using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.IO;

namespace CTunnel.Client.MessageHandle;

public class MessageHandleForward(AppConfig appConfig) : IMessageHandle
{
    public async Task HandleAsync(WebSocket webSocket, RecyclableMemoryStream stream)
    {
        var requestId = Encoding.UTF8.GetString(stream.GetMemory()[1..37].Span);
        if (appConfig.ConcurrentDictionary.TryGetValue(requestId, out var ri2))
        {
            try
            {
                await ri2.TargetSocketStream.ShardWriteAsync(stream, 37);
            }
            catch (Exception ex)
            {
                Output.Print(ex.Message, OutputMessageTypeEnum.Error);
                await ri2.CloseAsync(appConfig.ConcurrentDictionary);
            }
        }
        else
        {
            var ri = new RequestItem()
            {
                RequestId = requestId,
                TargetSocket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    appConfig.Type.ToProtocolType()
                ),
            };

            try
            {
                await ri.TargetSocket.ConnectAsync(
                    new DnsEndPoint(appConfig.Target.Host, appConfig.Target.Port)
                );
                ri.TargetSocket.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.KeepAlive,
                    true
                );
                ri.TargetSocketStream = await ri.TargetSocket.GetStreamAsync(
                    appConfig.Target.IsNeedTls(),
                    false,
                    appConfig.Target.Host
                );
                appConfig.ConcurrentDictionary.TryAdd(requestId, ri);
                await ri.TargetSocketStream.ShardWriteAsync(stream, 37);
            }
            catch (Exception ex)
            {
                Output.Print(ex.Message, OutputMessageTypeEnum.Error);
                await ri.CloseAsync(appConfig.ConcurrentDictionary);
            }

            TaskExtend.NewTask(
                async () =>
                {
                    using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
                    int readCount;
                    while ((readCount = await ri.TargetSocketStream.ReadAsync(memory.Memory)) != 0)
                    {
                        await webSocket.ForwardAsync(
                            MessageTypeEnum.Forward,
                            requestId.ToBytes(),
                            memory.Memory[..readCount],
                            appConfig.Slim
                        );
                    }
                },
                async ex =>
                {
                    Output.Print(ex.Message, OutputMessageTypeEnum.Error);
                    await ri.CloseAsync(appConfig.ConcurrentDictionary);
                }
            );
        }
    }
}
