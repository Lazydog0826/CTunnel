using CTunnel.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TunnelContext>();
builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
