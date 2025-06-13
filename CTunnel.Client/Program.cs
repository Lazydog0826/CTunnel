using CTunnel.Client;
using CTunnel.Client.MessageHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniComp.Core.App;
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

    await HostApp.StartConsoleAppAsync(
        args,
        async builder =>
        {
            builder.ConfigureLogging(
                (_, logging) =>
                {
                    logging.ClearProviders();
                }
            );
            builder.ConfigureServices(
                (_, services) =>
                {
                    services.AddKeyedSingleton<IMessageHandle, MessageHandleForward>(
                        nameof(MessageTypeEnum.Forward)
                    );
                    services.AddKeyedSingleton<IMessageHandle, MessageHandleCloseForward>(
                        nameof(MessageTypeEnum.CloseForward)
                    );
                    services.AddHostedService<MainBackgroundService>();
                    var configJson = File.ReadAllText(configFile);
                    Output.PrintConfig(configJson);
                    var appConfig = JsonConvert.DeserializeObject<AppConfig>(configJson);
                    if (appConfig == null)
                    {
                        Output.Print("配置文件有误", OutputMessageTypeEnum.Error);
                        Environment.Exit(0);
                    }

                    appConfig.Server = new UriBuilder(appConfig.ServerUrl);
                    appConfig.Target = new UriBuilder(appConfig.TargetUrl);
                    services.AddSingleton(appConfig);
                }
            );
            await Task.CompletedTask;
        },
        async app =>
        {
            await Task.CompletedTask;
        }
    );
}
catch (Exception ex)
{
    Output.Print(ex.Message, OutputMessageTypeEnum.Error);
    Environment.Exit(0);
}
