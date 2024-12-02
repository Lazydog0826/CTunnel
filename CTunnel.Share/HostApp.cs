using Microsoft.Extensions.Configuration;

namespace CTunnel.Share
{
    public static class HostApp
    {
        public static IServiceProvider ServiceProvider = null!;

        public static IConfiguration Configuration = null!;

        public static T GetConfig<T>()
        {
            return Configuration.GetSection(typeof(T).Name).Get<T>()!;
        }
    }
}
