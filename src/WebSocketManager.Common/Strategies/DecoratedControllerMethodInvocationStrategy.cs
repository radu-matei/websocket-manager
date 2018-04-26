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
    /// The decorated controller method invocation strategy. Finds methods in several classes using reflection.
    /// </summary>
    /// <seealso cref="WebSocketManager.Common.ControllerMethodInvocationStrategy"/>
    public class DecoratedControllerMethodInvocationStrategy : MethodInvocationStrategy
    {
        /// <summary>
        /// Gets the method name prefix. This prevents users from calling methods they aren't
        /// supposed to call. You could for example use the awesome 'ᐅ' character.
        /// </summary>
        /// <value>The method name prefix.</value>
        public string Prefix { get; } = "";

        /// <summary>
        /// Gets the prefix and method name separator. Default value is a forward slash '/'.
        /// </summary>
        /// <value>The separator to separate prefix and method name.</value>
        public char Separator { get; } = '/';

        /// <summary>
        /// Gets a value indicating whether there is no websocket argument (useful for client-side methods).
        /// </summary>
        /// <value><c>true</c> if there is no websocket argument; otherwise, <c>false</c>.</value>
        public bool NoWebsocketArgument { get; set; } = false;

        /// <summary>
        /// Gets the registered controllers.
        /// </summary>
        /// <value>The registered controllers.</value>
        public Dictionary<string, object> Controllers { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="DecoratedControllerMethodInvocationStrategy"/> class.
        /// </summary>
        public DecoratedControllerMethodInvocationStrategy()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="DecoratedControllerMethodInvocationStrategy"/> class.
        /// </summary>
        /// <param name="prefix">
        /// The method name prefix. This prevents users from calling methods they aren't supposed to
        /// call. You could for example use the awesome 'ᐅ' character.
        /// </param>
        public DecoratedControllerMethodInvocationStrategy(string prefix)
        {
            Prefix = prefix;
        }

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="DecoratedControllerMethodInvocationStrategy"/> class.
        /// </summary>
        /// <param name="prefix">
        /// The method name prefix. This prevents users from calling methods they aren't supposed to
        /// call. You could for example use the awesome 'ᐅ' character.
        /// </param>
        /// <param name="separator">The prefix and method name separator. Default value is a forward slash '/'.</param>
        public DecoratedControllerMethodInvocationStrategy(string prefix, char separator)
        {
            Prefix = prefix;
            Separator = separator;
        }

        /// <summary>
        /// Registers the specified controller to the specified prefix.
        /// </summary>
        /// <param name="prefix">The controller prefix (e.g. "session").</param>
        /// <param name="controller">The controller containing the methods.</param>
        public void Register(string prefix, object controller)
        {
            Controllers.Add(prefix.ToLower(), controller);
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
            // there must be a separator in the method name.
            if (!invocationDescriptor.MethodName.Contains(Separator)) throw new Exception($"Invalid controller or method name '{invocationDescriptor.MethodName}'.");

            // find the controller and the method name.
            string[] names = invocationDescriptor.MethodName.Split(Separator);
            string controller = names[0].ToLower();
            string command = Prefix + names[1];

            // find the desired controller.
            if (Controllers.TryGetValue(controller, out object self))
            {
                // use reflection to find the method in the desired controller.
                MethodInfo method = self.GetType().GetMethod(command);

                // if the method could not be found:
                if (method == null)
                    throw new Exception($"Received unknown command '{command}' for controller '{controller}'.");

                // optionally insert client as parameter.
                List<object> args = invocationDescriptor.Arguments.ToList();
                if (!NoWebsocketArgument)
                    args.Insert(0, socket);

                // call the method asynchronously.
                try
                {
                    return await Task.Run(() => method.Invoke(self, args.ToArray()));
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
            else throw new Exception($"Received command '{command}' for unknown controller '{controller}'.");
        }
    }
}