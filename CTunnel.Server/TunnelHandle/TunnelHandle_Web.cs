using CTunnel.Share.Model;

namespace CTunnel.Server.TunnelHandle
{
    /// <summary>
    /// WEB服务是走固定的443监听，所以这里是空方法
    /// </summary>
    public class TunnelHandle_Web : ITunnelHandle
    {
        public async Task HandleAsync(TunnelModel tunnel)
        {
            await Task.CompletedTask;
        }
    }
}
