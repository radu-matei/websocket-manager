using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketManager
{
  public class WebSocketConnectionManager
  {

    private ConcurrentDictionary<string, WebSocketConnection> _sockets = new ConcurrentDictionary<string, WebSocketConnection>();

    public WebSocketConnection GetSocketById(string id)
    {
      return _sockets.FirstOrDefault(p => p.Key == id).Value;
    }

    public ConcurrentDictionary<string, WebSocketConnection> GetAll()
    {
      return _sockets;
    }

    public List<WebSocketConnection> Connections()
    {
      return _sockets.Values.Where(x => x.Socket.State == WebSocketState.Open).ToList();
    }

    public string GetId(WebSocket socket)
    {
      return _sockets.Values.FirstOrDefault(p => p.Socket == socket).Id;
    }
    public void AddSocket(WebSocket socket)
    {
      string id = CreateConnectionId();
      _sockets.TryAdd(id, new WebSocketConnection { Id = id, Socket = socket });
    }

    public async Task RemoveSocket(string id)
    {
      WebSocketConnection connection;
      if (_sockets.TryRemove(id, out connection))
      {

        await connection.Socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                statusDescription: "Closed by the WebSocketManager",
                                cancellationToken: CancellationToken.None).ConfigureAwait(false);
      }
    }

    private string CreateConnectionId()
    {
      return Guid.NewGuid().ToString();
    }
  }
}
