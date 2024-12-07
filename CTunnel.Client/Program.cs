﻿using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Security.Cryptography.X509Certificates;
using CTunnel.Client;
using CTunnel.Client.MessageHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using Microsoft.Extensions.DependencyInjection;

var rootCommand = new RootCommand();

var serverOption = new Option<string>("--server", "服务端IP加端口");
rootCommand.AddOption(serverOption);
var tokenOption = new Option<string>("--token", "Token令牌");
rootCommand.AddOption(tokenOption);
var domainNameOption = new Option<string>("--domain", "域名，Web服务需要域名");
rootCommand.AddOption(domainNameOption);
var listenPortOption = new Option<int>("--port", "端口，Web服务不需要");
rootCommand.AddOption(listenPortOption);
var typeOption = new Option<string>("--type", "隧道类型，可选值：Web，Tcp，Udp");
rootCommand.AddOption(typeOption);
var targetOption = new Option<string>("--target", "转发目标IP加端口");
rootCommand.AddOption(targetOption);

rootCommand.SetHandler(
    async (server, token, domain, port, type, target) =>
    {
        Log.WriteLogo();
        await ServiceContainer.RegisterServiceAsync(async services =>
        {
            await Task.CompletedTask;
            services.AddSingleton<X509Certificate2>();
            services.AddKeyedSingleton<IMessageHandle, MessageHandle_Forward>(
                nameof(MessageTypeEnum.Forward)
            );
            services.AddKeyedSingleton<IMessageHandle, MessageHandle_CloseForward>(
                nameof(MessageTypeEnum.CloseForward)
            );
        });
        var config = new AppConfig()
        {
            Token = token,
            DomainName = domain,
            Port = port,
            Server = new UriBuilder(server),
            Target = new UriBuilder(target),
            Type = (TunnelTypeEnum)Enum.Parse(typeof(TunnelTypeEnum), type)
        };
        await MainHandle.HandleAsync(config);
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
