(function webpackUniversalModuleDefinition(root, factory) {
	if(typeof exports === 'object' && typeof module === 'object')
		module.exports = factory();
	else if(typeof define === 'function' && define.amd)
		define("WebSocketManager", [], factory);
	else if(typeof exports === 'object')
		exports["WebSocketManager"] = factory();
	else
		root["WebSocketManager"] = factory();
})(this, function() {
return /******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId])
/******/ 			return installedModules[moduleId].exports;
/******/
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			exports: {},
/******/ 			id: moduleId,
/******/ 			loaded: false
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.loaded = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(0);
/******/ })
/************************************************************************/
/******/ ([
/* 0 */
/***/ function(module, exports, __webpack_require__) {

	module.exports = __webpack_require__(1);


/***/ },
/* 1 */
/***/ function(module, exports, __webpack_require__) {

	"use strict";
	var InvocationDescriptor_1 = __webpack_require__(2);
	var Message_1 = __webpack_require__(3);
	var Connection = (function () {
	    function Connection(url, enableLogging) {
	        var _this = this;
	        if (enableLogging === void 0) { enableLogging = false; }
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


/***/ },
/* 2 */
/***/ function(module, exports) {

	"use strict";
	var InvocationDescriptor = (function () {
	    function InvocationDescriptor(methodName, args) {
	        this.methodName = methodName;
	        this.arguments = args;
	    }
	    return InvocationDescriptor;
	}());
	exports.InvocationDescriptor = InvocationDescriptor;


/***/ },
/* 3 */
/***/ function(module, exports) {

	"use strict";
	(function (MessageType) {
	    MessageType[MessageType["Text"] = 0] = "Text";
	    MessageType[MessageType["MethodInvocation"] = 1] = "MethodInvocation";
	    MessageType[MessageType["ConnectionEvent"] = 2] = "ConnectionEvent";
	})(exports.MessageType || (exports.MessageType = {}));
	var MessageType = exports.MessageType;
	var Message = (function () {
	    function Message() {
	    }
	    return Message;
	}());
	exports.Message = Message;


/***/ }
/******/ ])
});
;
//# sourceMappingURL=WebSocketManager.js.map