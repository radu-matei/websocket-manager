using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using WebSocketManager.Common;

namespace WebSocketManager.Client
{
    public class Connection
    {
        public string ConnectionId { get; set; }

        public delegate void ServerInvokeHandler(params object[] arguments);

        private ClientWebSocket _clientWebSocket { get; set; }
        private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        private Dictionary<string, ServerInvokeHandler> _handlers = new Dictionary<string, ServerInvokeHandler>();

        public Connection()
        {
            _clientWebSocket = new ClientWebSocket();
        }

        public async Task StartConnectionAsync(string uri)
        {
            await _clientWebSocket.ConnectAsync(new Uri(uri), CancellationToken.None);

            await Receive(_clientWebSocket, (message) =>
            {
                if (message.MessageType == MessageType.ConnectionEvent)
                {
                    this.ConnectionId = message.Data;
                }

                else if (message.MessageType == MessageType.ClientMethodInvocation)
                {
                    var invocationDescriptor = JsonConvert.DeserializeObject<InvocationDescriptor>(message.Data, _jsonSerializerSettings);
                    Invoke(invocationDescriptor);
                }
            });

        }

        public void On(string methodName, ServerInvokeHandler handler)
        {
            _handlers.Add(methodName, handler);
        }

        private void Invoke(InvocationDescriptor invocationDescriptor)
        {
            if(_handlers[invocationDescriptor.MethodName] != null)
                _handlers[invocationDescriptor.MethodName].Invoke(invocationDescriptor.Arguments);
        }

        public async Task StopConnectionAsync()
        {
            await _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

        private async Task Receive(ClientWebSocket clientWebSocket, Action<Message> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while (_clientWebSocket.State == WebSocketState.Open)
            {
                var result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var serializedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = JsonConvert.DeserializeObject<Message>(serializedMessage);
                    handleMessage(message);
                }

                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
    }
}