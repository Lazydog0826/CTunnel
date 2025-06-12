using System.Net;
using System.Net.Sockets;
using CTunnel.Server.SocketHandle;
using CTunnel.Share;
using CTunnel.Share.Expand;

namespace CTunnel.Server;

public static class SocketListen
{
    public static Socket CreateSocketListen(
        ProtocolType protocolType,
        int port,
        ISocketHandle socketHandle
    )
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, protocolType);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        socket.Listen();
        TaskExtend.NewTask(async () =>
        {
            while (true)
            {
                var newConnect = await socket.AcceptAsync();
                TaskExtend.NewTask(
                    async () =>
                    {
                        await socketHandle.HandleAsync(newConnect, port);
                        await newConnect.TryCloseAsync();
                    },
                    async ex =>
                    {
                        Output.Print(ex.Message, OutputMessageTypeEnum.Error);
                        await newConnect.TryCloseAsync();
                    }
                );
            }
            // ReSharper disable once FunctionNeverReturns
        });
        return socket;
    }
}
