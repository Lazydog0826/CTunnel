using System.Collections.Concurrent;
using CTunnel.Share.Model;
using Microsoft.Extensions.DependencyInjection;
using MiniComp.Autofac;

namespace CTunnel.Server;

[AutofacDependency(typeof(TunnelContext), ServiceLifetime = ServiceLifetime.Singleton)]
public class TunnelContext
{
    private readonly ConcurrentDictionary<string, TunnelModel> _tunnels = [];

    public TunnelModel? GetTunnel(string key)
    {
        return _tunnels.TryGetValue(key, out var t) ? t : null;
    }

    public bool AddTunnel(TunnelModel tunnel)
    {
        return _tunnels.TryAdd(tunnel.Key, tunnel);
    }

    public async Task RemoveTunnelAsync(string key)
    {
        if (_tunnels.TryGetValue(key, out var tunnel))
        {
            _tunnels.Remove(key, out _);
            await tunnel.CloseAsync();
        }
    }
}
