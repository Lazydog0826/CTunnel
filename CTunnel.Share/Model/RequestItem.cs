using System.Collections.Concurrent;
using System.Net.Sockets;
using CTunnel.Share.Expand;

namespace CTunnel.Share.Model;

public class RequestItem
{
    public string RequestId { get; set; } = string.Empty;

    public Socket TargetSocket { get; set; } = null!;

    public Stream TargetSocketStream { get; set; } = null!;

    /// <summary>
    /// 转发到目标服务限制
    /// </summary>
    public SemaphoreSlim ForwardToTargetSlim { get; set; } = new(1);

    /// <summary>
    /// 关闭
    /// </summary>
    /// <param name="pairs"></param>
    public async Task CloseAsync(ConcurrentDictionary<string, RequestItem> pairs)
    {
        pairs.Remove(RequestId, out _);
        await TargetSocket.TryCloseAsync();
        ForwardToTargetSlim.Dispose();
    }
}
