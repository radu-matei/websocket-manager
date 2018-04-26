using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketManager.Common
{
    /// <summary>
    /// The string method invocation strategy. Finds methods by registering the names and callbacks.
    /// </summary>
    public class StringMethodInvocationStrategy : MethodInvocationStrategy
    {
        /// <summary>
        /// The registered handlers.
        /// </summary>
        private Dictionary<string, InvocationHandler> _handlers = new Dictionary<string, InvocationHandler>();

        /// <summary>
        /// Registers the specified method name and calls the action.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="handler">The handler action with arguments.</param>
        public void On(string methodName, Action<object[]> handler)
        {
            var invocationHandler = new InvocationHandler(handler, new Type[] { });
            _handlers.Add(methodName, invocationHandler);
        }

        /// <summary>
        /// Registers the specified method name and calls the function.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="handler">The handler function with arguments and return value.</param>
        public void On(string methodName, Func<object[], object> handler)
        {
            var invocationHandler = new InvocationHandler(handler, new Type[] { });
            _handlers.Add(methodName, invocationHandler);
        }

        private class InvocationHandler
        {
            public Func<object[], object> Handler { get; set; }
            public Type[] ParameterTypes { get; set; }

            public InvocationHandler(Func<object[], object> handler, Type[] parameterTypes)
            {
                Handler = handler;
                ParameterTypes = parameterTypes;
            }

            public InvocationHandler(Action<object[]> handler, Type[] parameterTypes)
            {
                Handler = (args) => { handler(args); return null; };
                ParameterTypes = parameterTypes;
            }
        }

        /// <summary>
        /// Called when an invoke method call has been received.
        /// </summary>
        /// <param name="socket">The web-socket of the client that wants to invoke a method.</param>
        /// <param name="invocationDescriptor">
        /// The invocation descriptor containing the method name and parameters.
        /// </param>
        /// <returns>Awaitable Task.</returns>
        public override async Task<object> OnInvokeMethodReceivedAsync(WebSocket socket, InvocationDescriptor invocationDescriptor)
        {
            if (!_handlers.ContainsKey(invocationDescriptor.MethodName))
                throw new Exception($"Received unknown command '{invocationDescriptor.MethodName}'.");
            var invocationHandler = _handlers[invocationDescriptor.MethodName];
            if (invocationHandler != null)
                return await Task.Run(() => invocationHandler.Handler(invocationDescriptor.Arguments));
            return await Task.FromResult<object>(null);
        }
    }
}