#nullable enable
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using DTAClient.Online.Backend.EventArguments;
using DTAClient.Online.Backend.Models;
using Rampastring.Tools;

namespace DTAClient.Online.Backend
{
    public class BackendSessionManager
    {
        private readonly BackendApiClient _apiClient;
        private readonly BackendWebSocketClient _wsClient;
        private readonly PlayerIdentityService _playerIdentityService;
        private readonly GuestIdentityService _guestIdentityService;
        private readonly ClientCore.ExternalAccount.ExternalAccountService _externalAccountService;
        private SessionResponse? _currentSession;
        private string? _lobbyChannel;
        private int? _lobbySpaceId;

        public event EventHandler<SessionEventArgs>? SessionCreated;
        public event EventHandler<SessionEventArgs>? SessionUpdated;
        public event EventHandler? SessionEnded;
        public event EventHandler<OnlineUsersEventArgs>? OnlineUsersReceived;
        public event EventHandler<ReadyEventArgs>? Ready;
        public event EventHandler<ErrorEventArgs>? Error;
        public event EventHandler<RoomMemberJoinedEventArgs>? RoomMemberJoined;
        public event EventHandler<RoomMemberLeftEventArgs>? RoomMemberLeft;
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        public SessionResponse? CurrentSession => _currentSession;
        public bool IsConnected => _wsClient.IsConnected;
        public string? LobbyChannel => _lobbyChannel;
        public int? LobbySpaceId => _lobbySpaceId;

        public BackendSessionManager(
            BackendApiClient apiClient,
            BackendWebSocketClient wsClient,
            PlayerIdentityService playerIdentityService,
            GuestIdentityService guestIdentityService,
            ClientCore.ExternalAccount.ExternalAccountService externalAccountService)
        {
            _apiClient = apiClient;
            _wsClient = wsClient;
            _playerIdentityService = playerIdentityService;
            _guestIdentityService = guestIdentityService;
            _externalAccountService = externalAccountService;

            _wsClient.Connected += OnWebSocketConnected;
            _wsClient.Disconnected += OnWebSocketDisconnected;
            _wsClient.MessageReceived += OnWebSocketMessageReceived;
            _wsClient.Ready += OnWebSocketReady;
            _wsClient.Error += OnWebSocketError;
            _wsClient.RoomMemberJoined += OnRoomMemberJoined;
            _wsClient.RoomMemberLeft += OnRoomMemberLeft;
        }

        public async Task InitializeAsync()
        {
        }

