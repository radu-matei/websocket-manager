using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WebSocketManager
{
    public static class WebSocketManagerExtensions
    {
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddTransient<WebSocketManager>();
            return services;
        }

        public static IApplicationBuilder UseWebSocketManager(this IApplicationBuilder app, 
                                                              PathString path,
                                                              WebSocketHandler handler)
        {
            app.UseWebSockets();

            //return app.UseMiddleware<WebSocketManagerMiddleware>(path, handler);
            return app.Map(path, (_app) => _app.UseMiddleware<WebSocketManagerMiddleware>(path, handler));
        }
    }
}
