using System.Buffers;
using System.Net.Sockets;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;

namespace CTunnel.Server.SocketHandle;

public class SocketHandleRequest(AppConfig appConfig, TunnelContext tunnelContext) : ISocketHandle
{
    public async Task HandleAsync(Socket socket, int port)
    {
        var forwardSocketStream = await socket.GetStreamAsync(true, true, string.Empty);
        var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
        var readCount = await forwardSocketStream.ReadAsync(memory.Memory);
        try
        {
            var registerRequest = memory.Memory[..readCount].ConvertModel<RegisterRequest>();
            memory.Dispose();
            if (registerRequest.Token != appConfig.Token)
            {
                throw new Exception();
            }
            var tunnel = tunnelContext.GetTunnel(registerRequest.TunnelKey);
            if (tunnel == null)
            {
                throw new Exception();
            }
            var requestItem = tunnel.GetRequestItem(registerRequest.RequestId);
            if (requestItem == null)
            {
                throw new Exception();
            }

            requestItem.ForwardSocket = socket;
            requestItem.ForwardSocketStream = forwardSocketStream;
            requestItem.TokenSource.Token.Register(() =>
            {
                requestItem.TargetSocket.TryCloseAsync().Wait();
                requestItem.ForwardSocket.TryCloseAsync().Wait();
            });

            if (requestItem.ToBeSent != null)
            {
                await requestItem.ForwardSocketStream.WriteAsync(
                    requestItem.ToBeSent.Memory[..requestItem.ToBeSentCount]
                );
                requestItem.ToBeSent.Dispose();
            }

            TaskExtend.NewTask(
                async () =>
                {
                    await SocketExtend.ForwardAsync(
                        requestItem.TargetSocketStream,
                        requestItem.ForwardSocketStream,
                        requestItem.TokenSource.Token
                    );
                },
                null,
                async () =>
                {
                    await requestItem.TokenSource.CancelAsync();
                }
            );
            TaskExtend.NewTask(
                async () =>
                {
                    await SocketExtend.ForwardAsync(
                        requestItem.ForwardSocketStream,
                        requestItem.TargetSocketStream,
                        requestItem.TokenSource.Token
                    );
                },
                null,
                async () =>
                {
                    await requestItem.TokenSource.CancelAsync();
                }
            );
            await Task.Delay(Timeout.InfiniteTimeSpan, requestItem.TokenSource.Token);
        }
        catch
        {
            await socket.TryCloseAsync();
        }
    }
}
