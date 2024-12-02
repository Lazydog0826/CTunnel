using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using CTunnel.Share;

namespace CTunnel.Server
{
    public class WebSocketListenHostService(WebRequestHandle _webRequestHandle) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 统一监听443端口作为WEB服务
            var appConfig = HostApp.GetConfig<AppConfig>();
            var x509Certificate2 = new X509Certificate2(
                appConfig.Certificate,
                appConfig.CertificatePassword
            );
            var socker = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
            socker.Bind(new IPEndPoint(IPAddress.Any, 443));
            socker.Listen();
            Log.Write("正在监听443端口...");
            while (true)
            {
                var request = await socker.AcceptAsync(stoppingToken);
                if (request.ProtocolType != ProtocolType.Tcp)
                {
                    request.Close();
                    continue;
                }
                _ = Task.Run(
                    async () =>
                    {
                        try
                        {
                            await _webRequestHandle.HandleAsync(request, x509Certificate2);
                        }
                        catch { }
                        finally
                        {
                            request.Close();
                        }
                    },
                    stoppingToken
                );
            }
        }
    }
}
