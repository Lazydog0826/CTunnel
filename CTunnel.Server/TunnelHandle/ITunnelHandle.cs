using CTunnel.Share.Model;

namespace CTunnel.Server.TunnelHandle
{
    public interface ITunnelHandle
    {
        public Task HandleAsync(TunnelModel tunnel);
    }
}
