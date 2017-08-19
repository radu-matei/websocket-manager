using System;
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

namespace WebSocketManager
{

  public class WebSocketConnection
  {
    public string Id;
    public WebSocket Socket;
    public IQueryCollection Query { get; internal set; }
  }

}