using System.Collections.Concurrent;
using CTunnel.Server.WebSocketMessageHandle;
using CTunnel.Share;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Server
{
    public class TunnelContext
    {
        private readonly ConcurrentDictionary<string, TunnelModel> _tunnels = [];

        public bool IsRepeat(string key)
        {
            return _tunnels.Any(x => x.Key == key);
        }

        public TunnelModel? GetTunnel(string domainName)
        {
            if (_tunnels.TryGetValue(domainName, out var tunnel))
            {
                return tunnel;
            }
            return null;
        }

        public async Task<bool> AddTunnelAsync(TunnelModel tunnelModel)
        {
            if (IsRepeat(tunnelModel.DomainName))
            {
                return false;
            }
            _tunnels.TryAdd(tunnelModel.DomainName, tunnelModel);
            await Task.CompletedTask;
            return true;
        }

        public async Task RemoveAsync(TunnelModel tunnelModel)
        {
            _tunnels.Remove(tunnelModel.DomainName, out var _);
            await Task.CompletedTask;
        }

        public async Task NewWebSocketClientAsync(WebSocketManager webSocketManager)
        {
            var socket = await webSocketManager.AcceptWebSocketAsync();
            try
            {
                // 超时时间，如果规定时间内交换完信息就直接关闭此连接
                var timeout = new CancellationTokenSource(GlobalStaticConfig.Interval);
                var memory = new Memory<byte>(new byte[1024 * 1024]);

                // 接收类型消息
                var wsMessage = await socket.ReadModelAsync<WebSocketMessageModel>(timeout.Token);
                Log.Write("新的WebSocket连接");
                Log.Write(JsonConvert.SerializeObject(wsMessage));

                // 根据消息类型处理数据
                var service =
                    HostApp.ServiceProvider.GetRequiredKeyedService<IWebSocketMessageHandle>(
                        wsMessage.MessageType.ToString()
                    );
                await service.HandleAsync(socket, wsMessage.JsonData);
            }
            catch (OperationCanceledException)
            {
                Log.Write("WebSocket连接超时", LogType.Error);
            }
            catch (Exception ex)
            {
                await socket.TryCloseAsync();
                Log.Write(ex.Message, LogType.Error);
            }
        }
    }
}
