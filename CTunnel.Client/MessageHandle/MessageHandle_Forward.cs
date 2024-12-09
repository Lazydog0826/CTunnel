using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Client.MessageHandle
{
    public class MessageHandle_Forward : IMessageHandle
    {
        public async Task HandleAsync(
            WebSocket webSocket,
            byte[] bytes,
            int bytesCount,
            AppConfig appConfig,
            ConcurrentDictionary<string, RequestItem> pairs,
            SemaphoreSlim slim
        )
        {
            var requestId = Encoding.UTF8.GetString(bytes.AsSpan(1, 36));
            if (pairs.TryGetValue(requestId, out var ri2))
            {
                await ri2.TargetSocketStream.WriteAsync(bytes.AsMemory(37, bytesCount - 37));
            }
            else
            {
                var ri = new RequestItem()
                {
                    RequestId = requestId,
                    TargetSocket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp
                    )
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
                        TLSExtend.IsNeedTLS(appConfig.Target),
                        false,
                        appConfig.Target.Host
                    );
                    pairs.TryAdd(requestId, ri);
                    await ri.TargetSocketStream.WriteAsync(bytes.AsMemory(37, bytesCount - 37));
                }
                catch
                {
                    await ri.CloseAsync(pairs);
                    throw;
                }

                TaskExtend.NewTask(
                    async () =>
                    {
                        await BytesExpand.UseBufferAsync(
                            GlobalStaticConfig.BufferSize,
                            async buffer =>
                            {
                                int count;
                                while (
                                    (
                                        count = await ri.TargetSocketStream.ReadAsync(
                                            new Memory<byte>(buffer)
                                        )
                                    ) != 0
                                )
                                {
                                    await webSocket.ForwardAsync(
                                        MessageTypeEnum.Forward,
                                        requestId,
                                        buffer,
                                        0,
                                        count,
                                        slim
                                    );
                                }
                            }
                        );
                    },
                    async _ =>
                    {
                        await ri.CloseAsync(pairs);
                    }
                );
            }
        }
    }
}
