using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using WebSocketManager;

namespace ChatApplication
{
    public class TestMessageHandler : WebSocketHandler
    {
        public TestMessageHandler(WebSocketConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager)
        {
        }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);

            var socketId = WebSocketConnectionManager.GetId(socket);

            var message = new Message()
            {
                MessageType = MessageType.Text,
                Data = $"{socketId} is now connected"
            };

            await SendMessageToAllAsync(message);
        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);

            var message = new Message()
            {
                MessageType = MessageType.Text,
                Data = $"{socketId} said: {Encoding.UTF8.GetString(buffer, 0, result.Count)}"   
            };

            await SendMessageToAllAsync(message);
            await InvokeClientMethodToAllAsync("awesomeClientMethod", socketId, Encoding.UTF8.GetString(buffer, 0, result.Count));
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);
            
            await base.OnDisconnected(socket);

            var message = new Message()
            {
                MessageType = MessageType.Text,
                Data = $"{socketId} disconnected"
            };
            await SendMessageToAllAsync(message);
        }
    }
}
