using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using WebSocketManager;
using WebSocketManager.Common;

namespace ChatApplication
{
    public class ChatHandler : WebSocketHandler
    {
        public ChatHandler(WebSocketConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager, new ControllerMethodInvocationStrategy())
        {
            ((ControllerMethodInvocationStrategy)MethodInvocationStrategy).Controller = this;
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

        // this method can be called from a client, doesn't return anything.
        public async Task SendMessage(WebSocket socket, string message)
        {
            // chat command.
            if (message == "/math")
            {
                await AskClientToDoMath(socket);
            }
            else
            {
                await InvokeClientMethodToAllAsync("receiveMessage", WebSocketConnectionManager.GetId(socket), message);
            }
        }

        // this method can be called from a client, returns the integer result or throws an exception.
        public int DoMath(WebSocket socket, int a, int b)
        {
            if (a == 0 || b == 0) throw new Exception("That makes no sense.");
            return a + b;
        }

        // we ask a client to do some math for us then broadcast the results.
        private async Task AskClientToDoMath(WebSocket socket)
        {
            string id = WebSocketConnectionManager.GetId(socket);
            try
            {
                int result = await InvokeClientMethodAsync<int, int, int>(id, "DoMath", 3, 5);
                await InvokeClientMethodOnlyAsync(id, "receiveMessage", "Server", $"You sent me this result: " + result);
            }
            catch (Exception ex)
            {
                await InvokeClientMethodOnlyAsync(id, "receiveMessage", "Server", $"I had an exception: " + ex.Message);
            }
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