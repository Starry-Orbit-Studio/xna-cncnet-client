#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DTAClient.Online.DomainAction.Payloads
{
    /// <summary>
    /// 创建房间请求载荷
    /// </summary>
    public class CreateRoomRequest
    {
        /// <summary>
        /// 房间名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 地图显示名称
        /// </summary>
        [JsonPropertyName("map_name")]
        public string MapName { get; set; } = string.Empty;

        /// <summary>
        /// 地图文件唯一 Hash（MD5）
        /// </summary>
        [JsonPropertyName("map_hash")]
        public string MapHash { get; set; } = string.Empty;

        /// <summary>
        /// 最大玩家数（2-8）
        /// </summary>
        [JsonPropertyName("max_players")]
        public int MaxPlayers { get; set; } = 8;

        /// <summary>
        /// 是否为私有房间
        /// </summary>
        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        /// <summary>
        /// 房间密码（如果为私有房间）
        /// </summary>
        [JsonPropertyName("password")]
        public string? Password { get; set; }

        /// <summary>
        /// 游戏类型/版本
        /// </summary>
        [JsonPropertyName("game_type")]
        public string? GameType { get; set; }

        /// <summary>
        /// 游戏模式
        /// </summary>
        [JsonPropertyName("game_mode")]
        public string? GameMode { get; set; }
    }

    /// <summary>
    /// 加入房间请求载荷
    /// </summary>
    public class JoinRoomRequest
    {
        /// <summary>
        /// 房间密码（如果需要）
        /// </summary>
        [JsonPropertyName("password")]
        public string? Password { get; set; }

        /// <summary>
        /// 客户端地图Hash（用于预校验）
        /// </summary>
        [JsonPropertyName("client_map_hash")]
        public string? ClientMapHash { get; set; }
    }

    /// <summary>
    /// 房间设置
    /// </summary>
    public class RoomSettings
    {
        /// <summary>
        /// 房间名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 地图显示名称
        /// </summary>
        [JsonPropertyName("map_name")]
        public string MapName { get; set; } = string.Empty;

        /// <summary>
        /// 地图文件Hash
        /// </summary>
        [JsonPropertyName("map_hash")]
        public string MapHash { get; set; } = string.Empty;

        /// <summary>
        /// 最大玩家数
        /// </summary>
        [JsonPropertyName("max_players")]
        public int MaxPlayers { get; set; } = 8;

        /// <summary>
        /// 房主会话ID
        /// </summary>
        [JsonPropertyName("owner_session_id")]
        public string OwnerSessionId { get; set; } = string.Empty;

        /// <summary>
        /// 是否为私有房间
        /// </summary>
        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        /// <summary>
        /// 游戏类型/版本
        /// </summary>
        [JsonPropertyName("game_type")]
        public string GameType { get; set; } = string.Empty;

        /// <summary>
        /// 游戏模式
        /// </summary>
        [JsonPropertyName("game_mode")]
        public string GameMode { get; set; } = "Standard";

        /// <summary>
        /// 房间状态：waiting, starting, in_game
        /// </summary>
        [JsonPropertyName("room_status")]
        public string RoomStatus { get; set; } = "waiting";

        /// <summary>
        /// 创建时间戳
        /// </summary>
        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        /// <summary>
        /// 更新时间戳
        /// </summary>
        [JsonPropertyName("updated_at")]
        public long UpdatedAt { get; set; }
    }

    /// <summary>
    /// 房间成员信息
    /// </summary>
    public class RoomMember
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID
        /// </summary>
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 用户昵称
        /// </summary>
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// 是否为房主
        /// </summary>
        [JsonPropertyName("is_owner")]
        public bool IsOwner { get; set; }

        /// <summary>
        /// 是否准备就绪
        /// </summary>
        [JsonPropertyName("is_ready")]
        public bool IsReady { get; set; }

        /// <summary>
        /// 用户头像URL
        /// </summary>
        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        /// <summary>
        /// 用户等级
        /// </summary>
        [JsonPropertyName("level")]
        public int Level { get; set; }

        /// <summary>
        /// 战队标签
        /// </summary>
        [JsonPropertyName("clan_tag")]
        public string? ClanTag { get; set; }

        /// <summary>
        /// 是否为访客
        /// </summary>
        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        /// <summary>
        /// 连接状态：online, ghost, offline
        /// </summary>
        [JsonPropertyName("connection_status")]
        public string ConnectionStatus { get; set; } = "online";

        /// <summary>
        /// 加入时间戳
        /// </summary>
        [JsonPropertyName("joined_at")]
        public long JoinedAt { get; set; }
    }

    /// <summary>
    /// 房间全量同步载荷
    /// </summary>
    public class RoomSyncPayload
    {
        /// <summary>
        /// 房间ID
        /// </summary>
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = string.Empty;

        /// <summary>
        /// 房间设置
        /// </summary>
        [JsonPropertyName("settings")]
        public RoomSettings Settings { get; set; } = new();

        /// <summary>
        /// 房间成员列表
        /// </summary>
        [JsonPropertyName("members")]
        public List<RoomMember> Members { get; set; } = new();

        /// <summary>
        /// 同步原因：member_joined, member_left, ready_changed, settings_updated
        /// </summary>
        [JsonPropertyName("sync_reason")]
        public string? SyncReason { get; set; }

        /// <summary>
        /// 触发同步的会话ID（可选）
        /// </summary>
        [JsonPropertyName("triggered_by")]
        public string? TriggeredBy { get; set; }

        /// <summary>
        /// 同步时间戳
        /// </summary>
        [JsonPropertyName("sync_timestamp")]
        public long SyncTimestamp { get; set; }
    }

    /// <summary>
    /// 设置准备状态请求载荷
    /// </summary>
    public class SetReadyRequest
    {
        /// <summary>
        /// 是否准备
        /// </summary>
        [JsonPropertyName("is_ready")]
        public bool IsReady { get; set; }

        /// <summary>
        /// 客户端地图Hash（必须与房主一致）
        /// </summary>
        [JsonPropertyName("client_map_hash")]
        public string ClientMapHash { get; set; } = string.Empty;

        /// <summary>
        /// 客户端游戏版本
        /// </summary>
        [JsonPropertyName("client_version")]
        public string? ClientVersion { get; set; }
    }

    /// <summary>
    /// 设置准备状态广播载荷（服务器发送）
    /// </summary>
    public class SetReadyPayload
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        [JsonPropertyName("player_id")]
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>
        /// 是否准备
        /// </summary>
        [JsonPropertyName("is_ready")]
        public bool IsReady { get; set; }

        /// <summary>
        /// 房间ID
        /// </summary>
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// 开始游戏请求载荷
    /// </summary>
    public class StartGameRequest
    {
        /// <summary>
        /// 强制开始（忽略未准备玩家）
        /// </summary>
        [JsonPropertyName("force_start")]
        public bool ForceStart { get; set; }

        /// <summary>
        /// 游戏启动参数
        /// </summary>
        [JsonPropertyName("launch_args")]
        public string? LaunchArgs { get; set; }
    }

    /// <summary>
    /// 游戏开始中响应载荷
    /// </summary>
    public class GameStartingPayload
    {
        /// <summary>
        /// 隧道服务器IP地址
        /// </summary>
        [JsonPropertyName("tunnel_ip")]
        public string TunnelIp { get; set; } = string.Empty;

        /// <summary>
        /// 隧道服务器端口
        /// </summary>
        [JsonPropertyName("tunnel_port")]
        public int TunnelPort { get; set; }

        /// <summary>
        /// 连接令牌
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 房间ID（用于游戏内识别）
        /// </summary>
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = string.Empty;

        /// <summary>
        /// 游戏启动超时时间（秒）
        /// </summary>
        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 游戏启动参数
        /// </summary>
        [JsonPropertyName("launch_args")]
        public string? LaunchArgs { get; set; }

        /// <summary>
        /// 服务器时间戳
        /// </summary>
        [JsonPropertyName("server_time")]
        public long ServerTime { get; set; }
    }

    /// <summary>
    /// 房间创建成功响应
    /// </summary>
    public class RoomCreatedResponse
    {
        /// <summary>
        /// 房间ID
        /// </summary>
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = string.Empty;

        /// <summary>
        /// 房间设置
        /// </summary>
        [JsonPropertyName("settings")]
        public RoomSettings Settings { get; set; } = new();

        /// <summary>
        /// 创建时间戳
        /// </summary>
        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }
    }

    /// <summary>
    /// 房间更新通知
    /// </summary>
    public class RoomUpdatedPayload
    {
        /// <summary>
        /// 房间ID
        /// </summary>
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = string.Empty;

        /// <summary>
        /// 更新的设置（部分更新）
        /// </summary>
        [JsonPropertyName("updated_settings")]
        public Dictionary<string, object>? UpdatedSettings { get; set; }

        /// <summary>
        /// 更新的成员状态
        /// </summary>
        [JsonPropertyName("updated_members")]
        public List<RoomMember>? UpdatedMembers { get; set; }

        /// <summary>
        /// 更新原因
        /// </summary>
        [JsonPropertyName("update_reason")]
        public string UpdateReason { get; set; } = string.Empty;

        /// <summary>
        /// 更新时间戳
        /// </summary>
        [JsonPropertyName("updated_at")]
        public long UpdatedAt { get; set; }
    }
}