        public async Task ConnectToLobbyAsync(string? guestName = null)
        {
            try
            {
                if (_playerIdentityService.IsLoggedIn())
                {
                    Logger.Log("[BackendSessionManager] User is logged in with OAuth, attempting connection");
                    await ConnectWithOAuthAsync();
                }
                else
                {
                    Logger.Log("[BackendSessionManager] User is not logged in, attempting guest login");
                    await ConnectAsGuestAsync(guestName);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[BackendSessionManager] Failed to connect to lobby: {ex.Message}");
                throw;
            }
        }

        private async Task ConnectWithOAuthAsync()
        {
            string accessToken = _playerIdentityService.GetAccessToken();
            _apiClient.SetAccessToken(accessToken);

            try
            {
                Logger.Log("[BackendSessionManager] Requesting WebSocket ticket with OAuth token");
                var ticketResponse = await _apiClient.ConnectAsUserAsync();
                await CompleteConnection(ticketResponse);
            }
            catch (BackendApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                                                         ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                Logger.Log($"[BackendSessionManager] Token expired or invalid ({ex.StatusCode}), attempting refresh");
                bool refreshed = await _externalAccountService.RefreshTokenAsync();
                
                if (refreshed)
                {
                    Logger.Log("[BackendSessionManager] Token refreshed successfully, retrying connection");
                    accessToken = _playerIdentityService.GetAccessToken();
                    _apiClient.SetAccessToken(accessToken);
                    
                    var ticketResponse = await _apiClient.ConnectAsUserAsync();
                    await CompleteConnection(ticketResponse);
                }
                else
                {
                    Logger.Log("[BackendSessionManager] Token refresh failed, user needs to re-login");
                    throw new InvalidOperationException("OAuth token expired and refresh failed. Please login again.");
                }
            }
        }

        private async Task ConnectAsGuestAsync(string? guestName)
        {
            string accessToken = await _guestIdentityService.LoginAsGuestAsync(guestName);
            
            Logger.Log("[BackendSessionManager] Obtained guest access token, requesting WebSocket ticket");
            var ticketResponse = await _apiClient.ConnectAsUserAsync();
            await CompleteConnection(ticketResponse);
        }

        private async Task CompleteConnection(ConnectTicketResponse ticketResponse)
        {
            _currentSession = new SessionResponse { Id = ticketResponse.SessionId };
            SessionCreated?.Invoke(this, new SessionEventArgs(_currentSession));
            await ConnectWebSocketAsync(ticketResponse.WsTicket);
        }

        public async Task CreateGuestSessionAsync(string? guestName = null)
        {
            await ConnectToLobbyAsync(guestName);
        }

        public async Task JoinSpaceAsync(int spaceId)
        {
            await _apiClient.JoinSpaceAsync(spaceId);
        }

        public async Task LeaveSpaceAsync(int spaceId)
        {
            await _apiClient.LeaveSpaceAsync(spaceId);
        }

        public async Task EndSessionAsync()
        {
            if (_currentSession == null)
                return;

            await _apiClient.DeleteSessionAsync();
            await _wsClient.DisconnectAsync();

            _currentSession = null;

            SessionEnded?.Invoke(this, EventArgs.Empty);
        }

        private async Task ConnectWebSocketAsync(string ticket)
        {
            await _wsClient.ConnectAsync(ticket);
        }

        private void OnWebSocketConnected(object? sender, EventArgs e)
        {
            Logger.Log("Backend WebSocket connected");
            _ = FetchOnlineUsersAsync();
        }

        private async Task FetchOnlineUsersAsync()
        {
            try
            {
                var onlineUsers = await _apiClient.GetOnlineUsersAsync();
                OnlineUsersReceived?.Invoke(this, new OnlineUsersEventArgs(onlineUsers));
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to fetch online users: {ex.Message}");
            }
        }

        private void OnWebSocketDisconnected(object? sender, WebSocketErrorEventArgs e)
        {
            Logger.Log($"Backend WebSocket disconnected: {e.Message}");
        }

        private void OnWebSocketReady(object? sender, ReadyEventArgs e)
        {
            Logger.Log($"Backend WebSocket ready: {e.Data.UserInfo.Nickname}");
            _lobbyChannel = e.Data.LobbyInfo.Channel;
            _lobbySpaceId = e.Data.LobbyInfo.Id;
            Ready?.Invoke(this, e);
        }

        private void OnWebSocketError(object? sender, ErrorEventArgs e)
        {
            Logger.Log($"Backend WebSocket error: {e.Data.Code} - {e.Data.Message}");
            Error?.Invoke(this, e);
        }

        private void OnWebSocketMessageReceived(object? sender, WebSocketMessageEventArgs e)
        {
            switch (e.Message.EventType)
            {
                case "message_sent":
                    HandleNewMessage(e.Message);
                    break;
                case "user_joined":
                    HandleUserJoined(e.Message);
                    break;
                case "user_left":
                    HandleUserLeft(e.Message);
                    break;
                case "room_updated":
                    HandleSpaceUpdated(e.Message);
                    break;
            }
        }

        private void HandleNewMessage(WebSocketMessage message)
        {
            if (message.Data.HasValue)
            {
                var data = JsonSerializer.Deserialize<MessageSentEventData>(message.Data.Value);
                if (data != null)
                {
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(data));
                }
            }
        }

        private void HandleUserJoined(WebSocketMessage message)
        {
        }

        private void HandleUserLeft(WebSocketMessage message)
        {
        }

        private void HandleSpaceUpdated(WebSocketMessage message)
        {
        }

        private void OnRoomMemberJoined(object? sender, RoomMemberJoinedEventArgs e)
        {
            Logger.Log($"[BackendSessionManager] Room member joined: {e.Data.Nickname} in room {e.Data.RoomId}");
            RoomMemberJoined?.Invoke(this, e);
        }

        private void OnRoomMemberLeft(object? sender, RoomMemberLeftEventArgs e)
        {
            Logger.Log($"[BackendSessionManager] Room member left: user {e.Data.UserId} from room {e.Data.RoomId}");
            RoomMemberLeft?.Invoke(this, e);
        }
    }
}
