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
    /// CHANNEL 域消息处理器
    /// </summary>
    public class ChannelHandler : BaseDomainHandler
    {
        private readonly HashSet<string> _supportedActions = new HashSet<string>
        {
            Actions.JOIN_CHANNEL,
            Actions.MEMBER_CHANGED,
            Actions.SEND_CHAT,
            Actions.NEW_CHAT,
            Actions.USER_FULL_CARD
        };
        
        /// <summary>
        /// 加入频道成功事件
        /// </summary>
        public event EventHandler<JoinChannelResponse>? ChannelJoined;
        
        /// <summary>
        /// 频道成员变更事件
        /// </summary>
        public event EventHandler<MemberChangedPayload>? MemberChanged;
        
        /// <summary>
        /// 新聊天消息事件
        /// </summary>
        public event EventHandler<NewChatPayload>? NewChatReceived;
        
        /// <summary>
        /// 用户完整信息卡片事件
        /// </summary>
        public event EventHandler<UserFullCard>? UserFullCardReceived;
        
        /// <summary>
        /// 处理器负责的领域
        /// </summary>
        public override string Domain => Domains.CHANNEL;
        
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
                    case Actions.JOIN_CHANNEL:
                        await HandleJoinChannelAsync(message);
                        break;
                        
                    case Actions.MEMBER_CHANGED:
                        await HandleMemberChangedAsync(message);
                        break;
                        
                    case Actions.SEND_CHAT:
                        await HandleSendChatAsync(message);
                        break;
                        
                    case Actions.NEW_CHAT:
                        await HandleNewChatAsync(message);
                        break;
                        
                    case Actions.USER_FULL_CARD:
                        await HandleUserFullCardAsync(message);
                        break;
                        
                    default:
                        Logger.Log($"[ChannelHandler] Unhandled action: {message.Action}");
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
        /// 处理加入频道响应
        /// </summary>
        private async Task HandleJoinChannelAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<JoinChannelResponse>(message);
            if (payload != null)
            {
                Logger.Log($"[ChannelHandler] Joined channel: {payload.ChannelId}, online count: {payload.OnlineCount}");
                
                if (ChannelJoined != null)
                {
                    await Task.Run(() => ChannelJoined?.Invoke(this, payload));
                }
            }
        }
        
        /// <summary>
        /// 处理频道成员变更
        /// </summary>
        private async Task HandleMemberChangedAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<MemberChangedPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[ChannelHandler] Member changed in channel {message.TargetId}: {payload.User.UserId}, action: {payload.Action}");
                
                if (MemberChanged != null)
                {
                    await Task.Run(() => MemberChanged?.Invoke(this, payload));
                }
            }
        }
        
        /// <summary>
        /// 处理发送聊天请求（客户端发送消息后的响应）
        /// </summary>
        private async Task HandleSendChatAsync(DomainActionMessage message)
        {
            // 发送聊天消息的响应，通常包含消息ID或错误信息
            Logger.Log($"[ChannelHandler] Send chat response for message: {message.MessageId}");
            
            // 这里可以触发事件通知UI消息发送结果
        }
        
        /// <summary>
        /// 处理新聊天消息
        /// </summary>
        private async Task HandleNewChatAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<NewChatPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[ChannelHandler] New chat from {payload.Sender.UserId}: {payload.Content?.Substring(0, Math.Min(payload.Content.Length, 50))}");
                
                if (NewChatReceived != null)
                {
                    await Task.Run(() => NewChatReceived?.Invoke(this, payload));
                }
            }
        }
        
        /// <summary>
        /// 处理用户完整信息卡片
        /// </summary>
        private async Task HandleUserFullCardAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<UserFullCard>(message);
            if (payload != null)
            {
                Logger.Log($"[ChannelHandler] User full card received: {payload.UserId}, clan tag: {payload.ClanTag}");
                
                if (UserFullCardReceived != null)
                {
                    await Task.Run(() => UserFullCardReceived?.Invoke(this, payload));
                }
            }
        }
    }
}