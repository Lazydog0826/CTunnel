using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CTunnel.Core
{
    public class WebSocketsListenMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await context
                    .RequestServices.GetRequiredService<ClientManage>()
                    .NewClientAsync(context);
            }
            else
            {
                await _next(context);
            }
        }
    }
}
