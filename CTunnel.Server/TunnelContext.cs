using System.Collections.Concurrent;
using CTunnel.Share.Model;

namespace CTunnel.Server
{
    public class TunnelContext
    {
        private readonly ConcurrentDictionary<string, TunnelModel> _tunnels = [];

        public TunnelModel? GetTunnel(string host)
        {
            if (_tunnels.TryGetValue(host, out var t))
            {
                return t;
            }
            return null;
        }

        public bool AddTunnel(TunnelModel tunnelModel)
        {
            return _tunnels.TryAdd(tunnelModel.DomainName, tunnelModel);
        }

        public async Task RemoveAsync(TunnelModel tunnelModel)
        {
            _tunnels.Remove(tunnelModel.DomainName, out var _);
            await tunnelModel.CloseAsync();
        }
    }
}
