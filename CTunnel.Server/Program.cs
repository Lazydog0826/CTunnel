using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Net.Sockets;
using CTunnel.Server;
using CTunnel.Server.SocketHandle;
using CTunnel.Server.TunnelTypeHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var rootCommand = new RootCommand();
var configOption = new Option<string>("-c", "配置文件");
rootCommand.AddOption(configOption);

rootCommand.SetHandler(
    async configPath =>
    {
        Log.WriteLogo();
        await ServiceContainer.RegisterServiceAsync(async services =>
        {
            var configuration = new ConfigurationBuilder().AddJsonFile(configPath).Build();
            var config = configuration.GetConfig<AppConfig>();
            services.AddSingleton(config);
            services.AddSingleton<TunnelContext>();
            services.AddSingleton<WebSocketHandle>();

            #region ISocketHandle

            services.AddKeyedSingleton<ISocketHandle, SocketHandle_Http>("Http");
            services.AddKeyedSingleton<ISocketHandle, SocketHandle_Https>("Https");

            #endregion ISocketHandle

            #region ITunnelTypeHandle

            services.AddKeyedSingleton<ITunnelTypeHandle, TunnelTypeHandle_Web>(
                nameof(TunnelTypeEnum.Web)
            );

            #endregion ITunnelTypeHandle

            services.AddSingleton(_ =>
                CertificateExtend.LoadPem(config.Certificate, config.CertificateKey)
            );
            await Task.CompletedTask;
        });
        var config = ServiceContainer.GetService<AppConfig>();
        SocketListen.CreateWebSocketListen(config);
        SocketListen.CreateSocketListen(
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
            config.HttpPort,
            ServiceContainer.GetService<ISocketHandle>("Http")
        );
        SocketListen.CreateSocketListen(
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
            config.HttpsPort,
            ServiceContainer.GetService<ISocketHandle>("Https")
        );
        await Task.Delay(Timeout.InfiniteTimeSpan);
    },
    configOption
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
