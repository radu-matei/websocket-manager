"use strict";
var InvocationDescriptor_1 = require("./InvocationDescriptor");
var Message_1 = require("./Message");
var Connection = (function () {
    function Connection(url, enableLogging) {
        if (enableLogging === void 0) { enableLogging = false; }
        var _this = this;
        this.enableLogging = false;
        this.clientMethods = {};
        this.connectionMethods = {};
        this.url = url;
        this.enableLogging = enableLogging;
        this.connectionMethods['onConnected'] = function () {
            if (_this.enableLogging) {
                console.log('Connected! connectionId: ' + _this.connectionId);
            }
        };
        this.connectionMethods['onDisconnected'] = function () {
            if (_this.enableLogging) {
                console.log('Connection closed from: ' + _this.url);
            }
        };
        this.connectionMethods['onOpen'] = function (socketOpenedEvent) {
            if (_this.enableLogging) {
                console.log('WebSockets connection opened!');
            }
        };
    }
    Connection.prototype.start = function () {
        var _this = this;
        this.socket = new WebSocket(this.url);
        this.socket.onopen = function (event) {
            _this.connectionMethods['onOpen'].apply(_this, event);
        };
        this.socket.onmessage = function (event) {
            _this.message = JSON.parse(event.data);
            if (_this.message.messageType == Message_1.MessageType.Text) {
                if (_this.enableLogging) {
                    console.log('Text message received. Message: ' + _this.message.data);
                }
            }
            else if (_this.message.messageType == Message_1.MessageType.MethodInvocation) {
                var invocationDescriptor = JSON.parse(_this.message.data);
                _this.clientMethods[invocationDescriptor.methodName].apply(_this, invocationDescriptor.arguments);
            }
            else if (_this.message.messageType == Message_1.MessageType.ConnectionEvent) {
                _this.connectionId = _this.message.data;
                _this.connectionMethods['onConnected'].apply(_this);
            }
        };
        this.socket.onclose = function (event) {
            _this.connectionMethods['onDisconnected'].apply(_this);
        };
        this.socket.onerror = function (event) {
            if (_this.enableLogging) {
                console.log('Error data: ' + event.error);
            }
        };
    };
    Connection.prototype.invoke = function (methodName) {
        var args = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            args[_i - 1] = arguments[_i];
        }
        var invocationDescriptor = new InvocationDescriptor_1.InvocationDescriptor(methodName, args);
        if (this.enableLogging) {
            console.log(invocationDescriptor);
        }
        this.socket.send(JSON.stringify(invocationDescriptor));
    };
    return Connection;
}());
exports.Connection = Connection;
//# sourceMappingURL=Connection.js.map