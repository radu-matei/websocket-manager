using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebSocketManager.Common;
using System.Collections.Generic;

namespace WebSocketManager
{
    public abstract class WebSocketHandler
    {
        protected WebSocketConnectionManager WebSocketConnectionManager { get; set; }

        private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            SerializationBinder = new JsonBinderWithoutAssembly()
        };

        /// <summary>
        /// The waiting remote invocations for Server to Client method calls.
        /// </summary>
        private Dictionary<Guid, TaskCompletionSource<InvocationResult>> _waitingRemoteInvocations = new Dictionary<Guid, TaskCompletionSource<InvocationResult>>();

        /// <summary>
        /// Gets the method invocation strategy.
        /// </summary>
        /// <value>The method invocation strategy.</value>
        public MethodInvocationStrategy MethodInvocationStrategy { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketHandler"/> class.
        /// </summary>
        /// <param name="webSocketConnectionManager">The web socket connection manager.</param>
        /// <param name="methodInvocationStrategy">The method invocation strategy used for incoming requests.</param>
        public WebSocketHandler(WebSocketConnectionManager webSocketConnectionManager, MethodInvocationStrategy methodInvocationStrategy)
        {
            _jsonSerializerSettings.Converters.Insert(0, new PrimitiveJsonConverter());
            WebSocketConnectionManager = webSocketConnectionManager;
            MethodInvocationStrategy = methodInvocationStrategy;
        }

        /// <summary>
        /// Called when a client has connected to the server.
        /// </summary>
        /// <param name="socket">The web-socket of the client.</param>
        /// <returns>Awaitable Task.</returns>
        public virtual async Task OnConnected(WebSocket socket)
        {
            WebSocketConnectionManager.AddSocket(socket);

            await SendMessageAsync(socket, new Message()
            {
                MessageType = MessageType.ConnectionEvent,
                Data = WebSocketConnectionManager.GetId(socket)
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Called when a client has disconnected from the server.
        /// </summary>
        /// <param name="socket">The web-socket of the client.</param>
        /// <returns>Awaitable Task.</returns>
        public virtual async Task OnDisconnected(WebSocket socket)
        {
            await WebSocketConnectionManager.RemoveSocket(WebSocketConnectionManager.GetId(socket)).ConfigureAwait(false);
        }

        public async Task SendMessageAsync(WebSocket socket, Message message)
        {
            if (socket.State != WebSocketState.Open)
                return;

            var serializedMessage = JsonConvert.SerializeObject(message, _jsonSerializerSettings);
            var encodedMessage = Encoding.UTF8.GetBytes(serializedMessage);
            await socket.SendAsync(buffer: new ArraySegment<byte>(array: encodedMessage,
                                                                  offset: 0,
                                                                  count: encodedMessage.Length),
                                   messageType: WebSocketMessageType.Text,
                                   endOfMessage: true,
                                   cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }

        public async Task SendMessageAsync(string socketId, Message message)
        {
            await SendMessageAsync(WebSocketConnectionManager.GetSocketById(socketId), message).ConfigureAwait(false);
        }

        public async Task SendMessageToAllAsync(Message message)
        {
            foreach (var pair in WebSocketConnectionManager.GetAll())
            {
                try
                {
                    if (pair.Value.State == WebSocketState.Open)
                        await SendMessageAsync(pair.Value, message).ConfigureAwait(false);
                }
                catch (WebSocketException e)
                {
                    if (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        await OnDisconnected(pair.Value);
                    }
                }
            }
        }

        public async Task InvokeClientMethodAsync(string socketId, string methodName, object[] arguments)
        {
            var message = new Message()
            {
                MessageType = MessageType.MethodInvocation,
                Data = JsonConvert.SerializeObject(new InvocationDescriptor()
                {
                    MethodName = methodName,
                    Arguments = arguments
                }, _jsonSerializerSettings)
            };

            await SendMessageAsync(socketId, message).ConfigureAwait(false);
        }

        public async Task<T> InvokeClientMethodAsync<T>(string socketId, string methodName, object[] arguments)
        {
            // create the method invocation descriptor.
            InvocationDescriptor invocationDescriptor = new InvocationDescriptor { MethodName = methodName, Arguments = arguments };

            // generate a unique identifier for this invocation.
            invocationDescriptor.Identifier = Guid.NewGuid();

            // add ourselves to the waiting list for return values.
            TaskCompletionSource<InvocationResult> task = new TaskCompletionSource<InvocationResult>();
            // after a timeout of 60 seconds we will cancel the task and remove it from the waiting list.
            new CancellationTokenSource(1000 * 60).Token.Register(() => { _waitingRemoteInvocations.Remove(invocationDescriptor.Identifier); task.TrySetCanceled(); });
            _waitingRemoteInvocations.Add(invocationDescriptor.Identifier, task);

            // send the method invocation to the client.
            var message = new Message() { MessageType = MessageType.MethodInvocation, Data = JsonConvert.SerializeObject(invocationDescriptor, _jsonSerializerSettings) };
            await SendMessageAsync(socketId, message).ConfigureAwait(false);

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

        public async Task InvokeClientMethodToAllAsync(string methodName, params object[] arguments)
        {
            foreach (var pair in WebSocketConnectionManager.GetAll())
            {
                try
                {
                    if (pair.Value.State == WebSocketState.Open)
                        await InvokeClientMethodAsync(pair.Key, methodName, arguments).ConfigureAwait(false);
                }
                catch (WebSocketException e)
                {
                    if (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        await OnDisconnected(pair.Value);
                    }
                }
            }
        }

        public async Task SendMessageToGroupAsync(string groupID, Message message)
        {
            var sockets = WebSocketConnectionManager.GetAllFromGroup(groupID);
            if (sockets != null)
            {
                foreach (var socket in sockets)
                {
                    await SendMessageAsync(socket, message);
                }
            }
        }

        public async Task SendMessageToGroupAsync(string groupID, Message message, string except)
        {
            var sockets = WebSocketConnectionManager.GetAllFromGroup(groupID);
            if (sockets != null)
            {
                foreach (var id in sockets)
                {
                    if (id != except)
                        await SendMessageAsync(id, message);
                }
            }
        }

        public async Task InvokeClientMethodToGroupAsync(string groupID, string methodName, params object[] arguments)
        {
            var sockets = WebSocketConnectionManager.GetAllFromGroup(groupID);
            if (sockets != null)
            {
                foreach (var id in sockets)
                {
                    await InvokeClientMethodAsync(id, methodName, arguments);
                }
            }
        }

        public async Task InvokeClientMethodToGroupAsync(string groupID, string methodName, string except, params object[] arguments)
        {
            var sockets = WebSocketConnectionManager.GetAllFromGroup(groupID);
            if (sockets != null)
            {
                foreach (var id in sockets)
                {
                    if (id != except)
                        await InvokeClientMethodAsync(id, methodName, arguments);
                }
            }
        }

        public async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, Message receivedMessage)
        {
            // method invocation request.
            if (receivedMessage.MessageType == MessageType.MethodInvocation)
            {
                // retrieve the method invocation request.
                InvocationDescriptor invocationDescriptor = null;
                try
                {
                    invocationDescriptor = JsonConvert.DeserializeObject<InvocationDescriptor>(receivedMessage.Data, _jsonSerializerSettings);
                    if (invocationDescriptor == null) return;
                }
                catch { return; } // ignore invalid data sent to the server.

                // if the unique identifier hasn't been set then the client doesn't want a return value.
                if (invocationDescriptor.Identifier == Guid.Empty)
                {
                    // invoke the method only.
                    try
                    {
                        await MethodInvocationStrategy.OnInvokeMethodReceivedAsync(socket, invocationDescriptor);
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
                            Result = await MethodInvocationStrategy.OnInvokeMethodReceivedAsync(socket, invocationDescriptor),
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

                    // send a message to the client containing the result.
                    var message = new Message()
                    {
                        MessageType = MessageType.MethodReturnValue,
                        Data = JsonConvert.SerializeObject(invokeResult, _jsonSerializerSettings)
                    };
                    await SendMessageAsync(socket, message).ConfigureAwait(false);
                }
            }

            // method return value.
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
        }

        public async Task InvokeClientMethodOnlyAsync(string socketId, string method) => await InvokeClientMethodAsync(socketId, method, new object[] { });

        public async Task InvokeClientMethodOnlyAsync<T1>(string socketId, string method, T1 arg1) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2>(string socketId, string method, T1 arg1, T2 arg2) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6, T7>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15 });

        public async Task InvokeClientMethodOnlyAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16) => await InvokeClientMethodAsync(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16 });

        public async Task<Result> InvokeClientMethodAsync<Result>(string socketId, string method) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { });

        public async Task<Result> InvokeClientMethodAsync<Result, T1>(string socketId, string method, T1 arg1) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2>(string socketId, string method, T1 arg1, T2 arg2) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6, T7>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15 });

        public async Task<Result> InvokeClientMethodAsync<Result, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string socketId, string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16) => await InvokeClientMethodAsync<Result>(socketId, method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16 });

        public async Task InvokeClientMethodToAllAsync(string method) => await InvokeClientMethodToAllAsync(method, new object[] { });

        public async Task InvokeClientMethodToAllAsync<T1>(string method, T1 arg1) => await InvokeClientMethodToAllAsync(method, new object[] { arg1 });

        public async Task InvokeClientMethodToAllAsync<T1, T2>(string method, T1 arg1, T2 arg2) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3>(string method, T1 arg1, T2 arg2, T3 arg3) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6, T7>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6, T7, T8>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15 });

        public async Task InvokeClientMethodToAllAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16) => await InvokeClientMethodToAllAsync(method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16 });
    }
}