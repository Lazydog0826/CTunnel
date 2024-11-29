using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;

namespace CTunnel.Core
{
    public class TcpListenHostService : BackgroundService
    {
        private readonly ClientManage _clientManage;

        public TcpListenHostService(ClientManage clientManage)
        {
            _clientManage = clientManage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tcp = new TcpListener(IPAddress.Any, 5200);
            tcp.Start();
            while (true)
            {
                await _clientManage.NewClientAsync(await tcp.AcceptTcpClientAsync());
            }
        }
    }
}
