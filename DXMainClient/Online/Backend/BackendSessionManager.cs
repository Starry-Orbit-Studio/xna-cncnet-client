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
        private readonly Timer _httpHeartbeatTimer;
        private SessionResponse? _currentSession;
        private string? _savedSessionId;

        public event EventHandler<SessionEventArgs>? SessionCreated;
        public event EventHandler<SessionEventArgs>? SessionUpdated;
        public event EventHandler? SessionEnded;

        public SessionResponse? CurrentSession => _currentSession;
        public bool IsConnected => _wsClient.IsConnected;

        public BackendSessionManager(BackendApiClient apiClient, BackendWebSocketClient wsClient)
        {
            _apiClient = apiClient;
            _wsClient = wsClient;
            _httpHeartbeatTimer = new Timer(SendHttpHeartbeat, null, Timeout.Infinite, Timeout.Infinite);

            _wsClient.Connected += OnWebSocketConnected;
            _wsClient.Disconnected += OnWebSocketDisconnected;
            _wsClient.MessageReceived += OnWebSocketMessageReceived;
        }

        public async Task InitializeAsync()
        {
            _savedSessionId = LoadSessionIdFromStorage();

            if (!string.IsNullOrEmpty(_savedSessionId))
            {
                try
                {
                    _currentSession = await _apiClient.GetSessionInfoAsync(_savedSessionId);
                    if (_currentSession != null && _currentSession.IsActive)
                    {
                        await ConnectWebSocketAsync();
                        return;
                    }
                }
                catch
                {
                }
            }

            await CreateGuestSessionAsync();
        }

        public async Task CreateGuestSessionAsync(string? guestName = null)
        {
            _currentSession = await _apiClient.CreateGuestSessionAsync(guestName);
            SaveSessionIdToStorage(_currentSession.Id);

            SessionCreated?.Invoke(this, new SessionEventArgs(_currentSession));

            await ConnectWebSocketAsync();
        }

        public async Task BindUserAsync(int userId)
        {
            if (_currentSession == null)
                throw new InvalidOperationException("No active session");

            _currentSession = await _apiClient.BindUserToSessionAsync(userId);
            SessionUpdated?.Invoke(this, new SessionEventArgs(_currentSession));
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
            _httpHeartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);

            _currentSession = null;
            ClearSessionIdStorage();

            SessionEnded?.Invoke(this, EventArgs.Empty);
        }

        private async Task ConnectWebSocketAsync()
        {
            await _wsClient.ConnectAsync(_currentSession!.Id);
            _httpHeartbeatTimer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        private void SendHttpHeartbeat(object? state)
        {
            _ = _apiClient.SendHeartbeatAsync();
        }

        private void OnWebSocketConnected(object? sender, EventArgs e)
        {
            Logger.Log("Backend WebSocket connected");
        }

        private void OnWebSocketDisconnected(object? sender, WebSocketErrorEventArgs e)
        {
            Logger.Log($"Backend WebSocket disconnected: {e.Message}");
        }

        private void OnWebSocketMessageReceived(object? sender, WebSocketMessageEventArgs e)
        {
            switch (e.Message.Event)
            {
                case "NEW_MESSAGE":
                    HandleNewMessage(e.Message);
                    break;
                case "USER_JOINED":
                    HandleUserJoined(e.Message);
                    break;
                case "USER_LEFT":
                    HandleUserLeft(e.Message);
                    break;
                case "SPACE_UPDATED":
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

        private string? LoadSessionIdFromStorage()
        {
            return UserINISettings.Instance.BackendSessionId.Value;
        }

        private void SaveSessionIdToStorage(string sessionId)
        {
            UserINISettings.Instance.BackendSessionId.Value = sessionId;
            UserINISettings.Instance.SaveSettings();
        }

        private void ClearSessionIdStorage()
        {
            UserINISettings.Instance.BackendSessionId.Value = string.Empty;
            UserINISettings.Instance.SaveSettings();
        }
    }
}
