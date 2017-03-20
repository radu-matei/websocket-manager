using System;
using System.IO;
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
        private WebSocketHandler _webSocketHandler { get; set; }

        public WebSocketManagerMiddleware(RequestDelegate next,
                                          WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;

            var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            await _webSocketHandler.OnConnected(socket).ConfigureAwait(false);

            await Receive(socket, async (result, serializedInvocationDescriptor) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    await _webSocketHandler.ReceiveAsync(socket, result, serializedInvocationDescriptor).ConfigureAwait(false);
                    return;
                }

                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    try
                    {
                        await _webSocketHandler.OnDisconnected(socket);
                    }

                    catch (WebSocketException)
                    {
                        throw; //let's not swallow any exception for now
                    }

                    return;
                }

            });

            //TODO - investigate the Kestrel exception thrown when this is the last middleware
            //await _next.Invoke(context);
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, string> handleMessage)
        {
            while (socket.State == WebSocketState.Open)
            {
                ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[1024 * 4]);
                string serializedInvocationDescriptor = null;
                WebSocketReceiveResult result = null;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        serializedInvocationDescriptor = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }

                handleMessage(result, serializedInvocationDescriptor);
            }
        }
    }
}
