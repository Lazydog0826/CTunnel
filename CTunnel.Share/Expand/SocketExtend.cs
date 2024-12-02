using System.Net.Sockets;

namespace CTunnel.Share.Expand
{
    public static class SocketExtend
    {
        public static async Task TryCloseAsync(this Socket? socket)
        {
            if (socket != null)
            {
                try
                {
                    socket.Close();
                    await Task.CompletedTask;
                }
                catch { }
            }
        }
    }
}
