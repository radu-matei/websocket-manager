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

        /// <summary>
        /// Called when an invoke method call has been received. The default implementation calls
        /// actions registered with the <see cref="On(string, Action{object[]})"/> method.
        /// </summary>
        /// <param name="invocationDescriptor">
        /// The invocation descriptor containing the method name and parameters.
        /// </param>
        protected virtual void Invoke(InvocationDescriptor invocationDescriptor)
        {
            var invocationHandler = _handlers[invocationDescriptor.MethodName];
            if (invocationHandler != null)
                invocationHandler.Handler(invocationDescriptor.Arguments);
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

        public async Task SendAsync(string method) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { } });

        public async Task SendAsync<T1>(string method, T1 arg1) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1 } });

        public async Task SendAsync<T1, T2>(string method, T1 arg1, T2 arg2) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2 } });

        public async Task SendAsync<T1, T2, T3>(string method, T1 arg1, T2 arg2, T3 arg3) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3 } });

        public async Task SendAsync<T1, T2, T3, T4>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4 } });

        public async Task SendAsync<T1, T2, T3, T4, T5>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6, T7>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6, T7, T8>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15 } });

        public async Task SendAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16 } });
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