namespace CTunnel.Server
{
    public class ListenWebSocketMiddleware(RequestDelegate next, TunnelContext tunnelContext)
    {
        private readonly RequestDelegate _next = next;
        private readonly TunnelContext _tunnelContext = tunnelContext;

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await _tunnelContext.NewWebSocketClientAsync(context.WebSockets);
            }
            else
            {
                await _next(context);
            }
        }
    }
}
