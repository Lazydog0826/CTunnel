using System.Net.Sockets;
using CTunnel.Share.Expand;

namespace CTunnel.Share.Model
{
    public delegate Task ClientRequestConnectDelegete();

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
        public Socket MasterSocket { get; set; } = null!;

        /// <summary>
        /// 主连接流
        /// </summary>
        public Stream MasterSocketStream { get; set; } = null!;

        /// <summary>
        /// 网络请求
        /// </summary>
        public Socket ListenSocket { get; set; } = null!;

        /// <summary>
        /// 网络请求流
        /// </summary>
        public Stream ListenSocketStream { get; set; } = null!;

        /// <summary>
        /// 心跳计时器
        /// </summary>
        public Timer PulseCheck { get; set; } = null!;

        /// <summary>
        /// 连接取消状态
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = null!;

        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        public async Task TriggerEventAsync()
        {
            if (ClientRequestConnectEvent != null)
            {
                await ClientRequestConnectEvent.Invoke();
            }
        }

        /// <summary>
        /// 关闭子连接
        /// </summary>
        /// <returns></returns>
        public async Task CloseAllAsync(TunnelModel tunnelModel)
        {
            // 标记取消和关闭计时器
            await CancellationTokenSource.CancelAsync();
            await PulseCheck.DisposeAsync();

            // 关闭连接
            await MasterSocket.TryCloseAsync();
            await ListenSocket.TryCloseAsync();

            // 最后从隧道子连接中移除
            tunnelModel.SubConnect.Remove(RequestId, out var _);
        }

        /// <summary>
        /// 创建心跳检查
        /// </summary>
        public void CreateHeartbeatCheck(TunnelModel tunnelModel)
        {
            PulseCheck = new Timer(
                async _ =>
                {
                    try
                    {
                        if (MasterSocketStream == null)
                        {
                            throw new Exception("规定时间未连接");
                        }
                        Log.Write($"{RequestId} 心跳", LogType.Important);
                        await MasterSocketStream.SendHeartbeatPacketAsync(
                            CancellationTokenSource.Token
                        );
                        await ListenSocketStream.SendHeartbeatPacketAsync(
                            CancellationTokenSource.Token
                        );
                    }
                    catch
                    {
                        Log.Write($"{RequestId} 心跳异常，已关闭连接", LogType.Error);
                        await CloseAllAsync(tunnelModel);
                    }
                },
                null,
                GlobalStaticConfig.Interval,
                GlobalStaticConfig.Interval
            );
        }
    }
}
