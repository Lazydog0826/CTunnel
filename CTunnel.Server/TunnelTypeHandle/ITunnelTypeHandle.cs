using CTunnel.Share.Model;

namespace CTunnel.Server.TunnelTypeHandle;

/// <summary>
/// 注册隧道处理接口
/// </summary>
public interface ITunnelTypeHandle
{
    public Task HandleAsync(TunnelModel tunnel);
}
