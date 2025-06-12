using System.Text;
using CTunnel.Server.SocketHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.Extensions.DependencyInjection;

namespace CTunnel.Server.TunnelTypeHandle;

public class TunnelTypeHandle_TcpUdp(TunnelContext _tunnelContext) : ITunnelTypeHandle
{
    public async Task HandleAsync(TunnelModel tunnel)
    {
        // Tcp和Udp的key为监听的端口
        tunnel.Key = tunnel.ListenPort.ToString();
        tunnel.IsAdd = _tunnelContext.AddTunnel(tunnel);
        if (tunnel.IsAdd)
        {
            // 添加到字典成功发送成功消息
            await tunnel.WebSocket.SendMessageAsync(
                new WebSocketResult { Success = true },
                tunnel.Slim
            );
            // 开启端口监听
            var socketHandle =
                GlobalStaticConfig.ServiceProvider.GetRequiredKeyedService<ISocketHandle>("TcpUdp");
            SocketListen.CreateSocketListen(
                tunnel.Type.ToProtocolType(),
                tunnel.ListenPort,
                socketHandle
            );
            Log.Write($"注册隧道成功", LogType.Success, tunnel.Key);
            // 手动关闭不使用 using
            var ms = GlobalStaticConfig.MsManager.GetStream();
            await BytesExpand.UseBufferAsync(
                GlobalStaticConfig.BufferSize + 37,
                async buffer =>
                {
                    while (true)
                    {
                        var res = await tunnel.WebSocket.ReceiveAsync(
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
                                    ms = GlobalStaticConfig.MsManager.GetStream();

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
                                                    var requestId = Encoding.UTF8.GetString(
                                                        decompressBuffer.AsSpan(1, 36)
                                                    );
                                                    var ri = tunnel.GetRequestItem(requestId);
                                                    if (ri != null)
                                                    {
                                                        // 转发给访问者
                                                        await ri.TargetSocketStream.WriteAsync(
                                                            decompressBuffer.AsMemory(
                                                                37,
                                                                decompressBufferCount - 37
                                                            )
                                                        );
                                                    }
                                                    else
                                                    {
                                                        // 找不到通知客户端关闭请求
                                                        await tunnel.WebSocket.ForwardAsync(
                                                            MessageTypeEnum.CloseForward,
                                                            requestId,
                                                            [],
                                                            0,
                                                            0,
                                                            tunnel.Slim
                                                        );
                                                    }
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
        else
        {
            var result = new WebSocketResult { Success = false, Message = "注册失败，端口重复" };
            await tunnel.WebSocket.SendMessageAsync(result, tunnel.Slim);
        }
    }
}
