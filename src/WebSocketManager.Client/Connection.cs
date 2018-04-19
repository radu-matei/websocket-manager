using System;
using System.Collections.Generic;
using System.IO;
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

        private ClientWebSocket _clientWebSocket { get; set; }
        private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        private Dictionary<string, InvocationHandler> _handlers = new Dictionary<string, InvocationHandler>();

        public Connection()
        {
            _clientWebSocket = new ClientWebSocket();
        }

        public async Task StartConnectionAsync(string uri)
        {
            await _clientWebSocket.ConnectAsync(new Uri(uri), CancellationToken.None).ConfigureAwait(false);

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

        public void On(string methodName, Action<object[]> handler)
        {
            var invocationHandler = new InvocationHandler(handler, new Type[] { });
            _handlers.Add(methodName, invocationHandler);
        }
    /// <summary>
    /// Send a message to the server
    /// </summary>
    /// <param name="invocationDescriptor">Example usage: set the MethodName to SendMessage and set the arguments to the connectionID with a text message</param>
    /// <returns></returns>
    public async Task SendAsync(InvocationDescriptor invocationDescriptor)
    {
      var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(invocationDescriptor));
      await _clientWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
    private void Invoke(InvocationDescriptor invocationDescriptor)
    {
      if (_handlers.TryGetValue(invocationDescriptor.MethodName, out InvocationHandler invocationHandler))
      {
        invocationHandler.Handler(invocationDescriptor.Arguments);
      }
    }

        public async Task StopConnectionAsync()
        {
            await _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        }

        private async Task Receive(ClientWebSocket clientWebSocket, Action<Message> handleMessage)
        {

            while (_clientWebSocket.State == WebSocketState.Open)
            {
                ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[1024 * 4]);
                string serializedMessage = null;
                WebSocketReceiveResult result = null;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await clientWebSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        serializedMessage = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }

                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = JsonConvert.DeserializeObject<Message>(serializedMessage);
                    handleMessage(message);
                }

                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
                    break;
                }
            }
        }
    }

    public class InvocationHandler
    {
        public Action<object[]> Handler { get; set; }
        public Type[] ParameterTypes { get; set; }

        public InvocationHandler(Action<object[]> handler, Type[] parameterTypes)
        {
            Handler = handler;
            ParameterTypes = parameterTypes;
        }
    }
}
