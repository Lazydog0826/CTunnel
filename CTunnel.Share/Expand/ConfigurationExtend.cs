using Microsoft.Extensions.Configuration;

namespace CTunnel.Share.Expand
{
    public static class ConfigurationExtend
    {
        public static T GetConfig<T>(this IConfiguration configuration)
        {
            return configuration.GetSection(typeof(T).Name).Get<T>()!;
        }
    }
}
