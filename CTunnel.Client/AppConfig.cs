using System.Collections.Concurrent;
using CTunnel.Share.Enums;
using CTunnel.Share.Model;

namespace CTunnel.Client;

public class AppConfig
{
    /// <summary>
    /// 服务器
    /// </summary>
    public UriBuilder Server { get; set; } = null!;

    /// <summary>
    /// 服务器地址
    /// </summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 域名
    /// </summary>
    public string DomainName { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public TunnelTypeEnum Type { get; set; }

    /// <summary>
    /// 目标IP
    /// </summary>
    public UriBuilder Target { get; set; } = null!;

    /// <summary>
    /// 目标地址
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// 连接池（默认值初始化）
    /// </summary>
    public ConcurrentDictionary<string, RequestItem> ConcurrentDictionary { get; set; } = [];

    /// <summary>
    /// 信号（默认值初始化）
    /// </summary>
    public SemaphoreSlim Slim { get; set; } = new(1);
}
