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
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            SerializationBinder = new JsonBinderWithoutAssembly()
        };

        /// <summary>
        /// Gets the method invocation strategy.
        /// </summary>
        /// <value>The method invocation strategy.</value>
        public MethodInvocationStrategy MethodInvocationStrategy { get; }

        /// <summary>
        /// The waiting remote invocations for Client to Server method calls.
        /// </summary>
        private Dictionary<Guid, TaskCompletionSource<InvocationResult>> _waitingRemoteInvocations = new Dictionary<Guid, TaskCompletionSource<InvocationResult>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="methodInvocationStrategy">The method invocation strategy used for incoming requests.</param>
        public Connection(MethodInvocationStrategy methodInvocationStrategy)
        {
            MethodInvocationStrategy = methodInvocationStrategy;
            _jsonSerializerSettings.Converters.Insert(0, new PrimitiveJsonConverter());
        }

        public async Task StartConnectionAsync(string uri)
        {
            // also check if connection was lost, that's probably why we get called multiple times.
            if (_clientWebSocket == null || _clientWebSocket.State != WebSocketState.Open)
            {
                // create a new web-socket so the next connect call works.
                _clientWebSocket?.Dispose();
                _clientWebSocket = new ClientWebSocket();
            }
            // don't do anything, we are already connected.
            else return;

            await _clientWebSocket.ConnectAsync(new Uri(uri), CancellationToken.None).ConfigureAwait(false);

            await Receive(_clientWebSocket, async (receivedMessage) =>
            {
                if (receivedMessage.MessageType == MessageType.ConnectionEvent)
                {
                    this.ConnectionId = receivedMessage.Data;
                }
                else if (receivedMessage.MessageType == MessageType.MethodInvocation)
                {
                    // retrieve the method invocation request.
                    InvocationDescriptor invocationDescriptor = null;
                    try
                    {
                        invocationDescriptor = JsonConvert.DeserializeObject<InvocationDescriptor>(receivedMessage.Data, _jsonSerializerSettings);
                        if (invocationDescriptor == null) return;
                    }
                    catch { return; } // ignore invalid data sent to the client.

                    // if the unique identifier hasn't been set then the server doesn't want a return value.
                    if (invocationDescriptor.Identifier == Guid.Empty)
                    {
                        // invoke the method only.
                        try
                        {
                            await MethodInvocationStrategy.OnInvokeMethodReceivedAsync(_clientWebSocket, invocationDescriptor);
                        }
                        catch (Exception)
                        {
                            // we consume all exceptions.
                        }
                    }
                    else
                    {
                        // invoke the method and get the result.
                        InvocationResult invokeResult;
                        try
                        {
                            // create an invocation result with the results.
                            invokeResult = new InvocationResult()
                            {
                                Identifier = invocationDescriptor.Identifier,
                                Result = await MethodInvocationStrategy.OnInvokeMethodReceivedAsync(_clientWebSocket, invocationDescriptor),
                                Exception = null
                            };
                        }
                        // send the exception as the invocation result if there was one.
                        catch (Exception ex)
                        {
                            invokeResult = new InvocationResult()
                            {
                                Identifier = invocationDescriptor.Identifier,
                                Result = null,
                                Exception = new RemoteException(ex)
                            };
                        }

                        // send a message to the server containing the result.
                        var message = new Message()
                        {
                            MessageType = MessageType.MethodReturnValue,
                            Data = JsonConvert.SerializeObject(invokeResult, _jsonSerializerSettings)
                        };
                        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, _jsonSerializerSettings));
                        await _clientWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                    }
                }
                else if (receivedMessage.MessageType == MessageType.MethodReturnValue)
                {
                    var invocationResult = JsonConvert.DeserializeObject<InvocationResult>(receivedMessage.Data, _jsonSerializerSettings);
                    // find the completion source in the waiting list.
                    if (_waitingRemoteInvocations.ContainsKey(invocationResult.Identifier))
                    {
                        // set the result of the completion source so the invoke method continues executing.
                        _waitingRemoteInvocations[invocationResult.Identifier].SetResult(invocationResult);
                        // remove the completion source from the waiting list.
                        _waitingRemoteInvocations.Remove(invocationResult.Identifier);
                    }
                }
            });
        }

        /// <summary>
        /// Send a method invoke request to the server and waits for a reply.
        /// </summary>
        /// <param name="invocationDescriptor">Example usage: set the MethodName to SendMessage and set the arguments to the connectionID with a text message</param>
        /// <returns>An awaitable task with the return value on success.</returns>
        public async Task<T> SendAsync<T>(InvocationDescriptor invocationDescriptor)
        {
            // generate a unique identifier for this invocation.
            invocationDescriptor.Identifier = Guid.NewGuid();

            // add ourselves to the waiting list for return values.
            TaskCompletionSource<InvocationResult> task = new TaskCompletionSource<InvocationResult>();
            // after a timeout of 60 seconds we will cancel the task and remove it from the waiting list.
            new CancellationTokenSource(1000 * 60).Token.Register(() => { _waitingRemoteInvocations.Remove(invocationDescriptor.Identifier); task.TrySetCanceled(); });
            _waitingRemoteInvocations.Add(invocationDescriptor.Identifier, task);

            // send the method invocation to the server.
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Message { MessageType = MessageType.MethodInvocation, Data = JsonConvert.SerializeObject(invocationDescriptor, _jsonSerializerSettings) }, _jsonSerializerSettings));
            await _clientWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

            // wait for the return value elsewhere in the program.
            InvocationResult result = await task.Task;

            // ... we just got an answer.

            // if we have completed successfully:
            if (task.Task.IsCompleted)
            {
                // there was a remote exception so we throw it here.
                if (result.Exception != null)
                    throw new Exception(result.Exception.Message);

                // return the value.

                // support null.
                if (result.Result == null) return default(T);
                // cast anything to T and hope it works.
                return (T)result.Result;
            }

            // if we reach here we got cancelled or alike so throw a timeout exception.
            throw new TimeoutException(); // todo: insert fancy message here.
        }

        /// <summary>
        /// Send a method invoke request to the server.
        /// </summary>
        /// <param name="invocationDescriptor">Example usage: set the MethodName to SendMessage and set the arguments to the connectionID with a text message</param>
        /// <returns>An awaitable task.</returns>
        public async Task SendAsync(InvocationDescriptor invocationDescriptor)
        {
            // send the method invocation to the server.
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Message { MessageType = MessageType.MethodInvocation, Data = JsonConvert.SerializeObject(invocationDescriptor) }));
            await _clientWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
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
                    var message = JsonConvert.DeserializeObject<Message>(serializedMessage, _jsonSerializerSettings);
                    handleMessage(message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
                    break;
                }
            }
        }

        public async Task SendOnlyAsync(string method) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { } });

        public async Task SendOnlyAsync<T1>(string method, T1 arg1) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1 } });

        public async Task SendOnlyAsync<T1, T2>(string method, T1 arg1, T2 arg2) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2 } });

        public async Task SendOnlyAsync<T1, T2, T3>(string method, T1 arg1, T2 arg2, T3 arg3) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6, T7>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15 } });

        public async Task SendOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16) => await SendAsync(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16 } });

        public async Task<Result> SendAsync<Result>(string method) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { } });

        public async Task<Result> SendAsync<Result, T1>(string method, T1 arg1) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1 } });

        public async Task<Result> SendAsync<Result, T1, T2>(string method, T1 arg1, T2 arg2) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3>(string method, T1 arg1, T2 arg2, T3 arg3) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6, T7>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15 } });

        public async Task<Result> SendAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16) => await SendAsync<Result>(new InvocationDescriptor { MethodName = method, Arguments = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16 } });
    }
}