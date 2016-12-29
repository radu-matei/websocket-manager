using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using WebSocketManager;

namespace ChatApplication
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            app.UseWebSockets();

            app.MapWebSocketManager("/ws", serviceProvider.GetService<ChatMessageHandler>());
            app.MapWebSocketManager("/test", serviceProvider.GetService<TestMessageHandler>());

            app.UseStaticFiles();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketManager();
        }
    }
}
