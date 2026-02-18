#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClientCore;
using DTAClient.Online.Backend.Models;
using Rampastring.Tools;

namespace DTAClient.Online.Backend
{
    public class BackendApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public BackendApiException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class BackendApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string? _sessionId;

        public event EventHandler<string>? DebugLog;

        public BackendApiClient(string baseUrl, HttpClient httpClient)
        {
            _baseUrl = baseUrl;
            _httpClient = httpClient;
        }

        public string? SessionId => _sessionId;

        public async Task<SessionResponse> CreateGuestSessionAsync(string? guestName = null)
        {
            var request = new CreateGuestSessionRequest { GuestName = guestName };
            var response = await PostAsync<SessionResponse>("/v1/sessions/guest", request);
            _sessionId = response.Id;
            return response;
        }

        public async Task<SessionResponse> BindUserToSessionAsync(int userId)
        {
            var request = new BindUserRequest { UserId = userId };
            return await PostAsync<SessionResponse>($"/v1/sessions/{_sessionId}/bind", request);
        }

        public async Task<SessionResponse> GetSessionInfoAsync(string? sessionId = null)
        {
            string id = sessionId ?? _sessionId;
            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException("Session ID is not set");
            return await GetAsync<SessionResponse>($"/v1/sessions/{id}");
        }

        public async Task SendHeartbeatAsync()
        {
            if (string.IsNullOrEmpty(_sessionId))
                throw new InvalidOperationException("Session ID is not set");
            await PostAsync<object>($"/v1/sessions/{_sessionId}/heartbeat", null);
        }

        public async Task DeleteSessionAsync()
        {
            await DeleteAsync($"/v1/sessions/{_sessionId}");
            _sessionId = null;
        }

        public async Task<SpaceResponse> CreateSpaceAsync(CreateSpaceRequest request)
        {
            return await PostAsync<SpaceResponse>("/v1/spaces", request);
        }

        public async Task<SpaceResponse> GetSpaceAsync(int spaceId)
        {
            return await GetAsync<SpaceResponse>($"/v1/spaces/{spaceId}");
        }

        public async Task<List<SpaceResponse>> GetSpacesAsync(string? spaceType = null)
        {
            string url = "/v1/spaces";
            if (!string.IsNullOrEmpty(spaceType))
                url += $"?space_type={spaceType}";
            return await GetAsync<List<SpaceResponse>>(url);
        }

        public async Task<SpaceResponse> UpdateSpaceAsync(int spaceId, UpdateSpaceRequest request)
        {
            return await PatchAsync<SpaceResponse>($"/v1/spaces/{spaceId}", request);
        }

        public async Task DeleteSpaceAsync(int spaceId)
        {
            await DeleteAsync($"/v1/spaces/{spaceId}");
        }

        public async Task JoinSpaceAsync(int spaceId)
        {
            var request = new JoinSpaceRequest { SpaceId = spaceId };
            await PostAsync<object>("/v1/spaces/join", request);
        }

        public async Task LeaveSpaceAsync(int spaceId)
        {
            var request = new LeaveSpaceRequest { SpaceId = spaceId };
            await PostAsync<object>("/v1/spaces/leave", request);
        }

        public async Task<List<SpaceMemberResponse>> GetSpaceMembersAsync(int spaceId)
        {
            return await GetAsync<List<SpaceMemberResponse>>($"/v1/spaces/{spaceId}/members");
        }

        public async Task<OnlineUsersResponse> GetOnlineUsersAsync()
        {
            var response = await GetAsync<OnlineUsersResponse>("/v1/presence/online-users");
            string usersJson = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            Logger.Log($"[Backend] Online users response:\n{usersJson}");
            return response;
        }

        public async Task<ChatMessageResponse> SendMessageAsync(SendMessageRequest request)
        {
            return await PostAsync<ChatMessageResponse>("/v1/messages", request);
        }

