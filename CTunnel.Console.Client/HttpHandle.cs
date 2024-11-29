using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using CTunnel.Core.Model;

namespace CTunnel.Console.Client
{
    public class HttpHandle
    {
        public async Task HandleAsync(CreateTunnelModel tunnel)
        {
            var blockingCollection = new BlockingCollection<ClientWebSocket>(10);

            while (true)
            {
                var newSocket = new ClientWebSocket();
                blockingCollection.Add(newSocket);
                var cancellationToken = new CancellationTokenSource();
                var serverUri = new UriBuilder($"ws://{tunnel.ServerIp}:{tunnel.ServerPort}");

                newSocket.Options.SetRequestHeader(nameof(CreateTunnelModel.Id), tunnel.Id);
                newSocket.Options.SetRequestHeader(
                    nameof(CreateTunnelModel.Type),
                    ((int)tunnel.Type).ToString()
                );
                newSocket.Options.SetRequestHeader(
                    nameof(CreateTunnelModel.ListenPort),
                    tunnel.ListenPort.ToString()
                );

                await newSocket.ConnectAsync(serverUri.Uri, cancellationToken.Token);
                var arraySegment = new ArraySegment<byte>(new byte[1024 * 1024]);
                var receive = await newSocket.ReceiveAsync(arraySegment, cancellationToken.Token);
                var targetTcpClient = new TcpClient();
                await targetTcpClient.ConnectAsync(tunnel.TargetIp, tunnel.TargePort);
                var targetTcpStream = targetTcpClient.GetStream();
                await targetTcpStream.WriteAsync(arraySegment.Slice(0, receive.Count).ToArray());
                var t2 = Task.Run(async () =>
                {
                    try
                    {
                        var memory = new Memory<byte>(new byte[1024 * 1024]);
                        var count = 0;
                        while (
                            (
                                count = await targetTcpStream.ReadAsync(
                                    memory,
                                    cancellationToken.Token
                                )
                            ) > 0
                        )
                        {
                            await newSocket.SendAsync(
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
                        targetTcpClient.Close();
                    }
                });
                var t3 = Task.Run(async () =>
                {
                    try
                    {
                        while (newSocket.State == WebSocketState.Open)
                        {
                            var arraySegment = new ArraySegment<byte>(new byte[1024 * 1024]);
                            var result = await newSocket.ReceiveAsync(
                                arraySegment,
                                cancellationToken.Token
                            );
                            await targetTcpStream.WriteAsync(
                                arraySegment.Slice(0, result.Count).ToArray()
                            );
                        }
                    }
                    catch { }
                    finally { }
                });
                await Task.WhenAll(t2, t3);
                blockingCollection.Take();
            }
        }
    }
}
