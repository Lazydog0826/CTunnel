using CTunnel.Server;
using CTunnel.Server.BackendService;
using CTunnel.Share;
using CTunnel.Share.Expand;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniComp.Autofac;
using MiniComp.Core.App;
using MiniComp.Core.Extension;
using Newtonsoft.Json;

try
{
    Console.CancelKeyPress += (_, _) =>
    {
        Environment.Exit(0);
    };

    var configFile = args.FirstOrDefault();
    if (string.IsNullOrWhiteSpace(configFile))
    {
        Output.Print("未指定配置文件", OutputMessageTypeEnum.Error);
        return;
    }

    await HostApp.StartWebAppAsync(
        [],
        async builder =>
        {
            builder.Host.UseAutofac();
            builder.Services.AutoAddDependency(
                ObjectExtension
                    .GetAssemblyByRegex(AssemblyRegex.Create())
                    .GetTypeListByAssemblyList()
            );
            builder.Logging.ClearProviders();
            var configJson = File.ReadAllText(configFile);
            Output.PrintConfig(configJson);
            var appConfig = JsonConvert.DeserializeObject<AppConfig>(configJson);
            if (appConfig == null)
            {
                Output.Print("配置文件有误", OutputMessageTypeEnum.Error);
                Environment.Exit(0);
            }
            builder.Services.AddSingleton(appConfig);
            var certificate = CertificateExtend.LoadPem(
                appConfig.Certificate,
                appConfig.CertificateKey
            );
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
            builder.Services.AddWebSockets(_ => { });
            builder.Services.AddHostedService<HttpBackendService>();
            builder.Services.AddHostedService<HttpsBackendService>();
            builder.Services.AddHostedService<RequestSocketBackgroundService>();

            await Task.CompletedTask;
        },
        async app =>
        {
            app.UseWebSockets();
            app.UseMiddleware<WebSocketHandleMiddleware>();
            await Task.CompletedTask;
        }
    );
}
catch (Exception ex)
{
    Output.Print(ex.Message, OutputMessageTypeEnum.Error);
    Environment.Exit(0);
}
