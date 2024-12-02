using System.Net.Sockets;
using System.Net.WebSockets;

namespace CTunnel.Share.Model
{
    public delegate Task ClientRequestConnectDelegete(WebSocket webSocket);

    public class TunnelSubConnectModel
    {
        /// <summary>
        /// 客户端连接事件
        /// </summary>
        public event ClientRequestConnectDelegete ClientRequestConnectEvent = null!;

        /// <summary>
        /// 请求ID
        /// </summary>
        public string RequestId { get; set; } = string.Empty;

        /// <summary>
        /// 客户端连接对象
        /// </summary>
        public WebSocket WebSocket { get; set; } = null!;

        /// <summary>
        /// 网络请求
        /// </summary>
        public Socket Socket { get; set; } = null!;

        /// <summary>
        /// 心跳计时器
        /// </summary>
        public Timer PulseCheck { get; set; } = null!;

        /// <summary>
        /// 连接取消状态
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = null!;

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        public async Task CloseConnectAsync()
        {
            await CancellationTokenSource.CancelAsync();
            await WebSocket.CloseAsync(
                WebSocketCloseStatus.Empty,
                string.Empty,
                CancellationToken.None
            );
            await PulseCheck.DisposeAsync();
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        public async Task TriggerEventAsync(WebSocket webSocket)
        {
            if (ClientRequestConnectEvent != null)
            {
                await ClientRequestConnectEvent.Invoke(webSocket);
            }
        }
    }
}
