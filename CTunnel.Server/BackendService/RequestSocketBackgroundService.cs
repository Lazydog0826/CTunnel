using System.Net.Sockets;
using CTunnel.Server.SocketHandle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniComp.Core.App;

namespace CTunnel.Server.BackendService;

public class RequestSocketBackgroundService(AppConfig appConfig) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var socketHandle = HostApp.RootServiceProvider.GetRequiredKeyedService<ISocketHandle>(
            "Request"
        );
        SocketListen.CreateSocketListen(ProtocolType.Tcp, appConfig.ServerPort + 1, socketHandle);
        await Task.CompletedTask;
    }
}
