using System.Net;
using CTunnel.Client.Request;
using CTunnel.Client.Response;
using Microsoft.AspNetCore.Mvc;

namespace CTunnel.Client.Controllers
{
    public class AppController(TunnelContext _tunnelContext) : ControllerBase
    {
        [HttpPost("CreateTunnel")]
        public async Task<IActionResult> CreateTunnelAsync([FromBody] CreateTunnelRequest request)
        {
            // 接收创建隧道请求，开启一个任务去运行
            var message = await _tunnelContext.NewWebSocketClientAsync(request);
            return Ok(
                new ApiResult<object?>(
                    message == "成功" ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
                    message,
                    null
                )
            );
        }

        [HttpGet("GetTunnel")]
        public IActionResult GetTunnelAsync()
        {
            var datas = _tunnelContext.GetTunneListAsync();
            return Ok(new ApiResult<List<GetTunneResponse>>(HttpStatusCode.OK, "成功", datas));
        }
    }
}
