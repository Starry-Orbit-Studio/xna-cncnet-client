#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DTAClient.Online.SharedModels
{
    /// <summary>
    /// 统一用户完整名片
    /// 适配 REST API 和 Domain-Action 协议的不同字段命名
    /// </summary>
    public class UnifiedUserFullCard
    {
        // 用户标识字段（兼容不同命名）
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        // 名称字段（兼容不同命名）
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        // 头像字段（兼容不同命名）
        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        // 状态字段
        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        [JsonPropertyName("is_online")]
        public bool IsOnline { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "online";

        // 时间字段（兼容不同类型）
        [JsonPropertyName("last_seen")]
        public DateTime? LastSeen { get; set; }

        [JsonPropertyName("last_seen_timestamp")]
        public long? LastSeenTimestamp { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("created_at_timestamp")]
        public long? CreatedAtTimestamp { get; set; }

        // 游戏相关字段
        [JsonPropertyName("game_stats")]
        public UnifiedUserGameStats? GameStats { get; set; }

        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonPropertyName("clan_tag")]
        public string? ClanTag { get; set; }

        // 辅助方法：统一获取显示名称
        public string GetDisplayName() => DisplayName ?? Username ?? Nickname ?? Id;

        // 辅助方法：统一获取头像URL
        public string? GetAvatarUrl() => AvatarUrl ?? Avatar;

        // 辅助方法：统一获取最后活跃时间
        public DateTime? GetLastSeen()
        {
            if (LastSeen != null) return LastSeen;
            if (LastSeenTimestamp.HasValue) 
                return DateTimeOffset.FromUnixTimeSeconds(LastSeenTimestamp.Value).UtcDateTime;
            return null;
        }

        // 辅助方法：统一获取创建时间
        public DateTime? GetCreatedAt()
        {
            if (CreatedAt != null) return CreatedAt;
            if (CreatedAtTimestamp.HasValue)
                return DateTimeOffset.FromUnixTimeSeconds(CreatedAtTimestamp.Value).UtcDateTime;
            return null;
        }
    }

    /// <summary>
    /// 统一用户游戏统计
    /// </summary>
    public class UnifiedUserGameStats
    {
        [JsonPropertyName("total_games")]
        public int TotalGames { get; set; }

        [JsonPropertyName("wins")]
        public int Wins { get; set; }

        [JsonPropertyName("losses")]
        public int Losses { get; set; }

        [JsonPropertyName("rating")]
        public int? Rating { get; set; }

        [JsonPropertyName("rank")]
        public string? Rank { get; set; }

        [JsonPropertyName("win_rate")]
        public double? WinRate { get; set; }

        public double CalculateWinRate()
        {
            if (TotalGames == 0) return 0;
            return (double)Wins / TotalGames * 100;
        }
    }

    /// <summary>
    /// 统一频道信息
    /// </summary>
    public class UnifiedChannelInfo
    {
        // 频道标识（兼容不同ID类型）
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("channel_id")]
        public string? ChannelIdString { get; set; }

        // 频道基本信息
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // lobby, room

        // 人数统计
        [JsonPropertyName("member_count")]
        public int? MemberCount { get; set; }

        [JsonPropertyName("online_count")]
        public int OnlineCount { get; set; }

        [JsonPropertyName("room_count")]
        public int? RoomCount { get; set; }

        // 容量和状态
        [JsonPropertyName("max_capacity")]
        public int MaxCapacity { get; set; } = 100;

        [JsonPropertyName("is_default")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("requires_password")]
        public bool RequiresPassword { get; set; }

        // 区域和语言
        [JsonPropertyName("region_tag")]
        public string? RegionTag { get; set; }

        [JsonPropertyName("language_tag")]
        public string? LanguageTag { get; set; }

        // 辅助方法：获取频道ID（统一为字符串）
        public string GetChannelId() => ChannelIdString ?? Id.ToString();
    }

    /// <summary>
    /// 统一房间同步事件
    /// </summary>
    public class UnifiedRoomSyncEvent
    {
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = string.Empty;

        [JsonPropertyName("channel_id")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("host_id")]
        public string? HostId { get; set; }

        [JsonPropertyName("settings")]
        public UnifiedRoomSettings Settings { get; set; } = new();

        [JsonPropertyName("members")]
        public List<UnifiedRoomMember> Members { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("created_at_timestamp")]
        public long? CreatedAtTimestamp { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("updated_at_timestamp")]
        public long? UpdatedAtTimestamp { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = "waiting";

        [JsonPropertyName("sync_reason")]
        public string? SyncReason { get; set; }

        [JsonPropertyName("triggered_by")]
        public string? TriggeredBy { get; set; }

        [JsonPropertyName("sync_timestamp")]
        public long? SyncTimestamp { get; set; }

        // 辅助方法：获取创建时间
        public DateTime? GetCreatedAt()
        {
            if (CreatedAt != null) return CreatedAt;
            if (CreatedAtTimestamp.HasValue)
                return DateTimeOffset.FromUnixTimeSeconds(CreatedAtTimestamp.Value).UtcDateTime;
            return null;
        }

        // 辅助方法：获取更新时间
        public DateTime? GetUpdatedAt()
        {
            if (UpdatedAt != null) return UpdatedAt;
            if (UpdatedAtTimestamp.HasValue)
                return DateTimeOffset.FromUnixTimeSeconds(UpdatedAtTimestamp.Value).UtcDateTime;
            return null;
        }

        // 辅助方法：获取同步时间
        public DateTime? GetSyncTime()
        {
            if (SyncTimestamp.HasValue)
                return DateTimeOffset.FromUnixTimeSeconds(SyncTimestamp.Value).UtcDateTime;
            return GetUpdatedAt();
        }
    }

    /// <summary>
    /// 统一房间设置
    /// </summary>
    public class UnifiedRoomSettings
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("max_players")]
        public int MaxPlayers { get; set; } = 8;

        [JsonPropertyName("max_members")]
        public int? MaxMembers { get; set; }

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

        // 辅助方法：获取最大人数
        public int GetMaxPlayers() => MaxMembers ?? MaxPlayers;
    }

    /// <summary>
    /// 统一房间成员
    /// </summary>
    public class UnifiedRoomMember
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("session_id")]
        public string? SessionId { get; set; }

        // 名称字段（兼容不同命名）
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

        // 状态和角色
        [JsonPropertyName("is_ready")]
        public bool IsReady { get; set; }

        [JsonPropertyName("is_host")]
        public bool IsHost { get; set; }

        [JsonPropertyName("is_owner")]
        public bool IsOwner { get; set; }

        // 游戏位置
        [JsonPropertyName("slot_index")]
        public int SlotIndex { get; set; }

        [JsonPropertyName("team")]
        public int? Team { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        // 用户信息
        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonPropertyName("clan_tag")]
        public string? ClanTag { get; set; }

        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        [JsonPropertyName("connection_status")]
        public string? ConnectionStatus { get; set; }

        // 时间字段
        [JsonPropertyName("joined_at")]
        public DateTime? JoinedAt { get; set; }

        [JsonPropertyName("joined_at_timestamp")]
        public long? JoinedAtTimestamp { get; set; }

        [JsonPropertyName("ping")]
        public int? Ping { get; set; }

        // 辅助方法：获取显示名称
        public string GetDisplayName() => Username ?? Nickname ?? UserId;

        // 辅助方法：获取是否为房主
        public bool IsHostOrOwner() => IsHost || IsOwner;

        // 辅助方法：获取加入时间
        public DateTime? GetJoinedAt()
        {
            if (JoinedAt != null) return JoinedAt;
            if (JoinedAtTimestamp.HasValue)
                return DateTimeOffset.FromUnixTimeSeconds(JoinedAtTimestamp.Value).UtcDateTime;
            return null;
        }
    }

    /// <summary>
    /// 统一认证令牌响应
    /// </summary>
    public class UnifiedAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("user")]
        public UnifiedUserFullCard? User { get; set; }

        [JsonPropertyName("session_id")]
        public string? SessionId { get; set; }

        [JsonPropertyName("ws_ticket")]
        public string? WsTicket { get; set; }
    }

    /// <summary>
    /// 统一连接票据响应
    /// </summary>
    public class UnifiedConnectTicketResponse
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("ws_ticket")]
        public string WsTicket { get; set; } = string.Empty;

        [JsonPropertyName("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [JsonPropertyName("expires_at_timestamp")]
        public long? ExpiresAtTimestamp { get; set; }

        [JsonPropertyName("web_socket_url")]
        public string? WebSocketUrl { get; set; }

        // 辅助方法：获取过期时间
        public DateTime? GetExpiresAt()
        {
            if (ExpiresAt != null) return ExpiresAt;
            if (ExpiresAtTimestamp.HasValue)
                return DateTimeOffset.FromUnixTimeSeconds(ExpiresAtTimestamp.Value).UtcDateTime;
            return null;
        }
    }
}