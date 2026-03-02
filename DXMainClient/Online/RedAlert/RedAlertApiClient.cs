#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;
using DTAClient.Online.SharedModels;

namespace DTAClient.Online.RedAlert
{
    /// <summary>
    /// 新后端API客户端（适配 Domain-Action 协议的后端）
    /// 参考文档：https://api.es.ra2modol.com/docs
    /// </summary>
    public class RedAlertApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ModelMapper _modelMapper;
        private string? _accessToken;
        private string? _sessionId;

        public event EventHandler<string>? DebugLog;

        public RedAlertApiClient(string baseUrl, HttpClient httpClient)
        {
            _baseUrl = baseUrl;
            _httpClient = httpClient;
            _modelMapper = new ModelMapper();
        }

        public string? AccessToken => _accessToken;
        public string? SessionId => _sessionId;
        public string BaseUrl => _baseUrl;

        #region 认证相关

        /// <summary>
        /// 启动 OAuth2 认证流程
        /// </summary>
        /// <param name="provider">认证提供商（google, github 等）</param>
        /// <returns>重定向URL</returns>
        public async Task<OAuthStartResponse> StartOAuthAsync(string provider)
        {
            return await GetAsync<OAuthStartResponse>($"/auth/start/{provider}");
        }

        /// <summary>
        /// 游客登录
        /// </summary>
        public async Task<UnifiedAuthTokenResponse> LoginAsGuestAsync(GuestLoginRequest request)
        {
            var restAuth = await PostAsync<AuthTokenResponse>("/auth/login/guest", request);
            return _modelMapper.MapFromRestApi(restAuth);
        }

        /// <summary>
        /// OAuth2 登录（使用授权码）
        /// </summary>
        public async Task<UnifiedAuthTokenResponse> LoginWithOAuthAsync(string provider, string code, string state)
        {
            var request = new OAuthLoginRequest { Code = code, State = state };
            var restAuth = await PostAsync<AuthTokenResponse>($"/auth/login/{provider}", request);
            return _modelMapper.MapFromRestApi(restAuth);
        }

        /// <summary>
        /// 获取 WebSocket 连接票据
        /// </summary>
        public async Task<UnifiedConnectTicketResponse> GetConnectTicketAsync()
        {
            var restTicket = await PostAsync<ConnectTicketResponse>("/sessions/connect", null);
            _sessionId = restTicket.SessionId;
            return _modelMapper.MapFromRestApi(restTicket);
        }

        /// <summary>
        /// 获取会话信息
        /// </summary>
        public async Task<SessionResponse> GetSessionInfoAsync(string? sessionId = null)
        {
            string id = sessionId ?? _sessionId;
            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException("Session ID is not set");
            return await GetAsync<SessionResponse>($"/sessions/{id}");
        }

        /// <summary>
        /// 设置访问令牌（Bearer Token）
        /// </summary>
        public void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        /// <summary>
        /// 删除会话
        /// </summary>
        public async Task DeleteSessionAsync()
        {
            if (string.IsNullOrEmpty(_sessionId))
                throw new InvalidOperationException("Session ID is not set");
            await DeleteAsync($"/sessions/{_sessionId}");
            _sessionId = null;
        }

        #endregion

        #region 频道相关（替代原有的 Space）

        /// <summary>
        /// 获取频道列表
        /// </summary>
        public async Task<List<UnifiedChannelInfo>> GetChannelsAsync()
        {
            var restChannels = await GetAsync<List<ChannelResponse>>("/channels/list");
            return _modelMapper.MapFromRestApi(restChannels);
        }

        /// <summary>
        /// 获取频道中的房间列表
        /// </summary>
        public async Task<List<UnifiedRoomSyncEvent>> GetChannelRoomsAsync(string channelId)
        {
            var restRooms = await GetAsync<List<RoomSyncEvent>>($"/channels/{channelId}/rooms");
            return restRooms.ConvertAll(_modelMapper.MapFromRestApi);
        }

        /// <summary>
        /// 获取频道详情
        /// </summary>
        public async Task<UnifiedChannelInfo> GetChannelAsync(string channelId)
        {
            var restChannel = await GetAsync<ChannelResponse>($"/channels/{channelId}");
            return _modelMapper.MapFromRestApi(restChannel);
        }

        #endregion

        #region 房间相关

