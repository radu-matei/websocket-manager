using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using WebSocketManager;

namespace Mvc
{
    public class NotificationsMessageHandler : WebSocketHandler
    {
        public NotificationsMessageHandler(ConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager)
        {
        }

        public override Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            throw new NotImplementedException();
        }
    }
}