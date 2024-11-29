using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using CTunnel.Core.Expand;
using CTunnel.Core.Model;
using CTunnel.Core.TunnelHandle;
using Microsoft.Extensions.DependencyInjection;

namespace CTunnel.Core
{
    public class ClientManage
    {
        public ConcurrentDictionary<string, TunnelModel> Tunnels { get; set; } = [];

        private readonly object _lock = new();

        public async Task NewClientAsync(TcpClient tcpClient)
        {
            var createTunnelModel = await tcpClient.ReadCreateTunnelModelAsync();

            if (createTunnelModel != null)
            {
                Console.WriteLine("连接，ID = " + createTunnelModel.Id);

                lock (_lock)
                {
                    if (Tunnels.TryGetValue(createTunnelModel.Id, out var tunnel))
                    {
                        tunnel.ConnectionPool.Add(tcpClient);
                    }
                    else
                    {
                        var newTunnel = new TunnelModel
                        {
                            AuthCode = createTunnelModel.AuthCode,
                            Id = createTunnelModel.Id,
                            ListenPort = createTunnelModel.ListenPort,
                            Type = createTunnelModel.Type
                        };
                        newTunnel.ConnectionPool.Add(tcpClient);
                        Tunnels.TryAdd(newTunnel.Id, newTunnel);
                        _ = Task.Run(() => CreateListenAsync(newTunnel));
                    }
                }
            }
            else
            {
                tcpClient.Close();
            }
        }

        public async Task CreateListenAsync(TunnelModel tunnelModel)
        {
            tunnelModel.Listener = new TcpListener(IPAddress.Any, tunnelModel.ListenPort);
            tunnelModel.Listener.Start();
            Console.WriteLine("已监听：" + tunnelModel.ListenPort);
            var cancellationToken = new CancellationTokenSource();

            var tunnelHandle = HostApp.ServiceProvider.GetRequiredKeyedService<ITunnelHandle>(
                tunnelModel.Type.ToString()
            );

            tunnelModel.Timer = new Timer(
                async state =>
                {
                    if (await tunnelHandle.IsCloseAsync(tunnelModel))
                    {
                        (state as Timer)?.Dispose();
                        Console.WriteLine("隧道关闭，ID = " + tunnelModel.Id);
                        Tunnels.Remove(tunnelModel.Id, out var _);
                        tunnelModel.Listener.Stop();
                        await cancellationToken.CancelAsync();
                    }
                },
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(3)
            );

            while (!cancellationToken.IsCancellationRequested)
            {
                var tcpClient = await tunnelModel.Listener.AcceptTcpClientAsync(
                    cancellationToken.Token
                );
                Console.WriteLine("浏览器连接，ID = " + tunnelModel.Id);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await tunnelHandle.HandleAsync(tunnelModel, tcpClient);
                    }
                    catch { }
                });
            }
        }
    }
}
