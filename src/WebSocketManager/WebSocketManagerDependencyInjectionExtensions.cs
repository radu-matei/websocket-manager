using Microsoft.Extensions.DependencyInjection;

namespace WebSocketManager
{
    public static class WebSocketManagerDependencyInjectionExtensions
    {
        public static IServiceCollection AddWebSocketManager(IServiceCollection services)
        {
            services.AddSingleton<WebSocketManager>();
            services.AddTransient<WebSocketMessageHandler>();

            return services;
        }
    }
}
