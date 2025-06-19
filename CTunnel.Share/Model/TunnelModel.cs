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
    /// 关闭所有相关内容
    /// </summary>
    public async Task CloseAsync()
    {
        await ListenSocket.TryCloseAsync();
        foreach (var item in ConcurrentDictionary)
        {
            await item.Value.TokenSource.CancelAsync();
        }
    }

    /// <summary>
    /// 获取请求项
    /// </summary>
    /// <param name="requestId"></param>
    /// <returns></returns>
    public RequestItem? GetRequestItem(string requestId)
    {
        return ConcurrentDictionary.TryGetValue(requestId, out var item) ? item : null;
    }
}