        public async Task<List<ChatMessageResponse>> GetMessagesAsync(int? spaceId = null, int limit = 100, int offset = 0)
        {
            string url = "/v1/messages";
            var queryParams = new List<string>();
            if (spaceId.HasValue) queryParams.Add($"space_id={spaceId.Value}");
            queryParams.Add($"limit={limit}");
            queryParams.Add($"offset={offset}");
            if (queryParams.Any()) url += "?" + string.Join("&", queryParams);
            return await GetAsync<List<ChatMessageResponse>>(url);
        }

        public async Task<FriendshipResponse> SendFriendRequestAsync(int friendId)
        {
            var request = new SendFriendRequestRequest { FriendId = friendId };
            return await PostAsync<FriendshipResponse>("/v1/social/friends/request", request);
        }

        public async Task<FriendshipResponse> AcceptFriendRequestAsync(int friendId)
        {
            return await PostAsync<FriendshipResponse>($"/v1/social/friends/{friendId}/accept", null);
        }

        public async Task<List<FriendshipResponse>> GetFriendsAsync()
        {
            return await GetAsync<List<FriendshipResponse>>("/v1/social/friends");
        }

        public async Task<List<FriendshipResponse>> GetFriendRequestsAsync()
        {
            return await GetAsync<List<FriendshipResponse>>("/v1/social/friends/requests");
        }

        public async Task<FriendshipResponse> BlockUserAsync(int userId)
        {
            return await PostAsync<FriendshipResponse>($"/v1/social/friends/{userId}/block", null);
        }

        public async Task DeleteFriendAsync(int friendId)
        {
            await DeleteAsync($"/v1/social/friends/{friendId}");
        }

        public async Task<MuteResponse> MuteUserAsync(MuteUserRequest request)
        {
            return await PostAsync<MuteResponse>("/v1/moderation/mute", request);
        }

        public async Task<MuteCheckResponse> CheckMuteAsync(int userId, int? spaceId = null)
        {
            string url = $"/v1/moderation/mute/{userId}/check";
            if (spaceId.HasValue) url += $"?space_id={spaceId.Value}";
            return await GetAsync<MuteCheckResponse>(url);
        }

        public async Task UnmuteUserAsync(int userId, int? spaceId = null)
        {
            string url = $"/v1/moderation/mute/{userId}";
            if (spaceId.HasValue) url += $"?space_id={spaceId.Value}";
            await DeleteAsync(url);
        }

        private async Task<T> GetAsync<T>(string endpoint)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _baseUrl + endpoint);
            return await SendRequestAsync<T>(request);
        }

        private async Task<T> PostAsync<T>(string endpoint, object? body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + endpoint);
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return await SendRequestAsync<T>(request);
        }

        private async Task<T> PatchAsync<T>(string endpoint, object body)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), _baseUrl + endpoint);
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendRequestAsync<T>(request);
        }

        private async Task DeleteAsync(string endpoint)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, _baseUrl + endpoint);
            await SendRequestAsync<object>(request);
        }

        private async Task<T> SendRequestAsync<T>(HttpRequestMessage request)
        {
            if (ClientConfiguration.Instance.EnableBackendDebugLog)
            {
                string logMessage = $"[Backend API] {request.Method} {request.RequestUri}";
                DebugLog?.Invoke(this, logMessage);

                if (request.Content != null)
                {
                    string requestBody = await request.Content.ReadAsStringAsync();
                    DebugLog?.Invoke(this, $"[Backend API] Request: {requestBody}");
                }
            }

            var response = await _httpClient.SendAsync(request);

            string responseContent = await response.Content.ReadAsStringAsync();

            if (ClientConfiguration.Instance.EnableBackendDebugLog)
            {
                DebugLog?.Invoke(this, $"[Backend API] Response ({(int)response.StatusCode}): {responseContent}");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new BackendApiException(response.StatusCode, responseContent);
            }

            return JsonSerializer.Deserialize<T>(responseContent)!;
        }
    }
}
