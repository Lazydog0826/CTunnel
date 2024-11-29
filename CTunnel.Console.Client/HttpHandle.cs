using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using CTunnel.Core.Expand;
using CTunnel.Core.Model;
using Newtonsoft.Json;

namespace CTunnel.Console.Client
{
    public class HttpHandle
    {
        public async Task HandleAsync(CreateTunnelModel tunnel)
        {
            await Task.CompletedTask;
            var blockingCollection = new BlockingCollection<TcpClient>(10);

            _ = new Timer(
                async state =>
                {
                    var isOk = false;
                    foreach (TcpClient client in blockingCollection.ToList())
                    {
                        isOk = isOk || await client.HeartbeatAsync();
                        if (isOk)
                            break;
                    }
                    if (!isOk)
                    {
                        System.Console.WriteLine("与服务端的连接已断开");
                        Environment.Exit(0);
                    }
                },
                null,
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(3)
            );

            try
            {
                while (true)
                {
                    var tunnelClient = new TcpClient();
                    blockingCollection.Add(tunnelClient);
                    System.Console.WriteLine("已连接");
                    _ = Task.Run(async () =>
                    {
                        var cancellationToken = new CancellationTokenSource();
                        await tunnelClient.ConnectAsync(tunnel.ServerIp, tunnel.ServerPort);
                        var tunnelClientStream = tunnelClient.GetStream();
                        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tunnel));
                        await tunnelClientStream.WriteAsync(bytes);

                        var targetClient = new TcpClient();
                        await targetClient.ConnectAsync(tunnel.TargetIp, tunnel.TargePort);
                        var targetClientStream = targetClient.GetStream();

                        var t1 = Task.Run(async () =>
                        {
                            try
                            {
                                await targetClientStream.ForwardAsync(tunnelClientStream);
                            }
                            catch { }
                            finally
                            {
                                await cancellationToken.CancelAsync();
                                targetClientStream.Close();
                            }
                        });
                        var t2 = Task.Run(async () =>
                        {
                            try
                            {
                                await tunnelClientStream.ForwardAsync(targetClientStream);
                            }
                            catch { }
                            finally
                            {
                                await cancellationToken.CancelAsync();
                                tunnelClientStream.Close();
                            }
                        });

                        await Task.WhenAll(t1, t2);
                        blockingCollection.Take();
                    });
                }
            }
            catch
            {
                Environment.Exit(0);
            }
        }
    }
}
