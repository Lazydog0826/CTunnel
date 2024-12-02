﻿using CTunnel.Share.Enums;

namespace CTunnel.Share.Model
{
    public class RegisterTunnelModel
    {
        /// <summary>
        /// 授权码
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 域名
        /// </summary>
        public string DomainName { get; set; } = string.Empty;

        /// <summary>
        /// 监听端口
        /// </summary>
        public int ListenPort { get; set; }

        /// <summary>
        /// 隧道类型
        /// </summary>
        public TunnelTypeEnum Type { get; set; }

        /// <summary>
        /// 文件共享路径
        /// </summary>
        public string FileSharingPath { get; set; } = string.Empty;
    }
}