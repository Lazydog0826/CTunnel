using System.Net.Sockets;

namespace CTunnel.Server.SocketHandle;

/// <summary>
/// 监听服务端，http，https接口
/// </summary>
public interface ISocketHandle
{
    public Task HandleAsync(Socket socket, int port);
}
