#nullable enable
namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// Domain-Action 协议动作定义
    /// </summary>
    public static class Actions
    {
        // ====== SYSTEM 域动作 ======
        
        /// <summary>
        /// 连接成功
        /// </summary>
        public const string CONNECTED = "CONNECTED";

        /// <summary>
        /// 心跳请求
        /// </summary>
        public const string HEARTBEAT = "HEARTBEAT";

        /// <summary>
        /// 心跳响应
        /// </summary>
        public const string PONG = "PONG";

        /// <summary>
        /// 错误通知
        /// </summary>
        public const string ERROR = "ERROR";

        // ====== PRESENCE 域动作 ======
        
        /// <summary>
        /// 更新可见性
        /// </summary>
        public const string UPDATE_VISIBILITY = "UPDATE_VISIBILITY";

        // ====== CHANNEL 域动作 ======
        
        /// <summary>
        /// 加入频道
        /// </summary>
        public const string JOIN_CHANNEL = "JOIN_CHANNEL";

        /// <summary>
        /// 频道加入成功
        /// </summary>
        public const string JOIN_SUCCESS = "JOIN_SUCCESS";

        /// <summary>
        /// 成员变动
        /// </summary>
        public const string MEMBER_CHANGED = "MEMBER_CHANGED";

        /// <summary>
        /// 发送聊天消息
        /// </summary>
        public const string SEND_CHAT = "SEND_CHAT";

        /// <summary>
        /// 新聊天消息
        /// </summary>
        public const string NEW_CHAT = "NEW_CHAT";

        /// <summary>
        /// 用户完整信息卡片
        /// </summary>
        public const string USER_FULL_CARD = "USER_FULL_CARD";

        // ====== ROOM 域动作 ======
        
        /// <summary>
        /// 创建房间
        /// </summary>
        public const string CREATE_ROOM = "CREATE_ROOM";

        /// <summary>
        /// 加入房间
        /// </summary>
        public const string JOIN_ROOM = "JOIN_ROOM";

        /// <summary>
        /// 离开房间
        /// </summary>
        public const string LEAVE_ROOM = "LEAVE_ROOM";

        /// <summary>
        /// 房间状态同步
        /// </summary>
        public const string ROOM_SYNC = "ROOM_SYNC";

        /// <summary>
        /// 设置准备状态
        /// </summary>
        public const string SET_READY = "SET_READY";

        /// <summary>
        /// 开始游戏
        /// </summary>
        public const string START_GAME = "START_GAME";

        /// <summary>
        /// 游戏开始中
        /// </summary>
        public const string GAME_STARTING = "GAME_STARTING";

        /// <summary>
        /// 房间已创建
        /// </summary>
        public const string ROOM_CREATED = "ROOM_CREATED";

        /// <summary>
        /// 房间已更新
        /// </summary>
        public const string ROOM_UPDATED = "ROOM_UPDATED";

        /// <summary>
        /// 房间已删除
        /// </summary>
        public const string ROOM_DELETED = "ROOM_DELETED";

        /// <summary>
        /// 强制进入房间（组队联动）
        /// </summary>
        public const string FORCE_ENTER_ROOM = "FORCE_ENTER_ROOM";

        // ====== PARTY 域动作 ======
        
        /// <summary>
        /// 邀请用户
        /// </summary>
        public const string INVITE_USER = "INVITE_USER";

        /// <summary>
        /// 收到邀请
        /// </summary>
        public const string INVITE_RECEIVED = "INVITE_RECEIVED";

        /// <summary>
        /// 组队状态同步
        /// </summary>
        public const string PARTY_SYNC = "PARTY_SYNC";

        // ====== SOCIAL 域动作 ======
        
        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public const string SEND_PRIVATE_MESSAGE = "SEND_PRIVATE_MESSAGE";

        /// <summary>
        /// 收到私聊消息
        /// </summary>
        public const string PRIVATE_MESSAGE_RECEIVED = "PRIVATE_MESSAGE_RECEIVED";

        /// <summary>
        /// 好友请求已收到
        /// </summary>
        public const string FRIEND_REQUEST_RECEIVED = "FRIEND_REQUEST_RECEIVED";

        // ====== CLAN 域动作 ======
        
        /// <summary>
        /// 战队聊天
        /// </summary>
        public const string CLAN_CHAT = "CLAN_CHAT";

        /// <summary>
        /// 验证动作是否属于指定领域
        /// </summary>
        public static bool IsActionInDomain(string action, string domain)
        {
            return domain switch
            {
                Domains.SYSTEM => action == CONNECTED || action == HEARTBEAT || action == PONG || action == ERROR,
                Domains.PRESENCE => action == UPDATE_VISIBILITY,
                Domains.CHANNEL => action == JOIN_CHANNEL || action == JOIN_SUCCESS || action == MEMBER_CHANGED || 
                                   action == SEND_CHAT || action == NEW_CHAT || action == USER_FULL_CARD,
                Domains.ROOM => action == CREATE_ROOM || action == JOIN_ROOM || action == LEAVE_ROOM || 
                                action == ROOM_SYNC || action == SET_READY || action == START_GAME || 
                                action == GAME_STARTING || action == ROOM_CREATED || action == ROOM_UPDATED || 
                                action == ROOM_DELETED || action == FORCE_ENTER_ROOM,
                Domains.PARTY => action == INVITE_USER || action == INVITE_RECEIVED || action == PARTY_SYNC,
                Domains.SOCIAL => action == SEND_PRIVATE_MESSAGE || action == PRIVATE_MESSAGE_RECEIVED || 
                                 action == FRIEND_REQUEST_RECEIVED,
                Domains.CLAN => action == CLAN_CHAT,
                _ => false
            };
        }

        /// <summary>
        /// 获取指定领域的所有动作
        /// </summary>
        public static string[] GetActionsForDomain(string domain)
        {
            return domain switch
            {
                Domains.SYSTEM => new[] { CONNECTED, HEARTBEAT, PONG, ERROR },
                Domains.PRESENCE => new[] { UPDATE_VISIBILITY },
                Domains.CHANNEL => new[] { JOIN_CHANNEL, JOIN_SUCCESS, MEMBER_CHANGED, SEND_CHAT, NEW_CHAT, USER_FULL_CARD },
                Domains.ROOM => new[] { CREATE_ROOM, JOIN_ROOM, LEAVE_ROOM, ROOM_SYNC, SET_READY, START_GAME, 
                                        GAME_STARTING, ROOM_CREATED, ROOM_UPDATED, ROOM_DELETED, FORCE_ENTER_ROOM },
                Domains.PARTY => new[] { INVITE_USER, INVITE_RECEIVED, PARTY_SYNC },
                Domains.SOCIAL => new[] { SEND_PRIVATE_MESSAGE, PRIVATE_MESSAGE_RECEIVED, FRIEND_REQUEST_RECEIVED },
                Domains.CLAN => new[] { CLAN_CHAT },
                _ => new string[0]
            };
        }
    }
}