#nullable enable
using System;
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
        private SessionResponse? _currentSession;
        private string? _lobbyChannel;

        public event EventHandler<SessionEventArgs>? SessionCreated;
        public event EventHandler<SessionEventArgs>? SessionUpdated;
        public event EventHandler? SessionEnded;
        public event EventHandler<OnlineUsersEventArgs>? OnlineUsersReceived;
        public event EventHandler<ReadyEventArgs>? Ready;
        public event EventHandler<ErrorEventArgs>? Error;

        public SessionResponse? CurrentSession => _currentSession;
        public bool IsConnected => _wsClient.IsConnected;
        public string? LobbyChannel => _lobbyChannel;
        
        public BackendSessionManager(BackendApiClient apiClient, BackendWebSocketClient wsClient, PlayerIdentityService playerIdentityService)
        {
            _apiClient = apiClient;
            _wsClient = wsClient;
            _playerIdentityService = playerIdentityService;

            _wsClient.Connected += OnWebSocketConnected;
            _wsClient.Disconnected += OnWebSocketDisconnected;
            _wsClient.MessageReceived += OnWebSocketMessageReceived;
            _wsClient.Ready += OnWebSocketReady;
            _wsClient.Error += OnWebSocketError;
        }

        public async Task InitializeAsync()
        {
        }

        public async Task ConnectToLobbyAsync(string? guestName = null)
        {
            try
            {
                ConnectTicketResponse ticketResponse;

                if (_playerIdentityService.IsLoggedIn())
                {
                    _apiClient.SetAccessToken(_playerIdentityService.GetAccessToken());
                    ticketResponse = await _apiClient.ConnectAsUserAsync();
                }
                else
                {
                    ticketResponse = await _apiClient.ConnectAsGuestAsync(guestName);
                }

                _currentSession = new SessionResponse { Id = ticketResponse.SessionId };
                await ConnectWebSocketAsync(ticketResponse.WsTicket);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to connect to lobby: {ex.Message}");
                throw;
            }
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
    }
}
