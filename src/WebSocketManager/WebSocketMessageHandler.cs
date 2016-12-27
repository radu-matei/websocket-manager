using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketManager
{
    public class WebSocketMessageHandler
    {
        private const int bufferSize = 1024 * 4;
        private WebSocketManager _webSocketManager { get; set; }

        public WebSocketMessageHandler(WebSocketManager webSocketManager)
        {
            _webSocketManager = webSocketManager;
        }

        public async Task SendMessageToAllAsync(string message)
        {
            foreach(var pair in _webSocketManager.GetAll())
            {
                await SendMessageAsync(pair.Value, message);
            }
        }

        public async Task<bool> SendMessageAsync(string socketId, string message)
        {
            var socket = _webSocketManager.GetSocketById(socketId);

            return await SendMessageAsync(socket, message);            
        }

        private async Task<bool> SendMessageAsync(WebSocket socket, string message)
        {
            if(socket.State != WebSocketState.Open)
                return false;
            
            await socket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.ASCII.GetBytes(message),
                                                                  offset: 0, 
                                                                  count: message.Length),
                                   messageType: WebSocketMessageType.Text,
                                   endOfMessage: true,
                                   cancellationToken: CancellationToken.None);
            return true;
        }

        public async Task ReceiveMessageAsync(WebSocket socket, Action<WebSocketReceiveResult, byte[]> messageHandler)
        {
            var buffer = new byte[bufferSize];

            while(socket.State == WebSocketState.Open)
            {
                var messageResult = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), 
                                                              cancellationToken: CancellationToken.None);

                messageHandler(messageResult, buffer);
            }
        }
    }
}
