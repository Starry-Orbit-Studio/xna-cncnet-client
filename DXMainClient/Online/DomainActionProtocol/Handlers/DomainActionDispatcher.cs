#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// Domain-Action 消息分发器
    /// </summary>
    public class DomainActionDispatcher
    {
        private readonly Dictionary<string, IDomainActionHandler> _handlers = new();
        
        /// <summary>
        /// 注册处理器
        /// </summary>
        public void RegisterHandler(IDomainActionHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
                
            _handlers[handler.Domain] = handler;
            Logger.Log($"[DomainActionDispatcher] Registered handler for domain: {handler.Domain}");
        }
        
        /// <summary>
        /// 分发消息到对应的处理器
        /// </summary>
        public async Task DispatchAsync(DomainActionMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
                
            if (string.IsNullOrEmpty(message.Domain))
            {
                Logger.Log("[DomainActionDispatcher] Received message with empty domain");
                return;
            }
            
            if (_handlers.TryGetValue(message.Domain, out var handler))
            {
                if (handler.CanHandle(message.Action))
                {
                    Logger.Log($"[DomainActionDispatcher] Dispatching {message.Domain}:{message.Action} to handler");
                    await handler.HandleAsync(message);
                }
                else
                {
                    Logger.Log($"[DomainActionDispatcher] Handler for domain {message.Domain} cannot handle action {message.Action}");
                }
            }
            else
            {
                Logger.Log($"[DomainActionDispatcher] No handler registered for domain: {message.Domain}");
            }
        }
        
        /// <summary>
        /// 检查是否有处理器能处理指定领域
        /// </summary>
        public bool HasHandlerForDomain(string domain)
        {
            return _handlers.ContainsKey(domain);
        }
        
        /// <summary>
        /// 获取指定领域的处理器
        /// </summary>
        public IDomainActionHandler? GetHandler(string domain)
        {
            _handlers.TryGetValue(domain, out var handler);
            return handler;
        }
    }
}