using CTunnel.Core;
using CTunnel.Core.Enums;
using CTunnel.Core.TunnelHandle;
using Microsoft.AspNetCore.WebSockets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebSockets(opt => { });
builder.Services.AddSingleton<ClientManage>();
builder.Services.AddKeyedSingleton<ITunnelHandle, HttpTunnelHandle>(nameof(TunnelTypeEnum.Http));
builder.Services.AddControllers();
var app = builder.Build();
HostApp.ServiceProvider = app.Services;
app.UseWebSockets();
app.UseMiddleware<WebSocketsListenMiddleware>();
app.UseStaticFiles();
app.MapControllers();
app.Run();
