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
            var cancellationToken = new CancellationTokenSource();
            TaskExtend.NewTask(
                async () =>
                {
                    await HandleAsync(webSocket, httpContext.Request.Headers);
                    await cancellationToken.CancelAsync();
                },
                async ex =>
                {
                    Output.Print(ex.Message, OutputMessageTypeEnum.Error);
                    await webSocket.TryCloseAsync();
                    await cancellationToken.CancelAsync();
                }
            );
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken.Token);
        }
        else
        {
            await next(httpContext);
        }
    }

    private async Task HandleAsync(WebSocket webSocket, IHeaderDictionary headers)
    {
        RegisterTunnel? registerTunnel = null;
        if (headers.TryGetValue("Authorization", out var authorizationJson))
        {
            try
            {
                registerTunnel = JsonConvert.DeserializeObject<RegisterTunnel>(
                    authorizationJson.ToString()
                );
            }
            catch
            {
                // ignored
            }
        }
        if (registerTunnel == null || registerTunnel.Token != appConfig.Token)
        {
            await webSocket.SendMessageAsync(WebSocketMessageTypeEnum.ConnectionFail, "鉴权未通过");
            await webSocket.TryCloseAsync();
            return;
        }
        var newTunnel = new TunnelModel
        {
            DomainName = registerTunnel.DomainName,
            Type = registerTunnel.Type,
            ListenPort = registerTunnel.ListenPort,
            WebSocket = webSocket,
        };
        try
        {
            // 根据隧道类型调用服务
            var tunnelType = registerTunnel.Type.ToString();
            var tunnelTypeHandle =
                HostApp.RootServiceProvider.GetRequiredKeyedService<ITunnelTypeHandle>(tunnelType);
            await tunnelTypeHandle.HandleAsync(newTunnel);
            await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
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
