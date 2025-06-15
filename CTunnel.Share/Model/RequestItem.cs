using System.Collections.Concurrent;
using System.Net.Sockets;
using CTunnel.Share.Expand;

namespace CTunnel.Share.Model;

public class RequestItem
{
    /// <summary>
    /// 请求ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public ConcurrentDictionary<string, int> RequestId { get; set; } = [];

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
    public async Task CloseAsync(ConcurrentDictionary<string, RequestItem>? pairs = null)
    {
        if (pairs != null)
        {
            pairs.Remove(Id, out _);
        }
        RequestId.Clear();
        await TargetSocket.TryCloseAsync();
        ForwardToTargetSlim.Dispose();
    }
}
