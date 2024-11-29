using System.Net.Sockets;
using System.Text;
using CTunnel.Core.Model;
using Newtonsoft.Json;

namespace CTunnel.Core.Expand
{
    public static class TcpClientExtend
    {
        public static async Task<CreateTunnelModel?> ReadCreateTunnelModelAsync(
            this TcpClient tcpClient
        )
        {
            try
            {
                var s = tcpClient.GetStream();
                var memory = new Memory<byte>(new byte[1024 * 1024]);
                var count = await s.ReadAsync(memory);
                var json = Encoding.UTF8.GetString(memory[..count].ToArray());
                return JsonConvert.DeserializeObject<CreateTunnelModel>(json);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> HeartbeatAsync(this TcpClient tcpClient)
        {
            try
            {
                await tcpClient.GetStream().WriteAsync(new byte[1] { 0 });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
