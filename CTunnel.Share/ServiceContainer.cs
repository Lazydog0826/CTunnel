using Microsoft.Extensions.DependencyInjection;

namespace CTunnel.Share
{
    /// <summary>
    /// 静态依赖注入容器
    /// </summary>
    public static class ServiceContainer
    {
        private static readonly IServiceCollection Services = new ServiceCollection();
        private static IServiceProvider? ServiceProvider = null;

        public static async Task RegisterServiceAsync(Func<IServiceCollection, Task> func)
        {
            await func.Invoke(Services);
            ServiceProvider = Services.BuildServiceProvider();
        }

        public static T GetService<T>()
            where T : class
        {
            if (ServiceProvider == null)
                throw new Exception("服务容器未初始化");
            return ServiceProvider.GetRequiredService<T>();
        }

        public static T GetService<T>(string key)
            where T : class
        {
            if (ServiceProvider == null)
                throw new Exception("服务容器未初始化");
            return ServiceProvider.GetRequiredKeyedService<T>(key);
        }
    }
}