        /// <summary>
        /// 获取房间详情（包含房间设置和成员信息）
        /// </summary>
        public async Task<UnifiedRoomSyncEvent> GetRoomAsync(string roomId)
        {
            var restRoom = await GetAsync<RoomSyncEvent>($"/rooms/{roomId}");
            return _modelMapper.MapFromRestApi(restRoom);
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        public async Task<UnifiedRoomSyncEvent> CreateRoomAsync(CreateRoomRequest request)
        {
            var restRoom = await PostAsync<RoomSyncEvent>("/rooms", request);
            return _modelMapper.MapFromRestApi(restRoom);
        }

        /// <summary>
        /// 更新房间
        /// </summary>
        public async Task<UnifiedRoomSyncEvent> UpdateRoomAsync(string roomId, UpdateRoomRequest request)
        {
            var restRoom = await PatchAsync<RoomSyncEvent>($"/rooms/{roomId}", request);
            return _modelMapper.MapFromRestApi(restRoom);
        }

        /// <summary>
        /// 删除房间
        /// </summary>
        public async Task DeleteRoomAsync(string roomId)
        {
            await DeleteAsync($"/rooms/{roomId}");
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        public async Task JoinRoomAsync(string roomId)
        {
            await PostAsync<object>($"/rooms/{roomId}/join", null);
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        public async Task LeaveRoomAsync(string roomId)
        {
            await PostAsync<object>($"/rooms/{roomId}/leave", null);
        }

        #endregion

        #region 用户相关

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        public async Task<UnifiedUserFullCard> GetCurrentUserAsync()
        {
            var response = await GetAsync<UserMeResponse>("/users/me");
            
            // 直接从 UserMeResponse 映射到 UnifiedUserFullCard
            return new UnifiedUserFullCard
            {
                Id = response.Card.Profile.UserId.ToString(),
                UserId = response.Card.Profile.UserId.ToString(),
                Nickname = response.Card.Profile.Nickname,
                Avatar = response.Card.Profile.Avatar,
                Level = response.Card.Profile.Level,
                ClanTag = response.Card.Profile.ClanTag,
                IsOnline = response.Card.Status.Equals("ONLINE", StringComparison.OrdinalIgnoreCase),
                Status = response.Card.Status,
                IsGuest = response.Card.Profile.Role.Equals("guest", StringComparison.OrdinalIgnoreCase),
                // 其他字段设为默认值
                Username = response.Card.Profile.Nickname, // 使用昵称作为用户名
                DisplayName = response.Card.Profile.Nickname,
                AvatarUrl = response.Card.Profile.Avatar
            };
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<List<UnifiedUserFullCard>> SearchUsersAsync(string query)
        {
            var restUsers = await GetAsync<List<UserFullCard>>($"/users/search?q={Uri.EscapeDataString(query)}");
            return restUsers.ConvertAll(_modelMapper.MapFromRestApi);
        }

        /// <summary>
        /// 获取用户详情
        /// </summary>
        public async Task<UnifiedUserFullCard> GetUserAsync(string userId)
        {
            var restUser = await GetAsync<UserFullCard>($"/users/{userId}");
            return _modelMapper.MapFromRestApi(restUser);
        }

        /// <summary>
        /// 获取在线用户列表
        /// </summary>
        public async Task<OnlineUsersResponse> GetOnlineUsersAsync()
        {
            return await GetAsync<OnlineUsersResponse>("/presence/online-users");
        }

        #endregion

        #region 社交功能

        /// <summary>
        /// 发送好友请求
        /// </summary>
        public async Task<FriendshipResponse> SendFriendRequestAsync(string friendId)
        {
            var request = new SendFriendRequestRequest { FriendId = friendId };
            return await PostAsync<FriendshipResponse>("/social/friends/request", request);
        }

        /// <summary>
        /// 获取好友列表
        /// </summary>
        public async Task<List<FriendshipResponse>> GetFriendsAsync()
        {
            return await GetAsync<List<FriendshipResponse>>("/social/friends");
        }

        /// <summary>
        /// 获取好友请求列表
        /// </summary>
        public async Task<List<FriendshipResponse>> GetFriendRequestsAsync()
        {
            return await GetAsync<List<FriendshipResponse>>("/social/friends/requests");
        }

        #endregion

        #region 私有HTTP方法

        private string BuildUrl(string endpoint)
        {
            try
            {
                // 确保基础URL不以斜杠结尾，端点不以斜杠开头
                string baseUrl = _baseUrl.TrimEnd('/');
                string path = endpoint.TrimStart('/');
                
                // 检查并移除重复的路径段（例如基础URL已包含/api/v1，端点也以api/v1开头）
                var baseUri = new Uri(baseUrl);
                string basePath = baseUri.AbsolutePath.Trim('/');
                
                if (!string.IsNullOrEmpty(basePath) && path.StartsWith(basePath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    // 移除重复的路径段
                    path = path.Substring(basePath.Length + 1); // +1 for the slash
                }
                else if (path.Equals(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    // 端点路径与基础路径完全相同
                    path = string.Empty;
                }
                
                // 构建最终URL
                string result;
                if (string.IsNullOrEmpty(path))
                {
                    result = baseUrl;
                }
                else
                {
                    result = $"{baseUrl}/{path}";
                }
                
                // 记录构建的URL用于调试
                if (ClientConfiguration.Instance.EnableBackendDebugLog)
                {
                    Logger.Log($"[RedAlert API] BuildUrl: endpoint='{endpoint}', baseUrl='{_baseUrl}', result='{result}'");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"[RedAlert API] BuildUrl error: {ex.Message}");
                // 回退到简单拼接
                return $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            }
        }

        private async Task<T> GetAsync<T>(string endpoint)
        {
            var url = BuildUrl(endpoint);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendRequestAsync<T>(request);
        }

        private async Task<T> PostAsync<T>(string endpoint, object? body)
        {
            var url = BuildUrl(endpoint);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return await SendRequestAsync<T>(request);
        }

        private async Task<T> PatchAsync<T>(string endpoint, object body)
        {
            var url = BuildUrl(endpoint);
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendRequestAsync<T>(request);
        }

        private async Task DeleteAsync(string endpoint)
        {
            var url = BuildUrl(endpoint);
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            await SendRequestAsync<object>(request);
        }

        private async Task<T> SendRequestAsync<T>(HttpRequestMessage request)
        {
            // 总是记录请求URL用于调试
            Logger.Log($"[RedAlert API] {request.Method} {request.RequestUri}");
            
            if (ClientConfiguration.Instance.EnableBackendDebugLog)
            {
                string logMessage = $"[RedAlert API] {request.Method} {request.RequestUri}";
                DebugLog?.Invoke(this, logMessage);

                if (request.Content != null)
                {
                    string requestBody = await request.Content.ReadAsStringAsync();
                    DebugLog?.Invoke(this, $"[RedAlert API] Request: {requestBody}");
                }
            }

            var response = await _httpClient.SendAsync(request);

            string responseContent = await response.Content.ReadAsStringAsync();

            // 总是记录错误响应
            if (!response.IsSuccessStatusCode)
            {
                Logger.Log($"[RedAlert API] HTTP error: {(int)response.StatusCode} {response.StatusCode} - {responseContent}");
            }
            else if (ClientConfiguration.Instance.EnableBackendDebugLog)
            {
                DebugLog?.Invoke(this, $"[RedAlert API] Response ({(int)response.StatusCode}): {responseContent}");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new RedAlertApiException(response.StatusCode, responseContent);
            }

            return JsonSerializer.Deserialize<T>(responseContent)!;
        }

        #endregion
    }

    #region 异常类

    public class RedAlertApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public RedAlertApiException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    #endregion

    #region 请求/响应模型

    // OAuth2 启动响应
    public class OAuthStartResponse
    {
        [JsonPropertyName("redirect_url")]
        public string RedirectUrl { get; set; } = string.Empty;
    }

    // OAuth2 登录请求
    public class OAuthLoginRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;
    }

    // 认证令牌响应（与现有系统兼容）
    public class AuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";
    }

    // 游客登录请求（与现有系统兼容）
    public class GuestLoginRequest
    {
        [JsonPropertyName("guest_uid")]
        public string GuestUid { get; set; } = string.Empty;

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

        [JsonPropertyName("hwid_list")]
        public List<string> HwidList { get; set; } = new();
    }

    // 连接票据响应（与现有系统兼容）
    public class ConnectTicketResponse
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("ws_ticket")]
        public string WsTicket { get; set; } = string.Empty;
    }

    // 会话响应（与现有系统兼容）
    public class SessionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        [JsonPropertyName("guest_name")]
        public string? GuestName { get; set; }

        [JsonPropertyName("connected_at")]
        public DateTime ConnectedAt { get; set; }

        [JsonPropertyName("last_seen")]
        public DateTime? LastSeen { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }
    }

