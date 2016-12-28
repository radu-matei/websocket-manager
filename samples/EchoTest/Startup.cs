using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using WebSocketManager;

namespace EchoApp
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, WebSocketManager.WebSocketManager webSocketManager)
        {
            app.UseStaticFiles();
            app.UseWebSocketManager("/ws", new ChatMessageHandler(webSocketManager));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketManager();
        }
    }
}
