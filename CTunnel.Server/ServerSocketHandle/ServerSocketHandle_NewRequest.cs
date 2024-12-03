using System.Net.Sockets;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Server.ServerSocketHandle
{
    public class ServerSocketHandle_NewRequest(AppConfig _appConfig, TunnelContext _tunnelContext)
        : IServerSocketHandle
    {
        public async Task HandleAsync(Socket socket, Stream stream, string jsonData)
        {
            var requestModel = JsonConvert.DeserializeObject<NewRequest>(jsonData)!;
            Log.Write($"{requestModel.Host} 接收到来自客户端的请求Socket连接", LogType.Success);
            if (requestModel.Token != _appConfig.Token)
            {
                Log.Write($"{requestModel.Host} Token验证失败", LogType.Error);
                await socket.TryCloseAsync();
                return;
            }
            var tunnel = _tunnelContext.GetTunnel(requestModel.DomainName);
            if (tunnel == null)
            {
                Log.Write($"{requestModel.Host} 隧道未找到", LogType.Error);
                await socket.TryCloseAsync();
                return;
            }
            if (tunnel.SubConnect.TryGetValue(requestModel.RequestId, out var tunnelSubConnect))
            {
                tunnelSubConnect.MasterSocket = socket;
                tunnelSubConnect.MasterSocketStream = stream;

                await tunnelSubConnect.MasterSocketStream.SendSocketResultAsync(
                    WebSocketMessageTypeEnum.NewRequest,
                    true,
                    "成功",
                    tunnelSubConnect.CancellationTokenSource.Token
                );
                Log.Write($"{requestModel.Host} 验证成功，触发事件", LogType.Success);
                await tunnelSubConnect.TriggerEventAsync();
            }
            else
            {
                Log.Write($"{requestModel.Host} 请求ID未找到", LogType.Error);
                await socket.TryCloseAsync();
            }
        }
    }
}