    // 频道响应
    public class ChannelResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "lobby";

        [JsonPropertyName("member_count")]
        public int MemberCount { get; set; }

        [JsonPropertyName("room_count")]
        public int RoomCount { get; set; }
    }

    // 房间同步事件（完整房间状态）
    public class RoomSyncEvent
    {
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = string.Empty;

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; } = string.Empty;

        [JsonPropertyName("host_id")]
        public string HostId { get; set; } = string.Empty;

        [JsonPropertyName("settings")]
        public RoomSettings Settings { get; set; } = new();

        [JsonPropertyName("members")]
        public List<RoomMemberInfo> Members { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = "waiting"; // waiting, starting, in_game, finished
    }

    // 房间设置
    public class RoomSettings
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("max_players")]
        public int MaxPlayers { get; set; } = 8;

        [JsonPropertyName("game_mode")]
        public string GameMode { get; set; } = "standard";

        [JsonPropertyName("map_name")]
        public string MapName { get; set; } = string.Empty;

        [JsonPropertyName("map_hash")]
        public string MapHash { get; set; } = string.Empty;

        [JsonPropertyName("game_version")]
        public string GameVersion { get; set; } = string.Empty;

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("allow_spectators")]
        public bool AllowSpectators { get; set; } = true;
    }

    // 房间成员信息
    public class RoomMemberInfo
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("is_ready")]
        public bool IsReady { get; set; }

        [JsonPropertyName("is_host")]
        public bool IsHost { get; set; }

        [JsonPropertyName("slot_index")]
        public int SlotIndex { get; set; }

        [JsonPropertyName("team")]
        public int? Team { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("joined_at")]
        public DateTime JoinedAt { get; set; }

        [JsonPropertyName("ping")]
        public int? Ping { get; set; }
    }

    // 创建房间请求
    public class CreateRoomRequest
    {
        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("max_players")]
        public int MaxPlayers { get; set; } = 8;

        [JsonPropertyName("game_mode")]
        public string GameMode { get; set; } = "standard";

        [JsonPropertyName("map_name")]
        public string MapName { get; set; } = string.Empty;

        [JsonPropertyName("map_hash")]
        public string MapHash { get; set; } = string.Empty;

        [JsonPropertyName("game_version")]
        public string GameVersion { get; set; } = string.Empty;

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("allow_spectators")]
        public bool AllowSpectators { get; set; } = true;
    }

    // 更新房间请求
    public class UpdateRoomRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("max_players")]
        public int? MaxPlayers { get; set; }

        [JsonPropertyName("game_mode")]
        public string? GameMode { get; set; }

        [JsonPropertyName("map_name")]
        public string? MapName { get; set; }

        [JsonPropertyName("map_hash")]
        public string? MapHash { get; set; }

        [JsonPropertyName("is_private")]
        public bool? IsPrivate { get; set; }

        [JsonPropertyName("allow_spectators")]
        public bool? AllowSpectators { get; set; }
    }

    // 用户完整名片
    public class UserFullCard
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        [JsonPropertyName("is_online")]
        public bool IsOnline { get; set; }

        [JsonPropertyName("last_seen")]
        public DateTime? LastSeen { get; set; }

        [JsonPropertyName("game_stats")]
        public UserGameStats GameStats { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    // 用户游戏统计
    public class UserGameStats
    {
        [JsonPropertyName("total_games")]
        public int TotalGames { get; set; }

        [JsonPropertyName("wins")]
        public int Wins { get; set; }

        [JsonPropertyName("losses")]
        public int Losses { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("rank")]
        public string? Rank { get; set; }
    }

    // 在线用户响应（与现有系统兼容）
    public class OnlineUsersResponse
    {
        [JsonPropertyName("users")]
        public List<OnlineUserInfo> Users { get; set; } = new();

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }
    }

    // 在线用户信息（与现有系统兼容）
    public class OnlineUserInfo
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        [JsonPropertyName("current_channel")]
        public string? CurrentChannel { get; set; }

        [JsonPropertyName("current_room")]
        public string? CurrentRoom { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "online"; // online, away, busy, invisible
    }

    // 好友关系响应（与现有系统兼容）
    public class FriendshipResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("friend_id")]
        public string FriendId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "pending"; // pending, accepted, blocked

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    // 发送好友请求请求（与现有系统兼容）
    public class SendFriendRequestRequest
    {
        [JsonPropertyName("friend_id")]
        public string FriendId { get; set; } = string.Empty;
    }

    // 用户个人信息响应（/users/me 端点）
    public class UserMeResponse
    {
        [JsonPropertyName("card")]
        public UserCard Card { get; set; } = new();

        [JsonPropertyName("experience_points")]
        public int ExperiencePoints { get; set; }

        [JsonPropertyName("identities")]
        public List<UserIdentity> Identities { get; set; } = new();
    }

    // 用户卡片信息
    public class UserCard
    {
        [JsonPropertyName("profile")]
        public UserProfile Profile { get; set; } = new();

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("room_id")]
        public string? RoomId { get; set; }
    }

    // 用户个人资料
    public class UserProfile
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; } = string.Empty;

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("clan_tag")]
        public string? ClanTag { get; set; }
    }

    // 用户身份信息
    public class UserIdentity
    {
        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("provider_uid")]
        public string ProviderUid { get; set; } = string.Empty;
    }

    #endregion
}