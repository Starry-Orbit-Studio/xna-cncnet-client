#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// Domain-Action 协议客户端接口
    /// </summary>
    public interface IDomainActionClient : IDisposable
    {
        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 当前会话ID
        /// </summary>
        string? SessionId { get; }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="ticket">WebSocket连接票据</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task ConnectAsync(string ticket, CancellationToken cancellationToken = default);

        /// <summary>
        /// 断开连接
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// 发送 Domain-Action 消息
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SendAsync(DomainActionMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送指定领域和动作的消息
        /// </summary>
        /// <param name="domain">业务领域</param>
        /// <param name="action">动作</param>
        /// <param name="payload">载荷数据</param>
        /// <param name="targetId">目标ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SendAsync(string domain, string action, object? payload = null, string? targetId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送消息并等待响应
        /// </summary>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="message">消息对象</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应数据</returns>
        Task<TResponse?> SendWithResponseAsync<TResponse>(DomainActionMessage message, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        // ====== 事件 ======

        /// <summary>
        /// 收到消息时触发
        /// </summary>
        event EventHandler<DomainActionMessage>? MessageReceived;

        /// <summary>
        /// 连接成功时触发
        /// </summary>
        event EventHandler? Connected;

        /// <summary>
        /// 断开连接时触发
        /// </summary>
        event EventHandler<string>? Disconnected;

        /// <summary>
        /// 发生错误时触发
        /// </summary>
        event EventHandler<Exception>? ErrorOccurred;

        // ====== 心跳管理 ======

        /// <summary>
        /// 启用或禁用心跳
        /// </summary>
        /// <param name="enabled">是否启用</param>
        void SetHeartbeatEnabled(bool enabled);

        /// <summary>
        /// 设置心跳间隔
        /// </summary>
        /// <param name="interval">心跳间隔</param>
        void SetHeartbeatInterval(TimeSpan interval);

        // ====== 状态管理 ======

        /// <summary>
        /// 获取连接统计信息
        /// </summary>
        ConnectionStatistics GetStatistics();

        /// <summary>
        /// 重置统计信息
        /// </summary>
        void ResetStatistics();
    }

    /// <summary>
    /// 连接统计信息
    /// </summary>
    public class ConnectionStatistics
    {
        /// <summary>
        /// 已发送消息数
        /// </summary>
        public long MessagesSent { get; set; }

        /// <summary>
        /// 已接收消息数
        /// </summary>
        public long MessagesReceived { get; set; }

        /// <summary>
        /// 发送失败的消息数
        /// </summary>
        public long MessagesFailed { get; set; }

        /// <summary>
        /// 心跳发送次数
        /// </summary>
        public long HeartbeatsSent { get; set; }

        /// <summary>
        /// 心跳响应次数
        /// </summary>
        public long HeartbeatsReceived { get; set; }

        /// <summary>
        /// 连接持续时间
        /// </summary>
        public TimeSpan ConnectionDuration { get; set; }

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastActivityTime { get; set; }

        /// <summary>
        /// 平均往返时间（毫秒）
        /// </summary>
        public double AverageRoundTripTime { get; set; }

        /// <summary>
        /// 消息成功率
        /// </summary>
        public double SuccessRate => MessagesSent > 0 ? (double)(MessagesSent - MessagesFailed) / MessagesSent * 100 : 100;
    }
}