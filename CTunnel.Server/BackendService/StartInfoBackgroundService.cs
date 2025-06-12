using CTunnel.Share;
using Microsoft.Extensions.Hosting;

namespace CTunnel.Server.BackendService;

public class StartInfoBackgroundService : BackgroundService
{
    private readonly AppConfig _config;

    public StartInfoBackgroundService(AppConfig config)
    {
        _config = config;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Output.Print($"服务端连接端口：{_config.ServerPort}");
        Output.Print($"HTTP端口：{_config.HttpPort}");
        Output.Print($"HTTPS端口：{_config.HttpsPort}");
        Output.Print("服务端已启动");
        return Task.CompletedTask;
    }
}
