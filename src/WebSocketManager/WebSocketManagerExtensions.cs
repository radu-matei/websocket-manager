using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WebSocketManager
{
    public static class WebSocketManagerExtensions
    {
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddSingleton<WebSocketManager>();
            services.AddTransient<WebSocketMessageHandler>();

            return services;
        }

        public static IApplicationBuilder UseWebSocketManager(this IApplicationBuilder app, PathString path)
        {
            app.UseWebSockets();
            return app.UseMiddleware<WebSocketManagerMiddleware>(path);
        }
    }
}
