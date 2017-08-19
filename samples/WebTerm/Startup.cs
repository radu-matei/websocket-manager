using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using WebSocketManager;

namespace WebTerm {
	public class Startup {
		public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider) {
			app.UseWebSockets();
			app.MapWebSocketManager("/cmd", serviceProvider.GetService<WebTermHandler>());

			app.UseStaticFiles();
		}

		public void ConfigureServices(IServiceCollection services) {
			services.AddWebSocketManager();
		}
	}
}
