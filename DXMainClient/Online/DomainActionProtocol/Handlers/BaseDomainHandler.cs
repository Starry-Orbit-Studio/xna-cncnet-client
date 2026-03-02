#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// Domain-Action 处理器基类
    /// </summary>
    public abstract class BaseDomainHandler : IDomainActionHandler
    {
        /// <summary>
        /// 处理器负责的领域
        /// </summary>
        public abstract string Domain { get; }
        
        /// <summary>
        /// 支持的动作集合
        /// </summary>
        protected abstract HashSet<string> SupportedActions { get; }
        
        /// <summary>
        /// 检查是否能处理指定动作
        /// </summary>
        public virtual bool CanHandle(string action)
        {
            return SupportedActions.Contains(action);
        }
        
        /// <summary>
        /// 处理消息
        /// </summary>
        public abstract Task HandleAsync(DomainActionMessage message);
        
        /// <summary>
        /// 记录处理开始
        /// </summary>
        protected void LogHandlingStart(DomainActionMessage message)
        {
            Logger.Log($"[{GetType().Name}] Handling {message.Domain}:{message.Action} (MessageId: {message.MessageId})");
        }
        
        /// <summary>
        /// 记录处理完成
        /// </summary>
        protected void LogHandlingComplete(DomainActionMessage message)
        {
            Logger.Log($"[{GetType().Name}] Completed handling {message.Domain}:{message.Action}");
        }
        
        /// <summary>
        /// 记录处理错误
        /// </summary>
        protected void LogHandlingError(DomainActionMessage message, Exception ex)
        {
            Logger.Log($"[{GetType().Name}] Error handling {message.Domain}:{message.Action}: {ex.Message}");
        }
        
        /// <summary>
        /// 安全的从Payload获取数据，返回默认值如果失败
        /// </summary>
        protected T? GetPayloadSafely<T>(DomainActionMessage message, T? defaultValue = default)
        {
            try
            {
                if (message.TryGetPayload<T>(out var value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[{GetType().Name}] Failed to deserialize payload for {message.Domain}:{message.Action}: {ex.Message}");
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// 验证消息是否有效
        /// </summary>
        protected bool ValidateMessage(DomainActionMessage message)
        {
            if (!message.IsValid())
            {
                Logger.Log($"[{GetType().Name}] Received invalid message: Domain={message.Domain}, Action={message.Action}");
                return false;
            }
            
            if (!CanHandle(message.Action))
            {
                Logger.Log($"[{GetType().Name}] Cannot handle action {message.Action} for domain {message.Domain}");
                return false;
            }
            
            return true;
        }
    }
}