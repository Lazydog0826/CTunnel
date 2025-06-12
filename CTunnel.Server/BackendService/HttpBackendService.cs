using System.Net.Sockets;
using CTunnel.Server.SocketHandle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniComp.Core.App;

namespace CTunnel.Server.BackendService;

public class HttpBackendService(AppConfig appConfig) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var socketHandle = HostApp.RootServiceProvider.GetRequiredKeyedService<ISocketHandle>(
            "Http"
        );
        SocketListen.CreateSocketListen(ProtocolType.Tcp, appConfig.HttpPort, socketHandle);
        await Task.CompletedTask;
    }
}
