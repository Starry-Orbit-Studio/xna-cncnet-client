#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DTAClient.Online.DomainAction.Payloads
{
    /// <summary>
    /// 用户完整名片
    /// </summary>
    public class UserFullCard
    {
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
        /// 是否在线
        /// </summary>
        [JsonPropertyName("is_online")]
        public bool IsOnline { get; set; }

        /// <summary>
        /// 是否为访客
        /// </summary>
        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        /// <summary>
        /// 最后活跃时间
        /// </summary>
        [JsonPropertyName("last_seen")]
        public long? LastSeen { get; set; }

        /// <summary>
        /// 当前状态：online, away, busy, invisible
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "online";
    }

    /// <summary>
    /// 加入频道请求载荷
    /// </summary>
    public class JoinChannelRequest
    {
        /// <summary>
        /// 频道ID
        /// </summary>
        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        /// <summary>
        /// 客户端版本
        /// </summary>
        [JsonPropertyName("client_version")]
        public string? ClientVersion { get; set; }
    }

    /// <summary>
    /// 加入频道成功响应
    /// </summary>
    public class JoinChannelResponse
    {
        /// <summary>
        /// 频道ID
        /// </summary>
        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        /// <summary>
        /// 频道名称
        /// </summary>
        [JsonPropertyName("channel_name")]
        public string ChannelName { get; set; } = string.Empty;

        /// <summary>
        /// 当前在线人数
        /// </summary>
        [JsonPropertyName("online_count")]
        public int OnlineCount { get; set; }

        /// <summary>
        /// 频道描述
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 频道欢迎消息
        /// </summary>
        [JsonPropertyName("welcome_message")]
        public string? WelcomeMessage { get; set; }

        /// <summary>
        /// 加入时间戳
        /// </summary>
        [JsonPropertyName("joined_at")]
        public long JoinedAt { get; set; }
    }

    /// <summary>
    /// 成员变动广播载荷
    /// </summary>
    public class MemberChangedPayload
    {
        /// <summary>
        /// 变动动作：JOINED 或 LEFT
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 用户信息
        /// </summary>
        [JsonPropertyName("user")]
        public UserFullCard User { get; set; } = new();

        /// <summary>
        /// 频道ID
        /// </summary>
        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        /// <summary>
        /// 变动时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>
        /// 当前在线人数
        /// </summary>
        [JsonPropertyName("current_online_count")]
        public int CurrentOnlineCount { get; set; }
    }

    /// <summary>
    /// 发送聊天消息请求载荷
    /// </summary>
    public class SendChatRequest
    {
        /// <summary>
        /// 消息内容
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 消息类型：TEXT, SYSTEM, ANNOUNCEMENT
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "TEXT";

        /// <summary>
        /// 目标频道ID（可选，默认为当前频道）
        /// </summary>
        [JsonPropertyName("channel_id")]
        public int? ChannelId { get; set; }

        /// <summary>
        /// 回复的消息ID（用于回复）
        /// </summary>
        [JsonPropertyName("reply_to")]
        public string? ReplyTo { get; set; }

        /// <summary>
        /// 提及的用户ID列表
        /// </summary>
        [JsonPropertyName("mentions")]
        public string[]? Mentions { get; set; }
    }

    /// <summary>
    /// 新聊天消息广播载荷
    /// </summary>
    public class NewChatPayload
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// 发送者信息
        /// </summary>
        [JsonPropertyName("sender")]
        public UserFullCard Sender { get; set; } = new();

        /// <summary>
        /// 消息内容
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 消息类型：TEXT, SYSTEM, ANNOUNCEMENT
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "TEXT";

        /// <summary>
        /// 频道ID
        /// </summary>
        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>
        /// 回复的消息ID（可选）
        /// </summary>
        [JsonPropertyName("reply_to")]
        public string? ReplyTo { get; set; }

        /// <summary>
        /// 提及的用户ID列表
        /// </summary>
        [JsonPropertyName("mentions")]
        public string[]? Mentions { get; set; }

        /// <summary>
        /// 是否为系统消息
        /// </summary>
        public bool IsSystemMessage => Type == "SYSTEM" || Type == "ANNOUNCEMENT";

        /// <summary>
        /// 是否为公告消息
        /// </summary>
        public bool IsAnnouncement => Type == "ANNOUNCEMENT";
    }

    /// <summary>
    /// 聊天消息发送成功响应
    /// </summary>
    public class ChatSentResponse
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// 发送时间戳
        /// </summary>
        [JsonPropertyName("sent_at")]
        public long SentAt { get; set; }

        /// <summary>
        /// 接收者数量
        /// </summary>
        [JsonPropertyName("recipient_count")]
        public int RecipientCount { get; set; }
    }

    /// <summary>
    /// 离开频道请求载荷
    /// </summary>
    public class LeaveChannelRequest
    {
        /// <summary>
        /// 频道ID
        /// </summary>
        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

        /// <summary>
        /// 离开原因
        /// </summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 频道列表响应
    /// </summary>
    public class ChannelListResponse
    {
        /// <summary>
        /// 频道列表
        /// </summary>
        [JsonPropertyName("channels")]
        public List<ChannelInfo> Channels { get; set; } = new();

        /// <summary>
        /// 推荐频道ID
        /// </summary>
        [JsonPropertyName("recommended_channel_id")]
        public int RecommendedChannelId { get; set; }

        /// <summary>
        /// 上次选择的频道ID
        /// </summary>
        [JsonPropertyName("last_selected_channel_id")]
        public int? LastSelectedChannelId { get; set; }
    }

    /// <summary>
    /// 频道信息
    /// </summary>
    public class ChannelInfo
    {
        /// <summary>
        /// 频道ID
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// 频道名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 频道描述
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 当前在线人数
        /// </summary>
        [JsonPropertyName("online_count")]
        public int OnlineCount { get; set; }

        /// <summary>
        /// 最大容量
        /// </summary>
        [JsonPropertyName("max_capacity")]
        public int MaxCapacity { get; set; }

        /// <summary>
        /// 是否为默认频道
        /// </summary>
        [JsonPropertyName("is_default")]
        public bool IsDefault { get; set; }

        /// <summary>
        /// 是否需要密码
        /// </summary>
        [JsonPropertyName("requires_password")]
        public bool RequiresPassword { get; set; }

        /// <summary>
        /// 区域标签（如：华东、华南、国际）
        /// </summary>
        [JsonPropertyName("region_tag")]
        public string? RegionTag { get; set; }

        /// <summary>
        /// 语言标签
        /// </summary>
        [JsonPropertyName("language_tag")]
        public string? LanguageTag { get; set; }
    }
}