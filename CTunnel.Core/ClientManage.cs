using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using CTunnel.Core.Expand;
using CTunnel.Core.Model;
using CTunnel.Core.TunnelHandle;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CTunnel.Core
{
    public class ClientManage
    {
        public ConcurrentDictionary<string, TunnelModel> Tunnels { get; set; } = [];

        public async Task NewClientAsync(HttpContext httpContext)
        {
            var socket = await httpContext.WebSockets.AcceptWebSocketAsync();
            var newTunnel = httpContext.ReadParameter();

            if (newTunnel != null)
            {
                Console.WriteLine("连接，ID = " + newTunnel.Id);
                if (Tunnels.TryGetValue(newTunnel.Id, out var tunnel))
                {
                    tunnel.ConnectionPool.Add(socket);
                }
                else
                {
                    newTunnel.ConnectionPool.Add(socket);
                    Tunnels.TryAdd(newTunnel.Id, newTunnel);
                    _ = Task.Run(() => CreateListenAsync(newTunnel));
                }

                while (
                    socket.State == WebSocketState.Open || socket.State == WebSocketState.Connecting
                ) { }
                if (Tunnels.TryGetValue(newTunnel.Id, out tunnel))
                {
                    var tunnelHandle =
                        HostApp.ServiceProvider.GetRequiredKeyedService<ITunnelHandle>(
                            tunnel.Type.ToString()
                        );
                    if (await tunnelHandle.IsCloseAsync(tunnel))
                    {
                        Console.WriteLine("隧道关闭，ID = " + newTunnel.Id);
                        Tunnels.Remove(newTunnel.Id, out tunnel);
                    }
                }
            }
            else
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.MandatoryExtension,
                    string.Empty,
                    new CancellationTokenSource().Token
                );
            }
        }

        public async Task CreateListenAsync(TunnelModel tunnelModel)
        {
            tunnelModel.Listener = new TcpListener(IPAddress.Any, tunnelModel.ListenPort);
            tunnelModel.Listener.Start();
            while (true)
            {
                var tcpClient = await tunnelModel.Listener.AcceptTcpClientAsync();
                var tunnelHandle = HostApp.ServiceProvider.GetRequiredKeyedService<ITunnelHandle>(
                    tunnelModel.Type.ToString()
                );
                await tunnelHandle.HandleAsync(tunnelModel, tcpClient);
            }
        }
    }
}
