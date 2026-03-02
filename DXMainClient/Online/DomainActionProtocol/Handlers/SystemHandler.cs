#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;
using DTAClient.Online.DomainAction.Payloads;

namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// SYSTEM 域消息处理器
    /// </summary>
    public class SystemHandler : BaseDomainHandler
    {
        private readonly HashSet<string> _supportedActions = new HashSet<string>
        {
            Actions.CONNECTED,
            Actions.HEARTBEAT,
            Actions.PONG,
            Actions.ERROR
        };
        
        /// <summary>
        /// 连接成功事件
        /// </summary>
        public event EventHandler<ConnectedPayload>? Connected;
        
        /// <summary>
        /// 心跳响应事件
        /// </summary>
        public event EventHandler<PongPayload>? PongReceived;
        
        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<ErrorPayload>? ErrorReceived;
        
        /// <summary>
        /// 处理器负责的领域
        /// </summary>
        public override string Domain => Domains.SYSTEM;
        
        /// <summary>
        /// 支持的动作集合
        /// </summary>
        protected override HashSet<string> SupportedActions => _supportedActions;
        
        /// <summary>
        /// 处理消息
        /// </summary>
        public override async Task HandleAsync(DomainActionMessage message)
        {
            if (!ValidateMessage(message))
                return;
                
            LogHandlingStart(message);
            
            try
            {
                switch (message.Action)
                {
                    case Actions.CONNECTED:
                        await HandleConnectedAsync(message);
                        break;
                        
                    case Actions.HEARTBEAT:
                        await HandleHeartbeatAsync(message);
                        break;
                        
                    case Actions.PONG:
                        await HandlePongAsync(message);
                        break;
                        
                    case Actions.ERROR:
                        await HandleErrorAsync(message);
                        break;
                        
                    default:
                        Logger.Log($"[SystemHandler] Unhandled action: {message.Action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogHandlingError(message, ex);
                throw;
            }
            finally
            {
                LogHandlingComplete(message);
            }
        }
        
        /// <summary>
        /// 处理连接成功消息
        /// </summary>
        private async Task HandleConnectedAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<ConnectedPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[SystemHandler] Connected with session_id: {payload.SessionId}, user_id: {payload.UserId}, is_guest: {payload.IsGuest}");
                
                if (Connected != null)
                {
                    await Task.Run(() => Connected?.Invoke(this, payload));
                }
            }
            else
            {
                Logger.Log("[SystemHandler] Connected message missing or invalid payload");
            }
        }
        
        /// <summary>
        /// 处理心跳消息
        /// </summary>
        private async Task HandleHeartbeatAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<HeartbeatPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[SystemHandler] Heartbeat received with timestamp: {payload.Timestamp}");
                
                // 响应PONG消息
                var pongResponse = new DomainActionMessage
                {
                    Domain = Domains.SYSTEM,
                    Action = Actions.PONG,
                    TargetId = message.MessageId,
                    Payload = System.Text.Json.JsonSerializer.SerializeToElement(new PongPayload
                    {
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        ReceivedAt = payload.Timestamp
                    })
                };
                
                // 这里可以发送响应，但通常心跳由客户端主动发送，服务端响应PONG
                // 实际实现中，SystemHandler可能不直接发送消息，而是通过其他机制
            }
        }
        
        /// <summary>
        /// 处理PONG响应消息
        /// </summary>
        private async Task HandlePongAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<PongPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[SystemHandler] Pong received with processing_delay: {payload.ProcessingDelay}ms");
                
                if (PongReceived != null)
                {
                    await Task.Run(() => PongReceived?.Invoke(this, payload));
                }
            }
        }
        
        /// <summary>
        /// 处理错误消息
        /// </summary>
        private async Task HandleErrorAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<ErrorPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[SystemHandler] Error received: Code={payload.Code}, Reason={payload.Reason}, OriginalAction={payload.OriginalAction}");
                
                if (ErrorReceived != null)
                {
                    await Task.Run(() => ErrorReceived?.Invoke(this, payload));
                }
            }
            else
            {
                Logger.Log("[SystemHandler] Error message missing or invalid payload");
            }
        }
    }
}