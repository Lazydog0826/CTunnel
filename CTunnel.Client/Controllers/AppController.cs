using CTunnel.Client.Request;
using Microsoft.AspNetCore.Mvc;

namespace CTunnel.Client.Controllers
{
    public class AppController(TunnelContext _tunnelContext) : ControllerBase
    {
        [HttpPost("CreateTunnel")]
        public async Task CreateTunnelAsync([FromBody] CreateTunnelRequest request)
        {
            // 接收创建隧道请求，开启一个任务去运行
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
