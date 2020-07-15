using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketManager
{
    public class ConnectionManager
    {
        private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public WebSocket GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
        {
            return _sockets;
        }

        public string GetId(WebSocket socket)
        {
            return _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }
        
        public void AddSocket(WebSocket socket)
        {
            _sockets.TryAdd(CreateConnectionId(), socket);
        }

        public async Task CloseAndRemoveSocket(string id)
        {
            if (_sockets.TryRemove(id, out var socket))
            {
                await socket.CloseOutputAsync(closeStatus: WebSocketCloseStatus.NormalClosure, 
                    statusDescription: "Closed by the ConnectionManager", 
                    cancellationToken: CancellationToken.None);
            }
        }

        public Task RemoveSocket(string id)
        {
            if (_sockets.TryRemove(id, out var socket))
            {
                socket.Dispose();
            }

            return Task.CompletedTask;
        }

        private string CreateConnectionId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}