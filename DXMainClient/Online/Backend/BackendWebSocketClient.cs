#nullable enable
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using DTAClient.Online.Backend.EventArguments;
using DTAClient.Online.Backend.Models;
using Rampastring.Tools;

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
        public event EventHandler<UserJoinedEventArgs>? UserJoined;
        public event EventHandler<UserLeftEventArgs>? UserLeft;
        public event EventHandler<UserStatusChangedEventArgs>? UserStatusChanged;
        public event EventHandler<RoomCreatedEventArgs>? RoomCreated;
        public event EventHandler<RoomUpdatedEventArgs>? RoomUpdated;
        public event EventHandler<RoomDeletedEventArgs>? RoomDeleted;
        public event EventHandler<RoomMemberJoinedEventArgs>? RoomMemberJoined;
        public event EventHandler<RoomMemberLeftEventArgs>? RoomMemberLeft;
        public event EventHandler<RoomStatusChangedEventArgs>? RoomStatusChanged;
        public event EventHandler<MessageSentEventArgs>? MessageSent;
        public event EventHandler<MessageEditedEventArgs>? MessageEdited;
        public event EventHandler<MessageDeletedEventArgs>? MessageDeleted;
        public event EventHandler<AnnouncementEventArgs>? Announcement;
        public event EventHandler<NotificationEventArgs>? Notification;
        public event EventHandler<MatchFoundEventArgs>? MatchFound;
        public event EventHandler<MatchCancelledEventArgs>? MatchCancelled;
        public event EventHandler<string>? DebugLog;
        public event EventHandler<ReadyEventArgs>? Ready;
        public event EventHandler<ErrorEventArgs>? Error;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;

        public BackendWebSocketClient(string baseUrl)
        {
            _baseUrl = baseUrl;
            _heartbeatTimer = new Timer(SendHeartbeat, null, Timeout.Infinite, Timeout.Infinite);
        }

        public async Task ConnectAsync(string ticket)
        {
            string wsUrl = _baseUrl.Replace("http://", "ws://").Replace("https://", "wss://");
            wsUrl += $"/api/v1/ws?ticket={ticket}";

            if (ClientConfiguration.Instance.EnableBackendDebugLog)
                Logger.Log($"[Backend WebSocket] Connecting to {wsUrl}");

            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

            if (ClientConfiguration.Instance.EnableBackendDebugLog)
                Logger.Log("[Backend WebSocket] Connected");

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
                            if (ClientConfiguration.Instance.EnableBackendDebugLog && wsMessage.EventType != "pong")
                            {
                                Logger.Log($"[Backend WebSocket] Event: {wsMessage.EventType}");
                                if (wsMessage.Data.HasValue)
                                {
                                    Logger.Log($"[Backend WebSocket] Data: {wsMessage.Data.Value}");
                                }
                            }

                            MessageReceived?.Invoke(this, new WebSocketMessageEventArgs(wsMessage));
                            DispatchEvent(wsMessage);
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

        private void DispatchEvent(WebSocketMessage message)
        {
            switch (message.EventType)
            {
                case "ready":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<ReadyEventData>(message.Data.Value);
                        if (data != null)
                            Ready?.Invoke(this, new ReadyEventArgs(data));
                    }
                    break;

                case "error":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<ErrorEventData>(message.Data.Value);
                        if (data != null)
                            Error?.Invoke(this, new ErrorEventArgs(data));
                    }
                    break;

                case "user_joined":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<UserJoinedEventData>(message.Data.Value);
                        if (data != null)
                            UserJoined?.Invoke(this, new UserJoinedEventArgs(data));
                    }
                    break;

                case "user_left":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<UserLeftEventData>(message.Data.Value);
                        if (data != null)
                            UserLeft?.Invoke(this, new UserLeftEventArgs(data));
                    }
                    break;

                case "user_status_changed":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<UserStatusChangedEventData>(message.Data.Value);
                        if (data != null)
                            UserStatusChanged?.Invoke(this, new UserStatusChangedEventArgs(data));
                    }
                    break;

                case "room_created":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<RoomCreatedEventData>(message.Data.Value);
                        if (data != null)
                            RoomCreated?.Invoke(this, new RoomCreatedEventArgs(data));
                    }
                    break;

                case "room_updated":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<RoomUpdatedEventData>(message.Data.Value);
                        if (data != null)
                            RoomUpdated?.Invoke(this, new RoomUpdatedEventArgs(data));
                    }
                    break;

                case "room_deleted":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<RoomDeletedEventData>(message.Data.Value);
                        if (data != null)
                            RoomDeleted?.Invoke(this, new RoomDeletedEventArgs(data));
                    }
                    break;

                case "room_member_joined":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<RoomMemberJoinedEventData>(message.Data.Value);
                        if (data != null)
                            RoomMemberJoined?.Invoke(this, new RoomMemberJoinedEventArgs(data));
                    }
                    break;

                case "room_member_left":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<RoomMemberLeftEventData>(message.Data.Value);
                        if (data != null)
                            RoomMemberLeft?.Invoke(this, new RoomMemberLeftEventArgs(data));
                    }
                    break;

                case "room_status_changed":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<RoomStatusChangedEventData>(message.Data.Value);
                        if (data != null)
                            RoomStatusChanged?.Invoke(this, new RoomStatusChangedEventArgs(data));
                    }
                    break;

                case "message_sent":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<MessageSentEventData>(message.Data.Value);
                        if (data != null)
                            MessageSent?.Invoke(this, new MessageSentEventArgs(data));
                    }
                    break;

                case "message_edited":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<MessageEditedEventData>(message.Data.Value);
                        if (data != null)
                            MessageEdited?.Invoke(this, new MessageEditedEventArgs(data));
                    }
                    break;

                case "message_deleted":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<MessageDeletedEventData>(message.Data.Value);
                        if (data != null)
                            MessageDeleted?.Invoke(this, new MessageDeletedEventArgs(data));
                    }
                    break;

                case "announcement":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<AnnouncementEventData>(message.Data.Value);
                        if (data != null)
                            Announcement?.Invoke(this, new AnnouncementEventArgs(data));
                    }
                    break;

                case "notification":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<NotificationEventData>(message.Data.Value);
                        if (data != null)
                            Notification?.Invoke(this, new NotificationEventArgs(data));
                    }
                    break;

                case "match_found":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<MatchFoundEventData>(message.Data.Value);
                        if (data != null)
                            MatchFound?.Invoke(this, new MatchFoundEventArgs(data));
                    }
                    break;

                case "match_cancelled":
                    if (message.Data.HasValue)
                    {
                        var data = JsonSerializer.Deserialize<MatchCancelledEventData>(message.Data.Value);
                        if (data != null)
                            MatchCancelled?.Invoke(this, new MatchCancelledEventArgs(data));
                    }
                    break;
            }
        }

        private void SendHeartbeat(object? state)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                _ = SendAsync(new WebSocketClientMessage { Action = "HEARTBEAT" });
            }
        }

        public async Task SubscribeAsync(string channelType, string channelId)
        {
            await SendAsync(new WebSocketClientMessage
            {
                Action = "SUBSCRIBE",
                ChannelType = channelType,
                ChannelId = channelId
            });
        }

        public async Task UnsubscribeAsync(string channelType, string channelId)
        {
            await SendAsync(new WebSocketClientMessage
            {
                Action = "UNSUBSCRIBE",
                ChannelType = channelType,
                ChannelId = channelId
            });
        }

        public async Task SendMessageAsync(string channel, string content, string type = "room")
        {
            await SendAsync(new WebSocketClientMessage
            {
                Action = "SEND_MESSAGE",
                Payload = new MessagePayload { Type = type, Content = content, Channel = channel }
            });
        }

        public async Task JoinSpaceAsync(int spaceId)
        {
            await SendAsync(new WebSocketClientMessage
            {
                Action = "JOIN_SPACE",
                SpaceId = spaceId
            });
        }

        public async Task LeaveSpaceAsync(int spaceId)
        {
            await SendAsync(new WebSocketClientMessage
            {
                Action = "LEAVE_SPACE",
                SpaceId = spaceId
            });
        }

        private async Task SendAsync(WebSocketClientMessage message)
        {
            if (_webSocket?.State != WebSocketState.Open)
                return;

            string json = JsonSerializer.Serialize(message);

            if (ClientConfiguration.Instance.EnableBackendDebugLog && message.Action != "HEARTBEAT")
            {
                Logger.Log($"[Backend WebSocket] Send: {json}");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}