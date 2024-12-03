using System.Collections.Concurrent;
using System.Net.Sockets;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;

namespace CTunnel.Share.Model
{
    public class TunnelModel
    {
        /// <summary>
        /// 监听端口
        /// </summary>
        public int ListenPort { get; set; }

        /// <summary>
        /// 隧道类型
        /// </summary>
        public TunnelTypeEnum Type { get; set; }

        /// <summary>
        /// 域名
        /// </summary>
        public string DomainName { get; set; } = string.Empty;

        /// <summary>
        /// 主连接（客户端与服务端的连接）
        /// </summary>
        public Socket MasterSocket { get; set; } = null!;

        /// <summary>
        /// 主接连流
        /// </summary>
        public Stream MasterSocketStream { get; set; } = null!;

        /// <summary>
        /// 客户端请求监听的连接
        /// </summary>
        public Socket ListenSocket { get; set; } = null!;

        /// <summary>
        /// 心跳计时器
        /// </summary>
        public Timer PulseCheck { get; set; } = null!;

        /// <summary>
        /// 连接取消状态
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = null!;

        /// <summary>
        /// 子连接
        /// </summary>
        public ConcurrentDictionary<string, TunnelSubConnectModel> SubConnect { get; set; } = [];

        /// <summary>
        /// 关闭所有相关内容
        /// </summary>
        /// <returns></returns>
        public async Task CloseAllAsync()
        {
            // 标记取消和关闭计时器
            await CancellationTokenSource.CancelAsync();
            await PulseCheck.DisposeAsync();

            // 关闭主连接
            await MasterSocket.TryCloseAsync();
            await ListenSocket.TryCloseAsync();

            // 子链接全部断开
            foreach (var item in SubConnect)
            {
                await item.Value.CloseAllAsync(this);
            }
        }

        /// <summary>
        /// 创建心跳检查
        /// </summary>
        public void CreateHeartbeatCheck()
        {
            PulseCheck = new Timer(
                async _ =>
                {
                    try
                    {
                        Log.Write($"{DomainName} 心跳", LogType.Important);
                        await MasterSocketStream.SendHeartbeatPacketAsync(
                            CancellationTokenSource.Token
                        );
                    }
                    catch
                    {
                        Log.Write($"{DomainName} 心跳异常，已关闭连接", LogType.Error);
                        await CloseAllAsync();
                    }
                },
                null,
                GlobalStaticConfig.Interval,
                GlobalStaticConfig.Interval
            );
        }
    }
}
