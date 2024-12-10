using System.Net.Sockets;
using CTunnel.Server.SocketHandle;
using CTunnel.Share;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CTunnel.Server.BackendService
{
    public class HttpsBackendService(AppConfig appConfig) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var socketHandle =
                GlobalStaticConfig.ServiceProvider.GetRequiredKeyedService<ISocketHandle>("Https");
            SocketListen.CreateSocketListen(ProtocolType.Tcp, appConfig.HttpsPort, socketHandle);
            await Task.CompletedTask;
        }
    }
}
