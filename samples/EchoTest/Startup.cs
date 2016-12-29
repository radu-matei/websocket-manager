using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using WebSocketManager;

namespace EchoApp
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            app.UseWebSocketManager("/ws", serviceProvider.GetService<ChatMessageHandler>());
            app.UseWebSocketManager("/test", serviceProvider.GetService<TestMessageHandler>());

            app.UseStaticFiles();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketManager();

            services.AddSingleton<ChatMessageHandler>();
            services.AddSingleton<TestMessageHandler>();
        }
    }
}
