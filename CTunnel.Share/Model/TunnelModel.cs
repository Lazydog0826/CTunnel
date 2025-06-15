using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;

namespace CTunnel.Share.Model;

public class TunnelModel
{
    /// <summary>
    /// 隧道的KEY Web服务为对应的域名，Tcp和Udp为端口
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 是否添加成功
    /// </summary>
    public bool IsAdd { get; set; }

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
    /// 主连接
    /// </summary>
    public WebSocket WebSocket { get; set; } = null!;

    /// <summary>
    /// 连接
    /// </summary>
    public ConcurrentDictionary<string, RequestItem> ConcurrentDictionary { get; set; } = [];

    /// <summary>
    /// 客户端请求监听的连接
    /// </summary>
    public Socket? ListenSocket { get; set; }

    /// <summary>
    /// 信号量，阻止发送消息并发
    /// </summary>
    public SemaphoreSlim ForwardToClientSlim { get; set; } = new(1);

    /// <summary>
    /// 关闭所有相关内容
    /// </summary>
    /// <returns></returns>
    public async Task CloseAsync()
    {
        await ListenSocket.TryCloseAsync();
        ForwardToClientSlim.Dispose();
        foreach (var item in ConcurrentDictionary)
        {
            await item.Value.CloseAsync(ConcurrentDictionary);
        }
    }

    /// <summary>
    /// 获取请求项
    /// </summary>
    /// <param name="requestId"></param>
    /// <returns></returns>
    public RequestItem? GetRequestItem(string requestId)
    {
        return ConcurrentDictionary
            .FirstOrDefault(x => x.Value.RequestId.TryGetValue(requestId, out _))
            .Value;
    }
}
