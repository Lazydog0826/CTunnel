using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using CTunnel.Client;
using CTunnel.Client.MessageHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var rootCommand = new RootCommand();

var serverOption = new Option<string>("--server", "服务端");
rootCommand.AddOption(serverOption);
var tokenOption = new Option<string>("--token", "Token");
rootCommand.AddOption(tokenOption);
var domainNameOption = new Option<string>("--domain", "域名");
rootCommand.AddOption(domainNameOption);
var listenPortOption = new Option<int>("--port", "端口，如果是Web服务则不需要端口");
rootCommand.AddOption(listenPortOption);
var typeOption = new Option<string>("--type", "隧道类型，可选值：Web，Tcp，Udp");
rootCommand.AddOption(typeOption);
var targetOption = new Option<string>("--target", "转发的目标");
rootCommand.AddOption(targetOption);

rootCommand.SetHandler(
    async (server, token, domain, port, type, target) =>
    {
        Log.WriteLogo();
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
        builder.UseConsoleLifetime();
        builder.ConfigureServices(services =>
        {
            services.AddKeyedSingleton<IMessageHandle, MessageHandle_Forward>(
                nameof(MessageTypeEnum.Forward)
            );
            services.AddKeyedSingleton<IMessageHandle, MessageHandle_CloseForward>(
                nameof(MessageTypeEnum.CloseForward)
            );
            var config = new AppConfig()
            {
                Token = token,
                DomainName = domain,
                Port = port,
                Server = new UriBuilder(server),
                Target = new UriBuilder(target),
                Type = (TunnelTypeEnum)Enum.Parse(typeof(TunnelTypeEnum), type)
            };
            services.AddSingleton(config);
            services.AddHostedService<MainBackgroundService>();
        });
        var app = builder.Build();
        GlobalStaticConfig.ServiceProvider = app.Services;
        await app.RunAsync();
    },
    serverOption,
    tokenOption,
    domainNameOption,
    listenPortOption,
    typeOption,
    targetOption
);
var commandLineBuilder = new CommandLineBuilder(rootCommand);
commandLineBuilder.AddMiddleware(
    async (context, next) =>
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            Log.Write(ex.Message, LogType.Error);
        }
    }
);
commandLineBuilder.UseDefaults();
var parser = commandLineBuilder.Build();
return await parser.InvokeAsync(args);
