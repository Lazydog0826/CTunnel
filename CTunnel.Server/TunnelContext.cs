using System.Collections.Concurrent;
using CTunnel.Share.Model;

namespace CTunnel.Server
{
    public class TunnelContext
    {
        private readonly ConcurrentDictionary<string, TunnelModel> _tunnels = [];

        public bool IsRepeat(string key)
        {
            return _tunnels.Any(x => x.Key == key);
        }

        public TunnelModel? GetTunnel(string domainName)
        {
            if (_tunnels.TryGetValue(domainName, out var tunnel))
            {
                return tunnel;
            }
            return null;
        }

        public async Task<bool> AddTunnelAsync(TunnelModel tunnelModel)
        {
            if (IsRepeat(tunnelModel.DomainName))
            {
                return false;
            }
            _tunnels.TryAdd(tunnelModel.DomainName, tunnelModel);
            await Task.CompletedTask;
            return true;
        }

        public async Task RemoveAsync(TunnelModel tunnelModel)
        {
            _tunnels.Remove(tunnelModel.DomainName, out var _);
            await Task.CompletedTask;
        }
    }
}
