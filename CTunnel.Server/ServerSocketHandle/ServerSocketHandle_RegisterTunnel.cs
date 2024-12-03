using System.Net.Sockets;
using CTunnel.Server.TunnelTypeHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Newtonsoft.Json;

namespace CTunnel.Server.ServerSocketHandle
{
    public class ServerSocketHandle_RegisterTunnel(AppConfig _appConfig) : IServerSocketHandle
    {
        public async Task HandleAsync(Socket socket, Stream stream, string jsonData)
        {
            var registerModel = JsonConvert.DeserializeObject<RegisterTunnel>(jsonData)!;
            if (registerModel.Token != _appConfig.Token)
            {
                await stream.SendSocketResultAsync(
                    WebSocketMessageTypeEnum.RegisterTunnel,
                    false,
                    "Token无效",
                    CancellationToken.None
                );
                await socket.TryCloseAsync();
                return;
            }
            var newTimmel = new TunnelModel
            {
                MasterSocket = socket,
                MasterSocketStream = stream,
                DomainName = registerModel.DomainName,
                Type = registerModel.Type,
                ListenPort = registerModel.ListenPort,
                CancellationTokenSource = new CancellationTokenSource()
            };
            newTimmel.CreateHeartbeatCheck();
            var tunnelTypeHandle = ServiceContainer.GetService<ITunnelTypeHandle>(
                registerModel.Type.ToString()
            );
            await tunnelTypeHandle.HandleAsync(newTimmel);
        }
    }
}
