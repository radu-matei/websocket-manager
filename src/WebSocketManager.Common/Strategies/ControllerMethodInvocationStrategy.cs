using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketManager.Common
{
    /// <summary>
    /// The controller method invocation strategy. Finds methods in a single class using reflection.
    /// </summary>
    /// <seealso cref="WebSocketManager.Common.MethodInvocationStrategy"/>
    public class ControllerMethodInvocationStrategy : MethodInvocationStrategy
    {
        /// <summary>
        /// Gets the controller containing the methods.
        /// </summary>
        /// <value>The controller containing the methods.</value>
        public object Controller { get; set; }

        /// <summary>
        /// Gets the method name prefix. This prevents users from calling methods they aren't
        /// supposed to call. You could for example use the awesome 'ᐅ' character.
        /// </summary>
        /// <value>The method name prefix.</value>
        public string Prefix { get; } = "";

        /// <summary>
        /// Gets a value indicating whether there is no websocket argument (useful for client-side methods).
        /// </summary>
        /// <value><c>true</c> if there is no websocket argument; otherwise, <c>false</c>.</value>
        public bool NoWebsocketArgument { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerMethodInvocationStrategy"/> class.
        /// </summary>
        public ControllerMethodInvocationStrategy()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerMethodInvocationStrategy"/> class.
        /// </summary>
        /// <param name="controller">The controller containing the methods.</param>
        public ControllerMethodInvocationStrategy(object controller)
        {
            Controller = controller;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerMethodInvocationStrategy"/> class.
        /// </summary>
        /// <param name="prefix">
        /// The method name prefix. This prevents users from calling methods they aren't supposed to
        /// call. You could for example use the awesome 'ᐅ' character.
        /// </param>
        /// <param name="controller">The controller containing the methods.</param>
        public ControllerMethodInvocationStrategy(string prefix, object controller)
        {
            Prefix = prefix;
            Controller = controller;
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
            // create the method name that has to be found.
            string command = Prefix + invocationDescriptor.MethodName;

            // use reflection to find the method in the desired controller.
            MethodInfo method = Controller.GetType().GetMethod(command);
            // if the method could not be found:
            if (method == null) throw new Exception($"Received unknown command '{command}' for controller '{Controller.GetType().Name}'.");

            // optionally insert client as parameter.
            List<object> args = invocationDescriptor.Arguments.ToList();
            if (!NoWebsocketArgument)
                args.Insert(0, socket);

            // call the method asynchronously.
            try
            {
                return await Task.Run(() => method.Invoke(Controller, args.ToArray()));
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}