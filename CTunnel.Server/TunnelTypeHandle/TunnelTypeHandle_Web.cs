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
                if (_tunnelContext.AddTunnel(tunnel))
                {
                    // 添加到字典成功发送成功消息
                    await tunnel.WebSocket.SendMessageAsync(
                        new WebSocketResult { Success = true, Message = "成功" },
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
                                            ms.Seek(0, SeekOrigin.Begin);
                                            var buffer2ReadCount = await ms.ReadAsync(buffer2);
                                            await ms.DisposeAsync();
                                            ms = GlobalStaticConfig.MSManager.GetStream();

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
                                                    await ri.TargetSocketStream.WriteAsync(
                                                        buffer2.AsMemory(37, buffer2ReadCount - 37)
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
                                            else
                                            {
                                                Log.Write("非法数据格式", LogType.Error);
                                            }
                                        }
                                    );
                                }
                            }
                        }
                    );
                }
                else
                {
                    throw new Exception("注册失败");
                }
            }
            catch (Exception ex)
            {
                await tunnel.WebSocket.SendMessageAsync(
                    new WebSocketResult { Success = false, Message = ex.Message },
                    tunnel.Slim
                );
                await _tunnelContext.RemoveAsync(tunnel);
                Log.Write(ex.Message, LogType.Error);
            }
        }
    }
}
