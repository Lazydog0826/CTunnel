using System.Net;
using System.Net.Sockets;
using CTunnel.Share.Expand;
using CTunnel.Share.Model;
using Microsoft.Extensions.DependencyInjection;
using MiniComp.Core.App;
using Newtonsoft.Json;

namespace CTunnel.Client;

public static class ForwardSocket
{
    public static async Task CreateForwardSocketAsync(RegisterRequest request)
    {
        var appConfig = HostApp.RootServiceProvider.GetRequiredService<AppConfig>();
        request.Token = appConfig.Token;
        var requestItem = new RequestItem
        {
            Id = request.RequestId,
            TargetSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                appConfig.Type.ToProtocolType()
            ),
            ForwardSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                appConfig.Type.ToProtocolType()
            ),
        };

        requestItem.TokenSource.Token.Register(() =>
        {
            requestItem.ForwardSocket.TryCloseAsync().Wait();
            requestItem.TargetSocket.TryCloseAsync().Wait();
        });

        await requestItem.ForwardSocket.ConnectAsync(
            new DnsEndPoint(appConfig.Server.Host, appConfig.Server.Port + 1)
        );
        requestItem.ForwardSocket.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.KeepAlive,
            true
        );
        requestItem.ForwardSocketStream = await requestItem.ForwardSocket.GetStreamAsync(
            true,
            false,
            appConfig.Server.Host
        );
        await requestItem.ForwardSocketStream.WriteAsync(
            JsonConvert.SerializeObject(request).ToBytes()
        );

        try
        {
            await requestItem.TargetSocket.ConnectAsync(
                new DnsEndPoint(appConfig.Target.Host, appConfig.Target.Port)
            );
            requestItem.TargetSocket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.KeepAlive,
                true
            );
            requestItem.TargetSocketStream = await requestItem.TargetSocket.GetStreamAsync(
                appConfig.Target.IsNeedTls(),
                false,
                appConfig.Target.Host
            );

            TaskExtend.NewTask(
                async () =>
                {
                    await SocketExtend.ForwardAsync(
                        requestItem.TargetSocketStream,
                        requestItem.ForwardSocketStream,
                        requestItem.TokenSource.Token
                    );
                },
                null,
                async () =>
                {
                    await requestItem.TokenSource.CancelAsync();
                }
            );
            TaskExtend.NewTask(
                async () =>
                {
                    await SocketExtend.ForwardAsync(
                        requestItem.ForwardSocketStream,
                        requestItem.TargetSocketStream,
                        requestItem.TokenSource.Token
                    );
                },
                null,
                async () =>
                {
                    await requestItem.TokenSource.CancelAsync();
                }
            );
            await Task.Delay(Timeout.InfiniteTimeSpan, requestItem.TokenSource.Token);
        }
        finally
        {
            await requestItem.TokenSource.CancelAsync();
        }
    }
}
