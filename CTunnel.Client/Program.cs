using System.Diagnostics;
using CTunnel.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TunnelContext>();
builder.Services.AddControllers();
var app = builder.Build();
app.UseStaticFiles();
app.MapControllers();
Process.Start(
    new ProcessStartInfo { FileName = "http://localhost:5200/index.html", UseShellExecute = true }
);
await app.RunAsync();
