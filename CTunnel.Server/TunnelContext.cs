using System.Collections.Concurrent;
using CTunnel.Share.Model;

namespace CTunnel.Server;

public class TunnelContext
{
    private readonly ConcurrentDictionary<string, TunnelModel> _tunnels = [];

    public TunnelModel? GetTunnel(string key)
    {
        if (_tunnels.TryGetValue(key, out var t))
        {
            return t;
        }
        return null;
    }

    public bool AddTunnel(TunnelModel tunnel)
    {
        return _tunnels.TryAdd(tunnel.Key, tunnel);
    }

    public async Task RemoveTunnelAsync(string key)
    {
        if (_tunnels.TryGetValue(key, out var tunnel))
        {
            _tunnels.Remove(key, out var _);
            await tunnel.CloseAsync();
        }
    }
}
