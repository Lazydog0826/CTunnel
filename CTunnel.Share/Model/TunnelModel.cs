﻿using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
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
        public Socket ListenSocket { get; set; } = null!;

        /// <summary>
        /// 关闭所有相关内容
        /// </summary>
        /// <returns></returns>
        public async Task CloseAllAsync()
        {
            await WebSocket.TryCloseAsync();
            await ListenSocket.TryCloseAsync();

            // 子链接全部断开
            foreach (var item in ConcurrentDictionary)
            {
                await item.Value.CloseAllAsync(ConcurrentDictionary);
            }
        }

        /// <summary>
        /// 获取请求项
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public RequestItem? GetRequestItem(string requestId)
        {
            if (ConcurrentDictionary.TryGetValue(requestId, out var ri))
            {
                return ri;
            }
            return null;
        }

        /// <summary>
        /// 移除请求项
        /// </summary>
        /// <param name="requestId"></param>
        public async void RemoveRequestItem(string requestId)
        {
            if (ConcurrentDictionary.TryGetValue(requestId, out var ri))
            {
                await ri.TargetSocket.TryCloseAsync();
                ConcurrentDictionary.Remove(requestId, out var _);
            }
        }
    }
}
