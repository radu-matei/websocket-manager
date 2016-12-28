using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebSocketManager
{
    public class WebSocketManagerMiddleware
    {
        private readonly RequestDelegate _next;
        private PathString _path;
        private WebSocketManager _webSocketManager { get; set; }
        private WebSocketMessageHandler _webSocketMessageHandler { get; set; }

        public WebSocketManagerMiddleware(RequestDelegate next, 
                                          PathString path,
                                          WebSocketManager webSocketManager, 
                                          WebSocketMessageHandler webSocketMessageHandler)
        {
            _next = next;
            _path = path;
            _webSocketManager = webSocketManager;
            _webSocketMessageHandler = webSocketMessageHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if(!context.WebSockets.IsWebSocketRequest || context.Request.Path != _path)
                return;
            
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            _webSocketManager.AddSocket(socket);

            var socketId = _webSocketManager.GetId(socket);
            await _webSocketMessageHandler.SendMessageToAllAsync(message: $"{_webSocketManager.GetId(socket)} connected");
            
            
            await _webSocketMessageHandler.ReceiveMessageAsync(socket, async (result, buffer) => {

                if(result.MessageType == WebSocketMessageType.Text)
                {
                    var message = $"{socketId} said: {Encoding.UTF8.GetString(buffer, 0, result.Count)}";
                    await _webSocketMessageHandler.SendMessageToAllAsync(message: message);
                }

                if(result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, String.Empty, CancellationToken.None);
                }
            });
            
            //await _next.Invoke(context);
        }
    }
}
