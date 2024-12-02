using CTunnel.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TunnelContext>();
builder.Services.AddControllers();
var app = builder.Build();
app.UseStaticFiles();
app.MapControllers();
app.Run();
