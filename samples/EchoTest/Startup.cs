using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using WebSocketManager;

namespace EchoApp
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseWebSocketManager("/ws", new ChatMessageHandler(new WebSocketManager.WebSocketManager()));
            app.UseWebSocketManager("/test", new TestMessageHandler(new WebSocketManager.WebSocketManager()));

            app.UseStaticFiles();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketManager();
        }
    }
}
