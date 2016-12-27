using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebSocketManager
{
    public class WebSocketManagerMiddleware
    {
        private readonly RequestDelegate _next;
        private WebSocketManager _webSocketManager { get; set; }
        private WebSocketMessageHandler _webSocketMessageHandler { get; set; }

        public WebSocketManagerMiddleware(RequestDelegate next, 
                                          WebSocketManager webSocketManager, 
                                          WebSocketMessageHandler webSocketMessageHandler)
        {
            _next = next;
            _webSocketManager = webSocketManager;
            _webSocketMessageHandler = webSocketMessageHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if(!context.WebSockets.IsWebSocketRequest)
                return;

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            _webSocketManager.AddSocket(socket);

            await _webSocketMessageHandler.SendMessageAsync(socketId: "", 
                                                            message: _webSocketManager.GetId(socket));

            await _next.Invoke(context);
        }
    }
}
