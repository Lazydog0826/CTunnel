using System.Net.Sockets;

namespace CTunnel.Server.ServerSocketHandle
{
    /// <summary>
    /// 处理客户端连接
    /// </summary>
    public interface IServerSocketHandle
    {
        public Task HandleAsync(Socket socket, Stream stream, string jsonData);
    }
}
