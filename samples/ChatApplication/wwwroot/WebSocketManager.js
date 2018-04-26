/**
 * The WebSocketManager JavaScript Client. See https://github.com/radu-matei/websocket-manager/ for more information.
 */
var WebSocketManager = (function () {
    /**
     * Create a new web socket manager.
     * @param {any} url The web socket url (must start with ws://).
     */
    var constructor = function (url) {
        if (url === undefined) console.error("WebSocketManager constructor requires valid 'url'.");
        _this = this;

        /** Collection of methods on this client. */
        this.methods = [];

        ///////////////////////////////////////////////////////////////////////////////////////////

        /**
         * Create a new networking message.
         */
        var Message = function (messageType, data) {
            this.$type = 'WebSocketManager.Common.Message';
            this.messageType = messageType;
            this.data = data;
        };
        /** Text message (constant: 0). */
        Message.Text = 0;
        /** Remote method invocation request message (constant: 1). */
        Message.MethodInvocation = 1;
        /** Connection event message (constant: 2). */
        Message.ConnectionEvent = 2;
        /** Remote method return value message (constant: 3). */
        Message.MethodReturnValue = 3;

        ///////////////////////////////////////////////////////////////////////////////////////////

        /**
         * Create a new invocation descriptor.
         * @param {any} methodName The name of the remote method.
         * @param {any} args The arguments passed to the method.
         * @param {any} identifier The unique identifier of the invocation.
         */
        var InvocationDescriptor = function (methodName, args, identifier) {
            this.$type = 'WebSocketManager.Common.InvocationDescriptor';
            this.methodName = methodName;
            this.arguments = {
                $type: 'System.Object[]',
                $values: args
            };
            this.identifier = {
                $type: "System.Guid",
                $value: identifier
            };
        };

        ///////////////////////////////////////////////////////////////////////////////////////////

        /**
         * Represents the return value of a method that was executed remotely.
         * @param {any} identifier The unique identifier of the invocation.
         * @param {any} result The result of the method call.
         * @param {any} exception The remote exception of the method call.
         */
        var InvocationResult = function (identifier, result, exception) {
            this.$type = 'WebSocketManager.Common.InvocationResult';
            this.result = result;
            this.exception = exception;
            this.identifier = {
                $type: "System.Guid",
                $value: identifier
            };
            if (exception !== undefined) {
                this.exception = {
                    $type: "WebSocketManager.Common.RemoteException",
                    message: exception
                }
            }
        };

        ///////////////////////////////////////////////////////////////////////////////////////////

        /**
         * Collection of primitive type names and their C# mappings.
         */
        var typemappings = {
            guid: 'System.Guid',
            uuid: 'System.Guid', // convenience alias
            bool: 'System.Boolean',
            byte: 'System.Byte',
            sbyte: 'System.SByte',
            char: 'System.Char',
            decimal: 'System.Decimal',
            double: 'System.Double',
            float: 'System.Single',
            int: 'System.Int32',
            uint: 'System.UInt32',
            long: 'System.Int64',
            ulong: 'System.UInt64',
            short: 'System.Int16',
            ushort: 'System.UInt16',
            string: 'System.String',
            object: 'System.Object' // generic
        };

        ///////////////////////////////////////////////////////////////////////////////////////////

        /**
         * Generates a UUID using a random number generator and the current time.
         * This is not truly unique but it's good enough (TM).
         */
        var uuid = function () { // Public Domain/MIT
            var d = new Date().getTime();
            if (typeof performance !== 'undefined' && typeof performance.now === 'function') {
                d += performance.now(); // use high-precision timer if available
            }
            return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                var r = (d + Math.random() * 16) % 16 | 0;
                d = Math.floor(d / 16);
                return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /**
         * Takes a C# collection of $type+$value and turns them into a simple array of values.
         * @param {any} collection The C# collection of $type+$value.
         */
        var parseCSharpArguments = function (collection) {
            var args = [];
            for (var i = 0; i < collection.length; i++)
                args.push(collection[i].$value);
            return args;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /**
         * The waiting remote invocations for Client to Server method calls (return values).
         */
        var waitingRemoteInvocations = {};

        ///////////////////////////////////////////////////////////////////////////////////////////

        /**
         * Called whenever the socket opens the connection.
         * @param {any} event The associated event data.
         */
        var onSocketOpen = function (event) {
        };

        /**
         * Called whenever the socket closes the connection.
         * @param {any} event The associated event data.
         */
        var onSocketClose = function (event) {
            // public event:
            if (_this.onDisconnected !== undefined) _this.onDisconnected();
        };

        /**
         * Called whenever the socket has an error.
         * @param {any} event The associated event data.
         */
        var onSocketError = function (event) {
            console.error("WebSocketManager error:");
            console.error(event);
        };

        /**
         * Called whenever there is an incoming message.
         * @param {any} message The Message that was received.
         */
        var onSocketMessage = function (message) {
            // CONNECTION EVENT
            if (message.messageType === Message.ConnectionEvent) {
                // we received the unique identifier from the server.
                _this.id = message.data.$value;
                // public event:
                if (_this.onConnected !== undefined) _this.onConnected(_this.id);
            }

            // TEXT EVENT
            else if (message.messageType === Message.Text) {
                // public event:
                if (_this.onMessage !== undefined) _this.onMessage(message.data.$value);
            }

            // METHOD INVOCATION EVENT
            else if (message.messageType === Message.MethodInvocation) {
                var data = JSON.parse(message.data.$value);
                // find the method.
                if (_this.methods[data.methodName.$value] !== undefined) {
                    // call the method and catch any exceptions.
                    var result, error = undefined;
                    try { result = _this.methods[data.methodName.$value].apply(_this, parseCSharpArguments(data.arguments['$values'])); }
                    catch (e) { error = e; }

                    // if the server desires a result we send a method return value.
                    if (data.identifier.$value !== '00000000-0000-0000-0000-000000000000') {
                        // an error occured so let the server know.
                        if (error !== undefined) {
                            // send web-socket message to the server.
                            _this.socket.send(JSON.stringify(new Message(Message.MethodReturnValue,
                                JSON.stringify(new InvocationResult(data.identifier.$value, null, "A remote exception occured: " + error))
                            )));
                        }
                        // send result value to the server.
                        else {
                            // try finding an appropriate C# type.
                            if (typemappings[result[0]] !== undefined)
                                result[0] = typemappings[result[0]];
                            // send web-socket message to the server.
                            _this.socket.send(JSON.stringify(new Message(Message.MethodReturnValue,
                                JSON.stringify(new InvocationResult(data.identifier.$value, { $type: result[0], $value: result[1] }))
                            )));
                        }
                    }
                } else console.error("WebSocketManager: Server attempted to invoke unknown method '" + data.methodName.$value + "'!");
            }

            // METHOD RETURN VALUE EVENT
            else if (message.messageType === Message.MethodReturnValue) {
                var data = JSON.parse(message.data.$value);
                // find the waiting remote invocation.
                var callback = waitingRemoteInvocations[data.identifier.$value];
                // remove it from the waiting list.
                delete waitingRemoteInvocations[data.identifier.$value];
                // call the callback.
                if (data.exception !== null)
                    callback(undefined, data.exception.message.$value);
                else
                    callback(data.result.$value, undefined);
            }

            //console.log(message);
        };

        ///////////////////////////////////////////////////////////////////////////////////////////

        /**
         * Connects to the server.
         */
        this.connect = function () {
            // create a new web-socket connection to the server.
            _this.socket = new WebSocket(url);

            _this.socket.onopen = function (event) {
                onSocketOpen(event);
            }

            _this.socket.onclose = function (event) {
                // run all the callbacks on the waiting list so the program continues.
                Object.keys(waitingRemoteInvocations).forEach(function (guid) {
                    waitingRemoteInvocations[guid](undefined, 'The web-socket connection was closed.');
                });
                waitingRemoteInvocations = {};

                onSocketClose(event);
            }

            _this.socket.onerror = function (event) {
                onSocketError(event);
            }

            _this.socket.onmessage = function (event) {
                onSocketMessage(JSON.parse(event.data));
            }
        };

        /**
         * Invoke a remote method on the server only, without a return value.
         * @param {any} method The name of the remote method to be invoked.
         */
        this.invokeOnly = function (method) {
            var args = [];
            // iterate through all arguments and find type/value relationships.
            for (var i = 1; i < arguments.length; i += 2) {
                var type = arguments[i];
                var value = arguments[i + 1];
                // try finding an appropriate C# type.
                if (typemappings[type] !== undefined)
                    type = typemappings[type];
                // even if we can't find a C# type we assume the user knows what he's doing.
                args.push({ $type: type, $value: value });
            }

            // send web-socket message to the server.
            _this.socket.send(JSON.stringify(new Message(Message.MethodInvocation,
                JSON.stringify(new InvocationDescriptor(method, args, '00000000-0000-0000-0000-000000000000'))
            )));
        }

        /**
         * Invoke a remote method on the server, with a callback for the return value.
         * @param {any} method The name of the remote method to be invoked.
         */
        this.invoke = function (method) {
            var args = [];
            // iterate through all arguments and find type/value relationships.
            for (var i = 1; i < arguments.length - 1; i += 2) {
                var type = arguments[i];
                var value = arguments[i + 1];
                // try finding an appropriate C# type.
                if (typemappings[type] !== undefined)
                    type = typemappings[type];
                // even if we can't find a C# type we assume the user knows what he's doing.
                args.push({ $type: type, $value: value });
            }
            // the last argument should be the callback method.
            var callback = arguments[arguments.length - 1];
            // generate a unique identifier to associate return values.
            var guid = uuid();
            // put this call on the waiting list.
            waitingRemoteInvocations[guid] = callback;

            // send web-socket message to the server.
            _this.socket.send(JSON.stringify(new Message(Message.MethodInvocation,
                JSON.stringify(new InvocationDescriptor(method, args, guid))
            )));
        }
    };

    return constructor;
})();