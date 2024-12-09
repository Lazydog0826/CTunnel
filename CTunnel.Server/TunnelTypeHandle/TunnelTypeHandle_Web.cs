using System.Buffers;
using System.Text;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server.TunnelTypeHandle
{
    public class TunnelTypeHandle_Web(TunnelContext _tunnelContext) : ITunnelTypeHandle
    {
        public async Task HandleAsync(TunnelModel tunnel)
        {
            try
            {
                var isAdd = _tunnelContext.AddTunnel(tunnel);
                if (isAdd)
                {
                    // 添加到字典成功发送成功消息
                    await tunnel.WebSocket.SendMessageAsync(
                        new WebSocketResult { Success = true },
                        tunnel.Slim
                    );
                    Log.Write($"注册隧道成功", LogType.Success, tunnel.DomainName);

                    // 手动关闭不使用 using
                    var ms = GlobalStaticConfig.MSManager.GetStream();
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
                                            await TaskExtend.NewTaskAsBeginFunc(
                                                async () =>
                                                {
                                                    ms.Seek(0, SeekOrigin.Begin);
                                                    var buffer2ReadCount = await ms.ReadAsync(
                                                        buffer2
                                                    );
                                                    await ms.DisposeAsync();
                                                    ms = GlobalStaticConfig.MSManager.GetStream();
                                                    return buffer2ReadCount;
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
                                                        var requestId = Encoding.UTF8.GetString(
                                                            buffer2.AsSpan(1, 36)
                                                        );
                                                        var ri = tunnel.GetRequestItem(requestId);
                                                        if (ri != null)
                                                        {
                                                            await ri.TargetSocketStream.WriteAsSlimAsync(
                                                                buffer2.AsMemory(
                                                                    37,
                                                                    buffer2Count - 37
                                                                ),
                                                                ri.Slim
                                                            );
                                                        }
                                                        else
                                                        {
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
                                                },
                                                null,
                                                async _ =>
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
                else
                {
                    var result = new WebSocketResult { Success = false, Message = "注册失败，域名重复" };
                    await tunnel.WebSocket.SendMessageAsync(result, tunnel.Slim);
                    throw new Exception(result.Message);
                }
            }
            catch (Exception ex)
            {
                await _tunnelContext.RemoveAsync(tunnel);
                Log.Write(ex.Message, LogType.Error);
            }
        }
    }
}
