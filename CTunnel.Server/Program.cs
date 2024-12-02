using CTunnel.Server;
using CTunnel.Server.TunnelHandle;
using CTunnel.Server.WebSocketMessageHandle;
using CTunnel.Share;
using CTunnel.Share.Enums;
using Microsoft.AspNetCore.WebSockets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebSockets(opt => { });
builder.Services.AddSingleton<TunnelContext>();
builder.Services.AddSingleton<WebRequestHandle>();
builder.Services.AddHostedService<WebSocketListenHostService>();

#region ע��IWebSocketMessageHandleʵ��

builder.Services.AddKeyedSingleton<IWebSocketMessageHandle, WebSocketMessageHandle_RegisterTunnel>(
    nameof(WebSocketMessageTypeEnum.RegisterTunnel)
);
builder.Services.AddKeyedSingleton<IWebSocketMessageHandle, WebSocketMessageHandle_PulseCheck>(
    nameof(WebSocketMessageTypeEnum.PulseCheck)
);
builder.Services.AddKeyedSingleton<IWebSocketMessageHandle, WebSocketMessageHandle_NewRequest>(
    nameof(WebSocketMessageTypeEnum.NewRequest)
);

#endregion ע��IWebSocketMessageHandleʵ��

#region ע��ITunnelHandleʵ��

builder.Services.AddKeyedSingleton<ITunnelHandle, TunnelHandle_Web>(TunnelTypeEnum.Web.ToString());

#endregion ע��ITunnelHandleʵ��

builder.Services.AddControllers();
var app = builder.Build();
HostApp.ServiceProvider = app.Services;
HostApp.Configuration = app.Configuration;
app.UseWebSockets();
app.UseMiddleware<ListenWebSocketMiddleware>();
app.MapControllers();
app.Run();
