using System.Collections.Concurrent;
using System.Net.Sockets;
using CTunnel.Share.Expand;

namespace CTunnel.Share.Model
{
    public class RequestItem
    {
        public string RequestId { get; set; } = string.Empty;

        public Socket TargetSocket { get; set; } = null!;

        public Stream TargetSocketStream { get; set; } = null!;

        public SemaphoreSlim Slim { get; set; } = new(1);

        public async Task CloseAsync(ConcurrentDictionary<string, RequestItem> pairs)
        {
            pairs.Remove(RequestId, out var _);
            Slim.Dispose();
            await TargetSocket.TryCloseAsync();
        }
    }
}
