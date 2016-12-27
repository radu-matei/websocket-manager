using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using WebSocketManager;

public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        app.UseStaticFiles();
        app.UseWebSocketManager("/ws");
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddWebSocketManager();
    }
}