using CTunnel.Core;
using CTunnel.Core.Enums;
using CTunnel.Core.TunnelHandle;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ClientManage>();
builder.Services.AddKeyedSingleton<ITunnelHandle, HttpTunnelHandle>(nameof(TunnelTypeEnum.Http));
builder.Services.AddHostedService<TcpListenHostService>();
builder.Services.AddControllers();
var app = builder.Build();
HostApp.ServiceProvider = app.Services;
app.UseStaticFiles();
app.MapControllers();
app.Run();
