﻿using CTunnel.Share.Enums;

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
    /// 转发到服务器限制
    /// </summary>
    public SemaphoreSlim ForwardToServerSlim { get; set; } = new(1);

    /// <summary>
    /// Socket创建限制
    /// </summary>
    public SemaphoreSlim SocketCreateSlim { get; set; } = new(20);
}
