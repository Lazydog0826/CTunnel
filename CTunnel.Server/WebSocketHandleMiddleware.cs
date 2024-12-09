using System.Net;
using System.Net.WebSockets;
using CTunnel.Server.TunnelTypeHandle;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace CTunnel.Server
{
    public class WebSocketHandleMiddleware(AppConfig _appConfig, TunnelContext tunnelContext)
    {
        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                TaskExtend.NewTask(
                    async () =>
                    {
                        await HandleAsync(webSocket, httpContext.Request.Headers);
                        await webSocket.TryCloseAsync();
                    },
                    async ex =>
                    {
                        await webSocket.TryCloseAsync();
                        Log.Write(ex.Message, LogType.Error);
                    }
                );
            }
            else
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        public async Task HandleAsync(WebSocket webSocket, IHeaderDictionary headers)
        {
            var slim = new SemaphoreSlim(1);
            RegisterTunnel registerTunnelParam;
            if (headers.TryGetValue("RegisterTunnelParam", out var registerTunnelParamJson))
            {
                registerTunnelParam = JsonConvert.DeserializeObject<RegisterTunnel>(
                    registerTunnelParamJson.ToString()
                )!;
            }
            else
            {
                throw new Exception("必要的请求头不存在");
            }

            // 检查Token
            if (registerTunnelParam.Token != _appConfig.Token)
            {
                var result = new WebSocketResult { Success = false, Message = "Token无效" };
                await webSocket.SendMessageAsync(result, slim);
                throw new Exception(result.Message);
            }

            var newTimmel = new TunnelModel
            {
                DomainName = registerTunnelParam.DomainName,
                Type = registerTunnelParam.Type,
                ListenPort = registerTunnelParam.ListenPort,
                WebSocket = webSocket,
                Slim = slim
            };

            try
            {
                // 根据隧道类型调用服务
                var tunnelTypeHandle =
                    GlobalStaticConfig.ServiceProvider.GetRequiredKeyedService<ITunnelTypeHandle>(
                        registerTunnelParam.Type.ToString()
                    );
                await tunnelTypeHandle.HandleAsync(newTimmel);
            }
            finally
            {
                await tunnelContext.RemoveAsync(newTimmel);
            }
        }
    }
}
