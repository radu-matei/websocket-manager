using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebSocketManager
{
    public class WebSocketManagerMiddleware
    {
        private readonly RequestDelegate _next;
        private WebSocketHandler _webSocketHandler { get; set; }

        public WebSocketManagerMiddleware(RequestDelegate next, 
                                          WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                await _webSocketHandler.OnConnected(socket);

                try
                {
                    await Receive(socket, async(result, buffer) =>
                    {
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            await _webSocketHandler.ReceiveAsync(socket, result, buffer);
                        }
                        else if(result.MessageType == WebSocketMessageType.Close)
                        {
                            if (result.CloseStatus.HasValue && result.CloseStatus == WebSocketCloseStatus.EndpointUnavailable)
                            {
                                await _webSocketHandler.OnDisconnected(socket);
                            }
                            else
                            {
                                await _webSocketHandler.OnCloseConnection(socket);
                            }
                        }
                    });
                }
                catch (WebSocketException e)
                {
                    if (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        await _webSocketHandler.OnDisconnected(socket);
                        return;
                    }

                    throw;
                }
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while(socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                                                       cancellationToken: CancellationToken.None);

                handleMessage(result, buffer);                
            }
        }
    }
}