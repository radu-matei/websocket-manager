using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WebSocketManager
{
    public static class WebSocketManagerExtensions
    {
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddTransient<WebSocketConnectionManager>();
            return services;

            //TODO - decide if using reflection to detect *MessageHandlers is necessary
            //so you don't have to manually register all message handlers in Startup
        }

        public static IApplicationBuilder UseWebSocketManager(this IApplicationBuilder app, 
                                                              PathString path,
                                                              WebSocketHandler handler)
        {
            //TODO - break the addition of WebSockets from mapping middleware to paths
            //so that app.UseWebSockets() isn't called multiple times
            app.UseWebSockets();

            //return app.UseMiddleware<WebSocketManagerMiddleware>(path, handler);
            return app.Map(path, (_app) => _app.UseMiddleware<WebSocketManagerMiddleware>(handler));
        }
    }
}
