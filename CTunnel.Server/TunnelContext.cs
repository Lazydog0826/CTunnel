using System.Collections.Concurrent;
using System.Text;
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

        public TunnelModel? GetTunnel(string domainName)
        {
            if (_tunnels.TryGetValue(domainName, out var tunnel))
            {
                return tunnel;
            }
            return null;
        }

        public async Task AddTunnelAsync(TunnelModel tunnelModel)
        {
            if (!_tunnels.TryAdd(tunnelModel.DomainName, tunnelModel))
            {
                await tunnelModel.WebSocket.TryCloseAsync();
            }
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
                // 超时时间，如果规定时间内没有传输类型信息就直接关闭
                var timeout = new CancellationTokenSource(GlobalStaticConfig.Interval);
                var memory = new Memory<byte>(new byte[1024 * 1024]);

                // 接收类型消息
                var receiveRes = await socket.ReceiveAsync(memory, timeout.Token);
                var model = JsonConvert.DeserializeObject<WebSocketMessageModel>(
                    Encoding.UTF8.GetString(memory.ToArray())
                )!;

                // 根据消息类型处理数据
                var service =
                    HostApp.ServiceProvider.GetRequiredKeyedService<IWebSocketMessageHandle>(
                        model.MessageType.ToString()
                    );
                await service.HandleAsync(socket, model.JsonData);
            }
            catch
            {
                await socket.TryCloseAsync();
            }
        }
    }
}
