using System.Net.Sockets;

namespace CTunnel.Core.Expand
{
    public static class NetworkStreamExtend
    {
        public static async Task ForwardAsync(this NetworkStream stream, NetworkStream stream2)
        {
            var memory = new Memory<byte>(new byte[1024 * 1024]);
            int count;
            while ((count = await stream.ReadAsync(memory)) > 0)
            {
                var bytes = memory[..count].ToArray();
                if (count > 1 || bytes[0] != 0)
                {
                    await stream2.WriteAsync(bytes);
                }
            }
        }
    }
}
