using CTunnel.Server;
using CTunnel.Server.BackendService;
using CTunnel.Server.SocketHandle;
using CTunnel.Server.TunnelTypeHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.CancelKeyPress += (_, _) =>
{
    Environment.Exit(0);
};
Log.WriteLogo();
var configFile = args.FirstOrDefault();
if (string.IsNullOrWhiteSpace(configFile))
{
    Log.Write("未指定配置文件");
    return;
}
var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.Configuration.AddJsonFile(configFile);
var appConfig = builder.Configuration.GetSection(nameof(AppConfig)).Get<AppConfig>()!;
builder.Services.AddSingleton(appConfig);
var certificate = CertificateExtend.LoadPem(appConfig.Certificate, appConfig.CertificateKey);
builder.Services.AddSingleton(certificate);
builder.WebHost.ConfigureKestrel(kso =>
{
    kso.ListenAnyIP(
        appConfig.ServerPort,
        lo =>
        {
            lo.UseHttps(certificate);
        }
    );
});
builder.Services.AddWebSockets(opt => { });
builder.Services.AddSingleton<TunnelContext>();
builder.Services.AddHostedService<HttpBackendService>();
builder.Services.AddHostedService<HttpsBackendService>();
builder.Services.AddSingleton<WebSocketHandleMiddleware>();

#region ISocketHandle

builder.Services.AddKeyedSingleton<ISocketHandle, SocketHandle_Http>("Http");
builder.Services.AddKeyedSingleton<ISocketHandle, SocketHandle_Https>("Https");

#endregion ISocketHandle

#region ITunnelTypeHandle

builder.Services.AddKeyedSingleton<ITunnelTypeHandle, TunnelTypeHandle_Web>(
    nameof(TunnelTypeEnum.Web)
);

#endregion ITunnelTypeHandle

var app = builder.Build();
GlobalStaticConfig.ServiceProvider = app.Services;
app.UseWebSockets();
app.UseMiddleware<WebSocketHandleMiddleware>();

Log.Write($"服务端连接端口：{appConfig.ServerPort}", LogType.Important);
Log.Write($"HTTP端口：{appConfig.HttpPort}", LogType.Important);
Log.Write($"HTTPS端口：{appConfig.HttpsPort}", LogType.Important);
Log.Write("服务端已启动...", LogType.Success);
await app.RunAsync();
