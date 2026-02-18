#nullable enable
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DTAClient.Online.Backend.EventArguments;
using DTAClient.Online.Backend.Models;

namespace DTAClient.Online.Backend
{
    public class BackendWebSocketClient
    {
        private ClientWebSocket? _webSocket;
        private readonly string _baseUrl;
        private string? _sessionId;
        private readonly Timer _heartbeatTimer;
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);

        public event EventHandler<WebSocketMessageEventArgs>? MessageReceived;
        public event EventHandler? Connected;
        public event EventHandler<WebSocketErrorEventArgs>? Disconnected;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;

        public BackendWebSocketClient(string baseUrl)
        {
            _baseUrl = baseUrl;
            _heartbeatTimer = new Timer(SendHeartbeat, null, Timeout.Infinite, Timeout.Infinite);
        }

        public async Task ConnectAsync(string sessionId)
        {
            _sessionId = sessionId;

            string wsUrl = _baseUrl.Replace("http://", "ws://").Replace("https://", "wss://");
            wsUrl += $"/v1/ws?session_id={sessionId}";

            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

            Connected?.Invoke(this, EventArgs.Empty);

            _heartbeatTimer.Change(TimeSpan.Zero, _heartbeatInterval);

            _ = ReceiveMessagesAsync();
        }

        public async Task DisconnectAsync()
        {
            _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }

            _webSocket?.Dispose();
            _webSocket = null;
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];

            while (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync();
                        Disconnected?.Invoke(this, new WebSocketErrorEventArgs("WebSocket closed"));
                        return;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var wsMessage = JsonSerializer.Deserialize<WebSocketMessage>(message);

                        if (wsMessage != null)
                        {
                            MessageReceived?.Invoke(this, new WebSocketMessageEventArgs(wsMessage));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Disconnected?.Invoke(this, new WebSocketErrorEventArgs(ex.Message));
                    return;
                }
            }
        }

        private void SendHeartbeat(object? state)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                _ = SendAsync(new WebSocketMessage { Event = "PING" });
            }
        }

        private async Task SendAsync(WebSocketMessage message)
        {
            if (_webSocket?.State != WebSocketState.Open)
                return;

            string json = JsonSerializer.Serialize(message);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
