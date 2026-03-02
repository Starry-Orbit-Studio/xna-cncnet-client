#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// Domain-Action 协议基础消息结构
    /// </summary>
    public class DomainActionMessage
    {
        /// <summary>
        /// 业务领域
        /// </summary>
        [JsonPropertyName("domain")]
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// 具体动作
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 消息唯一标识
        /// </summary>
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 目标标识（如房间ID、频道ID、用户ID等）
        /// </summary>
        [JsonPropertyName("target_id")]
        public string? TargetId { get; set; }

        /// <summary>
        /// 业务数据载荷
        /// </summary>
        [JsonPropertyName("payload")]
        public JsonElement? Payload { get; set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static DomainActionMessage CreateSuccessResponse(string domain, string action, string? targetId = null, object? data = null)
        {
            return new DomainActionMessage
            {
                Domain = domain,
                Action = action,
                TargetId = targetId,
                Payload = data != null ? JsonSerializer.SerializeToElement(data) : null
            };
        }

        /// <summary>
        /// 创建错误响应
        /// </summary>
        public static DomainActionMessage CreateErrorResponse(string domain, string action, int code, string reason, string? targetId = null)
        {
            return new DomainActionMessage
            {
                Domain = domain,
                Action = action,
                TargetId = targetId,
                Payload = JsonSerializer.SerializeToElement(new
                {
                    code,
                    reason,
                    original_action = action
                })
            };
        }

        /// <summary>
        /// 验证消息基本结构
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Domain) && !string.IsNullOrEmpty(Action);
        }

        /// <summary>
        /// 尝试获取Payload为指定类型
        /// </summary>
        public bool TryGetPayload<T>(out T? value)
        {
            value = default;
            if (Payload == null)
                return false;

            try
            {
                value = Payload.Value.Deserialize<T>();
                return value != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}