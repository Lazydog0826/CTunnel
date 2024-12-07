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
                if (_tunnelContext.AddTunnel(tunnel))
                {
                    await tunnel.WebSocket.SendMessageAsync(
                        new WebSocketResult { Success = true, Message = "成功" }
                    );
                    Log.Write($"注册隧道成功", LogType.Success, tunnel.DomainName);
                    var buffer = ArrayPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize + 37);
                    var ms = new MemoryStream();
                    try
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
                                ms.Seek(0, SeekOrigin.Begin);
                                var completeBuffer = ms.ToArray();
                                ms.Close();
                                ms = new MemoryStream();
                                try
                                {
                                    if (Enum.IsDefined(typeof(MessageTypeEnum), completeBuffer[0]))
                                    {
                                        var requestId = Encoding.UTF8.GetString(
                                            completeBuffer.AsSpan(1, 36)
                                        );
                                        var ri = tunnel.GetRequestItem(requestId);
                                        if (ri != null)
                                        {
                                            await ri.TargetSocketStream.WriteAsync(
                                                completeBuffer.AsMemory(
                                                    37,
                                                    completeBuffer.Length - 37
                                                )
                                            );
                                        }
                                        else
                                        {
                                            await tunnel.WebSocket.ForwardAsync(
                                                MessageTypeEnum.CloseForward,
                                                requestId,
                                                [],
                                                0,
                                                0
                                            );
                                        }
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
                        Log.Write(ex.Message, LogType.Error, tunnel.DomainName);
                        await _tunnelContext.RemoveAsync(tunnel);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                else
                {
                    throw new Exception("注册失败");
                }
            }
            catch (Exception ex)
            {
                await tunnel.WebSocket.SendMessageAsync(
                    new WebSocketResult { Success = false, Message = ex.Message }
                );
                await tunnel.CloseAsync();
                Log.Write(ex.Message, LogType.Error);
            }
        }
    }
}
