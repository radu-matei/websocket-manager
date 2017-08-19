﻿using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebSocketManager.Common;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Collections.Generic;

namespace WebSocketManager
{

  public abstract class WebSocketHandler
  {
    // Method-cache
    private Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

    protected WebSocketConnectionManager WebSocketConnectionManager { get; set; }
    private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
    {
      ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
    public WebSocketHandler(WebSocketConnectionManager webSocketConnectionManager)
    {
      WebSocketConnectionManager = webSocketConnectionManager;
    }

    public virtual async Task OnConnected(WebSocket socket, HttpContext context)
    {
      WebSocketConnectionManager.AddSocket(socket, context);

      await SendMessageAsync(socket, new Message()
      {
        MessageType = MessageType.ConnectionEvent,
        Data = WebSocketConnectionManager.GetId(socket)
      }).ConfigureAwait(false);
    }

    public virtual async Task OnDisconnected(WebSocket socket)
    {
      await WebSocketConnectionManager.RemoveSocket(WebSocketConnectionManager.GetId(socket)).ConfigureAwait(false);
    }

    public async Task SendMessageAsync(WebSocket socket, Message message)
    {
      if (socket.State != WebSocketState.Open)
      {
        return;
      }

      await socket.SendAsync(buffer: message.Serialized, messageType: WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);
    }

    public async Task SendMessageAsync(string connectionId, Message message)
    {
      await SendMessageAsync(WebSocketConnectionManager.GetSocketById(connectionId).Socket, message).ConfigureAwait(false);
    }


    public async Task SendMessageToAllAsync(Message message, Func<WebSocketConnection, bool> filter = null)
    {
      var connections = WebSocketConnectionManager.Connections();
      if (filter != null)
      {
        connections = connections.Where(x => filter(x));
      }
      // Get all connections to send to and send all at once, without blocking
      IEnumerable<Task> allTasks = connections.Select(x => SendMessageAsync(x.Socket, message));
      await Task.WhenAll(allTasks).ConfigureAwait(false);
    }

    public async Task InvokeClientMethodAsync(string socketId, string methodName, object[] arguments)
    {
      Message message = GetInvocationMessage(methodName, arguments);
      await SendMessageAsync(socketId, message).ConfigureAwait(false);
    }

    private Message GetInvocationMessage(string methodName, object[] arguments)
    {
      return new Message()
      {
        MessageType = MessageType.ClientMethodInvocation,
        Data = JsonConvert.SerializeObject(new InvocationDescriptor
        {
          MethodName = methodName,
          Arguments = arguments
        }, _jsonSerializerSettings)
      };
    }

    public async Task InvokeClientMethodToAllAsync(string methodName, Func<WebSocketConnection, bool> filter, params object[] arguments)
    {
      Message message = GetInvocationMessage(methodName, arguments);
      await SendMessageToAllAsync(message, filter);
    }

    public async Task InvokeClientMethodToAllAsync(string methodName, params object[] arguments)
    {
      Message message = GetInvocationMessage(methodName, arguments);
      await SendMessageToAllAsync(message);
    }

    public async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, string serializedInvocationDescriptor)
    {
      var invocationDescriptor = JsonConvert.DeserializeObject<InvocationDescriptor>(serializedInvocationDescriptor);

      MethodInfo method = GetMethod(invocationDescriptor.MethodName);


      if (method == null)
      {
        await SendMessageAsync(socket, new Message()
        {
          MessageType = MessageType.Text,
          Data = $"Cannot find method {invocationDescriptor.MethodName}"
        }).ConfigureAwait(false);
        return;
      }

      try
      {
        method.Invoke(this, invocationDescriptor.Arguments);
      }
      catch (TargetParameterCountException e)
      {
        await SendMessageAsync(socket, new Message()
        {
          MessageType = MessageType.Text,
          Data = $"The {invocationDescriptor.MethodName} method does not take {invocationDescriptor.Arguments.Length} parameters!"
        }).ConfigureAwait(false);
      }

      catch (ArgumentException e)
      {
        await SendMessageAsync(socket, new Message()
        {
          MessageType = MessageType.Text,
          Data = $"The {invocationDescriptor.MethodName} method takes different arguments!"
        }).ConfigureAwait(false);
      }
    }

    private MethodInfo GetMethod(string methodName)
    {
      MethodInfo method = null;
      if (!methods.TryGetValue(methodName, out method))
      {
        // Todo: make sure this method can be invoked
        method = this.GetType().GetMethod(methodName);
        lock (methods)
        {
          if (method != null && !methods.ContainsKey(methodName))
          {
            methods.Add(methodName, method);
          }
        }

      }
      return method;
    }

  }
}