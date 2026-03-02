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
    /// ROOM 域消息处理器
    /// </summary>
    public class RoomHandler : BaseDomainHandler
    {
        private readonly HashSet<string> _supportedActions = new HashSet<string>
        {
            Actions.CREATE_ROOM,
            Actions.JOIN_ROOM,
            Actions.ROOM_SYNC,
            Actions.SET_READY,
            Actions.START_GAME,
            Actions.GAME_STARTING,
            Actions.LEAVE_ROOM
        };
        
        /// <summary>
        /// 房间创建事件
        /// </summary>
        public event EventHandler<RoomSyncPayload>? RoomCreated;
        
        /// <summary>
        /// 房间同步事件（玩家加入、离开、状态更新）
        /// </summary>
        public event EventHandler<RoomSyncPayload>? RoomSynced;
        
        /// <summary>
        /// 玩家准备状态变更事件
        /// </summary>
        public event EventHandler<SetReadyPayload>? PlayerReadyChanged;
        
        /// <summary>
        /// 游戏开始事件
        /// </summary>
        public event EventHandler<GameStartingPayload>? GameStarting;
        
        /// <summary>
        /// 房间离开事件
        /// </summary>
        public event EventHandler<string>? RoomLeft;
        
        /// <summary>
        /// 处理器负责的领域
        /// </summary>
        public override string Domain => Domains.ROOM;
        
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
                    case Actions.CREATE_ROOM:
                        await HandleCreateRoomAsync(message);
                        break;
                        
                    case Actions.JOIN_ROOM:
                        await HandleJoinRoomAsync(message);
                        break;
                        
                    case Actions.ROOM_SYNC:
                        await HandleRoomSyncAsync(message);
                        break;
                        
                    case Actions.SET_READY:
                        await HandleSetReadyAsync(message);
                        break;
                        
                    case Actions.START_GAME:
                        await HandleStartGameAsync(message);
                        break;
                        
                    case Actions.GAME_STARTING:
                        await HandleGameStartingAsync(message);
                        break;
                        
                    case Actions.LEAVE_ROOM:
                        await HandleLeaveRoomAsync(message);
                        break;
                        
                    default:
                        Logger.Log($"[RoomHandler] Unhandled action: {message.Action}");
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
        /// 处理创建房间响应
        /// </summary>
        private async Task HandleCreateRoomAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<RoomSyncPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[RoomHandler] Room created: {payload.RoomId}, players: {payload.Members?.Count ?? 0}");
                
                if (RoomCreated != null)
                {
                    await Task.Run(() => RoomCreated?.Invoke(this, payload));
                }
            }
        }
        
        /// <summary>
        /// 处理加入房间响应
        /// </summary>
        private async Task HandleJoinRoomAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<RoomSyncPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[RoomHandler] Joined room: {payload.RoomId}, players: {payload.Members?.Count ?? 0}");
                
                if (RoomSynced != null)
                {
                    await Task.Run(() => RoomSynced?.Invoke(this, payload));
                }
            }
        }
        
        /// <summary>
        /// 处理房间同步消息
        /// </summary>
        private async Task HandleRoomSyncAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<RoomSyncPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[RoomHandler] Room synced: {payload.RoomId}, players: {payload.Members?.Count ?? 0}");
                
                if (RoomSynced != null)
                {
                    await Task.Run(() => RoomSynced?.Invoke(this, payload));
                }
            }
        }
        
        /// <summary>
        /// 处理玩家准备状态变更
        /// </summary>
        private async Task HandleSetReadyAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<SetReadyPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[RoomHandler] Player ready changed: {payload.PlayerId}, ready: {payload.IsReady}");
                
                if (PlayerReadyChanged != null)
                {
                    await Task.Run(() => PlayerReadyChanged?.Invoke(this, payload));
                }
            }
        }
        
        /// <summary>
        /// 处理开始游戏请求
        /// </summary>
        private async Task HandleStartGameAsync(DomainActionMessage message)
        {
            Logger.Log($"[RoomHandler] Start game requested for room: {message.TargetId}");
            
            // 客户端通常不直接处理 START_GAME，而是发送 START_GAME 请求后等待 GAME_STARTING
            // 这里可以触发事件通知UI
        }
        
        /// <summary>
        /// 处理游戏即将开始
        /// </summary>
        private async Task HandleGameStartingAsync(DomainActionMessage message)
        {
            var payload = GetPayloadSafely<GameStartingPayload>(message);
            if (payload != null)
            {
                Logger.Log($"[RoomHandler] Game starting: {payload.RoomId}, tunnel_ip provided: {!string.IsNullOrEmpty(payload.TunnelIp)}");
                
                if (GameStarting != null)
                {
                    await Task.Run(() => GameStarting?.Invoke(this, payload));
                }
            }
        }
        
        /// <summary>
        /// 处理离开房间
        /// </summary>
        private async Task HandleLeaveRoomAsync(DomainActionMessage message)
        {
            Logger.Log($"[RoomHandler] Left room: {message.TargetId}");
            
            if (RoomLeft != null)
            {
                await Task.Run(() => RoomLeft?.Invoke(this, message.TargetId ?? string.Empty));
            }
        }
    }
}