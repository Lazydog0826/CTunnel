using System.Buffers;
using System.Net.Sockets;

namespace CTunnel.Share.Model;

public class RequestItem
{
    /// <summary>
    /// 请求ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 目标Socket
    /// </summary>
    public Socket TargetSocket { get; set; } = null!;

    /// <summary>
    /// 目标Stream
    /// </summary>
    public Stream TargetSocketStream { get; set; } = null!;

    /// <summary>
    /// 转发Socket
    /// </summary>
    public Socket ForwardSocket { get; set; } = null!;

    /// <summary>
    /// 转发Stream
    /// </summary>
    public Stream ForwardSocketStream { get; set; } = null!;

    /// <summary>
    /// 取消信号
    /// </summary>
    public CancellationTokenSource TokenSource { get; set; } = new();

    /// <summary>
    /// 待发送
    /// </summary>
    public IMemoryOwner<byte>? ToBeSent { get; set; }

    /// <summary>
    /// 待发送字节数
    /// </summary>
    public int ToBeSentCount { get; set; }
}
