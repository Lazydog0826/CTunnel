using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using CTunnel.Console.Client;
using CTunnel.Core.Enums;

var rootCommand = new RootCommand();

var serverIpOption = new Option<string>(name: "-s");
var serverPortOption = new Option<int>(name: "-sp");
var listenPortOption = new Option<int>(name: "-lp");
var authCodeOption = new Option<string>(name: "-a");
var typeOption = new Option<string>(name: "-tt");
var targetIpOption = new Option<string>(name: "-t");
var targePortOption = new Option<int>(name: "-tp");

rootCommand.AddOption(serverIpOption);
rootCommand.AddOption(serverPortOption);
rootCommand.AddOption(listenPortOption);
rootCommand.AddOption(authCodeOption);
rootCommand.AddOption(typeOption);
rootCommand.AddOption(targetIpOption);
rootCommand.AddOption(targePortOption);

rootCommand.SetHandler(
    async (serverIp, serverPort, listenPort, authCode, type, targetIp, targePort) =>
    {
        if (type == TunnelTypeEnum.Http.ToString())
        {
            await new HttpHandle().HandleAsync(
                new CTunnel.Core.Model.CreateTunnelModel
                {
                    Type = TunnelTypeEnum.Http,
                    AuthCode = authCode,
                    ListenPort = listenPort,
                    Id = Guid.NewGuid().ToString(),
                    ServerIp = serverIp,
                    ServerPort = serverPort,
                    TargePort = targePort,
                    TargetIp = targetIp,
                }
            );
        }
    },
    serverIpOption,
    serverPortOption,
    listenPortOption,
    authCodeOption,
    typeOption,
    targetIpOption,
    targePortOption
);

var commandLineBuilder = new CommandLineBuilder(rootCommand);
commandLineBuilder.UseDefaults();
var parser = commandLineBuilder.Build();
return await parser.InvokeAsync(args);
