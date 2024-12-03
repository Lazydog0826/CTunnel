using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Net.Sockets;
using CTunnel.Server;
using CTunnel.Server.ServerSocketHandle;
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

            #region ISocketHandle

            services.AddKeyedSingleton<ISocketHandle, SocketHandle_Server>("Server");
            services.AddKeyedSingleton<ISocketHandle, SocketHandle_Http>("Http");
            services.AddKeyedSingleton<ISocketHandle, SocketHandle_Https>("Https");

            #endregion ISocketHandle

            #region IServerSocketHandle

            services.AddKeyedSingleton<IServerSocketHandle, ServerSocketHandle_NewRequest>(
                nameof(WebSocketMessageTypeEnum.NewRequest)
            );
            services.AddKeyedSingleton<IServerSocketHandle, ServerSocketHandle_RegisterTunnel>(
                nameof(WebSocketMessageTypeEnum.RegisterTunnel)
            );

            #endregion IServerSocketHandle

            #region ITunnelTypeHandle

            services.AddKeyedSingleton<ITunnelTypeHandle, TunnelTypeHandle_Web>(
                nameof(TunnelTypeEnum.Web)
            );
            services.AddKeyedSingleton<ITunnelTypeHandle, TunnelTypeHandle_Tcp>(
                nameof(TunnelTypeEnum.Tcp)
            );
            services.AddKeyedSingleton<ITunnelTypeHandle, TunnelTypeHandle_Udp>(
                nameof(TunnelTypeEnum.Udp)
            );

            #endregion ITunnelTypeHandle

            services.AddSingleton(_ =>
                CertificateExtend.LoadPem(config.Certificate, config.CertificateKey)
            );
            await Task.CompletedTask;
        });
        var config = ServiceContainer.GetService<AppConfig>();
        SocketListen.CreateSocketListen(
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
            config.ServerPort,
            ServiceContainer.GetService<ISocketHandle>("Server")
        );
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
