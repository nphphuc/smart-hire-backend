using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace The_Hirelo.Common
{
    public interface IWebSocketManager
    {
        void AddSocket(string userId, WebSocket socket);
        void RemoveSocket(string userId);
        Task SendToUserAsync(string userId, string message);
    }

    public class WebSocketManager : IWebSocketManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

        public void AddSocket(string userId, WebSocket socket)
            => _sockets[userId] = socket;

        public void RemoveSocket(string userId)
            => _sockets.TryRemove(userId, out _);

        public async Task SendToUserAsync(string userId, string message)
        {
            if (!_sockets.TryGetValue(userId, out var socket)) return;
            if (socket.State != WebSocketState.Open) { RemoveSocket(userId); return; }

            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);
        }
    }
}
