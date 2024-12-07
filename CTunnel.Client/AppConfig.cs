﻿using CTunnel.Share.Enums;

namespace CTunnel.Client
{
    public class AppConfig
    {
        /// <summary>
        /// 唯一ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 服务器IP
        /// </summary>
        public UriBuilder Server { get; set; } = null!;

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
    }
}