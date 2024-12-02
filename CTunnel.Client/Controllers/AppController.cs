using CTunnel.Client.Request;
using Microsoft.AspNetCore.Mvc;

namespace CTunnel.Client.Controllers
{
    public class AppController(TunnelContext _tunnelContext) : ControllerBase
    {
        public async Task CreateTunnelAsync([FromBody] CreateTunnelRequest request)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _tunnelContext.NewWebSocketClientAsync(request);
                }
                catch { }
            });
            await Task.CompletedTask;
        }
    }
}
