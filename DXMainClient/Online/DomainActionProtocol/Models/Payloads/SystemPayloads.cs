#nullable enable
using System.Text.Json.Serialization;

namespace DTAClient.Online.DomainAction.Payloads
{
    /// <summary>
    /// SYSTEM 域 Connected 动作的载荷
    /// </summary>
    public class ConnectedPayload
    {
        /// <summary>
        /// 连接会话ID
        /// </summary>
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID（如果已登录）
        /// </summary>
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        /// <summary>
        /// 是否为访客
        /// </summary>
        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        /// <summary>
        /// 访客名称（如果是访客）
        /// </summary>
        [JsonPropertyName("guest_name")]
        public string? GuestName { get; set; }

        /// <summary>
        /// 服务端时间戳
        /// </summary>
        [JsonPropertyName("server_time")]
        public long? ServerTime { get; set; }
    }

    /// <summary>
    /// SYSTEM 域 Heartbeat 动作的载荷
    /// </summary>
    public class HeartbeatPayload
    {
        /// <summary>
        /// 心跳时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>
        /// 客户端本地时间
        /// </summary>
        [JsonPropertyName("client_time")]
        public long? ClientTime { get; set; }
    }

    /// <summary>
    /// SYSTEM 域 Pong 动作的载荷
    /// </summary>
    public class PongPayload
    {
        /// <summary>
        /// 响应时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>
        /// 服务端接收时间戳
        /// </summary>
        [JsonPropertyName("received_at")]
        public long? ReceivedAt { get; set; }

        /// <summary>
        /// 服务端处理延迟（毫秒）
        /// </summary>
        [JsonPropertyName("processing_delay")]
        public int? ProcessingDelay { get; set; }
    }

    /// <summary>
    /// SYSTEM 域 Error 动作的载荷
    /// </summary>
    public class ErrorPayload
    {
        /// <summary>
        /// 错误码
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// 错误原因描述
        /// </summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 导致错误的原始动作
        /// </summary>
        [JsonPropertyName("original_action")]
        public string OriginalAction { get; set; } = string.Empty;

        /// <summary>
        /// 错误详情（可选）
        /// </summary>
        [JsonPropertyName("details")]
        public string? Details { get; set; }

        /// <summary>
        /// 建议的重试时间（秒）
        /// </summary>
        [JsonPropertyName("retry_after")]
        public int? RetryAfter { get; set; }

        /// <summary>
        /// 获取错误描述
        /// </summary>
        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(Details))
                return $"{Reason} ({Details})";
            return Reason;
        }
    }
}