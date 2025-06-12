using CTunnel.Share;
using Microsoft.Extensions.Hosting;

namespace CTunnel.Server.BackendService;

public class StartInfoBackgroundService(AppConfig config) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Output.Print($"websocket 端口 {config.ServerPort}");
        Output.Print($"http 端口 {config.HttpPort}");
        Output.Print($"https 端口 {config.HttpsPort}");
        Output.Print("服务已启动");
        return Task.CompletedTask;
    }
}
