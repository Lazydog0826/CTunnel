using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using CTunnel.Share;
using CTunnel.Share.Expand;

namespace CTunnel.Server
{
    public class WebSocketListenHostService(WebRequestHandle _webRequestHandle) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 统一监听443端口作为WEB服务
            var appConfig = HostApp.GetConfig<AppConfig>();

            var certPem = File.ReadAllText(appConfig.Certificate);
            var keyPem = File.ReadAllText(appConfig.CertificateKey);
            var x509Certificate2 = X509Certificate2.CreateFromPem(certPem, keyPem);
            x509Certificate2 = new X509Certificate2(
                x509Certificate2.Export(X509ContentType.Pfx, "123456"),
                "123456"
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
                    await request.TryCloseAsync();
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
                            await request.TryCloseAsync();
                        }
                    },
                    stoppingToken
                );
            }
        }
    }
}
