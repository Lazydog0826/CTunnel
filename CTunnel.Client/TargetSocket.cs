using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using CTunnel.Share;
using CTunnel.Share.Enums;
using CTunnel.Share.Expand;

namespace CTunnel.Client;

public class TargetSocket(AppConfig appConfig)
{
    private Socket _targetSocket = null!;
    private Stream _stream = null!;
    private string _requestId = string.Empty;

    public async Task ConnectAsync(string requestId)
    {
        _requestId = requestId;
        _targetSocket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            appConfig.Type.ToProtocolType()
        );
        await _targetSocket.ConnectAsync(
            new DnsEndPoint(appConfig.Target.Host, appConfig.Target.Port)
        );
        _targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        _stream = await _targetSocket.GetStreamAsync(
            appConfig.Target.IsNeedTls(),
            false,
            appConfig.Target.Host
        );
    }

    public async Task WriteAsync(Memory<byte> buffer)
    {
        await _stream.WriteAsync(buffer);
    }

    public async Task CloseAsync()
    {
        await _targetSocket.TryCloseAsync();
    }

    public async Task ReadAsync(ClientWebSocket clientWebSocket, SemaphoreSlim forward)
    {
        try
        {
            int readCount;
            var isHaveSlim = false;
            using var memory = MemoryPool<byte>.Shared.Rent(GlobalStaticConfig.BufferSize);
            while (
                (readCount = await _stream.ReadAsync(memory.Memory, CancellationToken.None)) != 0
            )
            {
                if (isHaveSlim == false)
                {
                    await forward.WaitAsync();
                    isHaveSlim = true;
                    await clientWebSocket.SendAsync(
                        _requestId.ToBytes(),
                        WebSocketMessageType.Binary,
                        false,
                        CancellationToken.None
                    );
                }

                var isEnd = _targetSocket.Available == 0 && appConfig.Type == TunnelTypeEnum.Web;
                await clientWebSocket.SendAsync(
                    memory.Memory[..readCount],
                    WebSocketMessageType.Binary,
                    isEnd,
                    CancellationToken.None
                );
                if (isEnd)
                {
                    break;
                }
            }
        }
        finally
        {
            forward.Release();
            await CloseAsync();
        }
    }
}
