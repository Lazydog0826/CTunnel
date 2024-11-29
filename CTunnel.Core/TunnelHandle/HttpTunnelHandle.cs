using System.Net.Sockets;
using System.Net.WebSockets;
using CTunnel.Core.Model;

namespace CTunnel.Core.TunnelHandle
{
    public class HttpTunnelHandle : ITunnelHandle
    {
        public async Task HandleAsync(TunnelModel tunnel, TcpClient tcpClient)
        {
            WebSocket? webSocket = null;
            while (tunnel.ConnectionPool.TryTake(out webSocket))
            {
                if (webSocket.State == WebSocketState.Open)
                    break;
            }

            if (webSocket != null)
            {
                var cancellationToken = new CancellationTokenSource();

                var tcpStream = tcpClient.GetStream();

                var t1 = Task.Run(async () =>
                {
                    try
                    {
                        while (webSocket.State == WebSocketState.Open)
                        {
                            var arraySegment = new ArraySegment<byte>(new byte[1024 * 1024]);
                            var result = await webSocket.ReceiveAsync(
                                arraySegment,
                                cancellationToken.Token
                            );
                            await tcpStream.WriteAsync(
                                arraySegment.Slice(0, result.Count).ToArray()
                            );
                        }
                    }
                    catch { }
                    finally
                    {
                        await cancellationToken.CancelAsync();
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.Empty,
                            string.Empty,
                            CancellationToken.None
                        );
                    }
                });

                var t2 = Task.Run(async () =>
                {
                    try
                    {
                        var memory = new Memory<byte>(new byte[1024 * 1024]);
                        var count = 0;
                        while (
                            (count = await tcpStream.ReadAsync(memory, cancellationToken.Token)) > 0
                        )
                        {
                            await webSocket.SendAsync(
                                memory[..count].ToArray(),
                                WebSocketMessageType.Binary,
                                false,
                                cancellationToken.Token
                            );
                        }
                    }
                    catch { }
                    finally
                    {
                        await cancellationToken.CancelAsync();
                        tcpClient.Close();
                    }
                });

                await Task.WhenAll(t1, t2);
            }
            else
            {
                throw new Exception("连接池没有可用的连接");
            }
        }

        public async Task<bool> IsCloseAsync(TunnelModel tunnel)
        {
            await Task.CompletedTask;
            return !tunnel
                .ConnectionPool.ToList()
                .Any(x => x.State == WebSocketState.Open || x.State == WebSocketState.Connecting);
        }
    }
}
