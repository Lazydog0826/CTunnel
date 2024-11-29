using System.Net.Sockets;
using CTunnel.Core.Expand;
using CTunnel.Core.Model;

namespace CTunnel.Core.TunnelHandle
{
    public class HttpTunnelHandle : ITunnelHandle
    {
        public async Task HandleAsync(TunnelModel tunnel, TcpClient requestClient)
        {
            TcpClient? tunnelClient = null;
            while (tunnel.ConnectionPool.TryTake(out tunnelClient))
            {
                if (tunnelClient.Connected)
                    break;
            }

            if (tunnelClient != null)
            {
                var token = new CancellationTokenSource();

                var requestClientStream = requestClient.GetStream();
                var tunnelClientStream = tunnelClient.GetStream();

                var t1 = Task.Run(async () =>
                {
                    try
                    {
                        await requestClientStream.ForwardAsync(tunnelClientStream);
                    }
                    catch { }
                    finally
                    {
                        await token.CancelAsync();
                        requestClientStream.Close();
                    }
                });

                var t2 = Task.Run(async () =>
                {
                    try
                    {
                        await tunnelClientStream.ForwardAsync(requestClientStream);
                    }
                    catch { }
                    finally
                    {
                        await token.CancelAsync();
                        tunnelClientStream.Close();
                    }
                });

                await Task.WhenAll(t1, t2);
            }
            else
            {
                throw new Exception("连接池没有可用的连接");
            }
        }

        public async Task<bool> IsCloseAsync(TunnelModel tunnel)
        {
            await Task.CompletedTask;
            var isOk = false;
            foreach (var item in tunnel.ConnectionPool.ToList())
            {
                isOk = isOk || await item.HeartbeatAsync();
                if (isOk)
                    break;
            }
            return !isOk;
        }
    }
}
