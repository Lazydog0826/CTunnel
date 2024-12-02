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

#region 注入IWebSocketMessageHandle实现

builder.Services.AddKeyedSingleton<IWebSocketMessageHandle, WebSocketMessageHandle_RegisterTunnel>(
    nameof(WebSocketMessageTypeEnum.RegisterTunnel)
);
builder.Services.AddKeyedSingleton<IWebSocketMessageHandle, WebSocketMessageHandle_PulseCheck>(
    nameof(WebSocketMessageTypeEnum.PulseCheck)
);
builder.Services.AddKeyedSingleton<IWebSocketMessageHandle, WebSocketMessageHandle_NewRequest>(
    nameof(WebSocketMessageTypeEnum.NewRequest)
);

#endregion 注入IWebSocketMessageHandle实现

#region 注入ITunnelHandle实现

builder.Services.AddKeyedSingleton<ITunnelHandle, TunnelHandle_Web>(TunnelTypeEnum.Web.ToString());

#endregion 注入ITunnelHandle实现

builder.Services.AddControllers();
var app = builder.Build();
HostApp.ServiceProvider = app.Services;
HostApp.Configuration = app.Configuration;
app.UseWebSockets();
app.UseMiddleware<ListenWebSocketMiddleware>();
app.MapControllers();
app.Run();
