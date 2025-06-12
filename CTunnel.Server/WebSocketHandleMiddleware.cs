using System.Net.WebSockets;
using CTunnel.Server.TunnelTypeHandle;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MiniComp.Core.App;
using Newtonsoft.Json;

namespace CTunnel.Server;

public class WebSocketHandleMiddleware(
    RequestDelegate next,
    AppConfig appConfig,
    TunnelContext tunnelContext
)
{
    public async Task Invoke(HttpContext httpContext)
    {
        if (httpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
            var cancellationTokenSource = new CancellationTokenSource();
            TaskExtend.NewTask(
                async () =>
                {
                    await HandleAsync(
                        webSocket,
                        httpContext.Request.Headers,
                        cancellationTokenSource
                    );
                    await webSocket.TryCloseAsync();
                    await cancellationTokenSource.CancelAsync();
                },
                async ex =>
                {
                    await webSocket.TryCloseAsync(ex.Message);
                    await cancellationTokenSource.CancelAsync();
                    Output.Print(ex.Message, OutputMessageTypeEnum.Error);
                }
            );
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);
        }
        else
        {
            await next(httpContext);
        }
    }

    private async Task HandleAsync(
        WebSocket webSocket,
        IHeaderDictionary headers,
        CancellationTokenSource cancellationToken
    )
    {
        RegisterTunnel registerTunnelParam;
        if (headers.TryGetValue("RegisterTunnelParam", out var registerTunnelParamJson))
        {
            registerTunnelParam =
                JsonConvert.DeserializeObject<RegisterTunnel>(registerTunnelParamJson.ToString())
                ?? throw new Exception("BadRequest");
        }
        else
        {
            throw new Exception("BadRequest");
        }

        // 检查Token
        if (registerTunnelParam.Token != appConfig.Token)
        {
            throw new Exception("Token无效");
        }

        var newTunnel = new TunnelModel
        {
            CancellationTokenSource = cancellationToken,
            DomainName = registerTunnelParam.DomainName,
            Type = registerTunnelParam.Type,
            ListenPort = registerTunnelParam.ListenPort,
            WebSocket = webSocket,
            Slim = new SemaphoreSlim(1),
        };

        try
        {
            // 根据隧道类型调用服务
            var tunnelTypeHandle =
                HostApp.RootServiceProvider.GetRequiredKeyedService<ITunnelTypeHandle>(
                    registerTunnelParam.Type.ToString()
                );
            await tunnelTypeHandle.HandleAsync(newTunnel);
        }
        finally
        {
            if (newTunnel.IsAdd)
            {
                await tunnelContext.RemoveTunnelAsync(newTunnel.Key);
            }
            else
            {
                await newTunnel.CloseAsync();
            }
            Output.Print($"{newTunnel.Key} - 连接已断开", OutputMessageTypeEnum.Error);
        }
    }
}
