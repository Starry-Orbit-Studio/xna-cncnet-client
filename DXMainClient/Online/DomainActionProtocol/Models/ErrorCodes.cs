#nullable enable
namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// Domain-Action 协议错误码定义
    /// </summary>
    public static class ErrorCodes
    {
        // ====== 40xx - 接入与鉴权 ======
        
        /// <summary>
        /// Ticket 无效或已过期
        /// </summary>
        public const int TICKET_INVALID_OR_EXPIRED = 4001;

        /// <summary>
        /// Session 已失效
        /// </summary>
        public const int SESSION_EXPIRED = 4002;

        /// <summary>
        /// 凭据冲突（账号异地登录）
        /// </summary>
        public const int CREDENTIAL_CONFLICT = 4003;

        // ====== 41xx - 消息验证 ======
        
        /// <summary>
        /// 消息格式错误
        /// </summary>
        public const int MESSAGE_FORMAT_ERROR = 4101;

        /// <summary>
        /// 缺少必要字段
        /// </summary>
        public const int MISSING_REQUIRED_FIELD = 4102;

        /// <summary>
        /// 字段类型错误
        /// </summary>
        public const int FIELD_TYPE_ERROR = 4103;

        /// <summary>
        /// 无效的领域或动作
        /// </summary>
        public const int INVALID_DOMAIN_OR_ACTION = 4104;

        // ====== 42xx - 频率控制 ======
        
        /// <summary>
        /// 聊天发送过快
        /// </summary>
        public const int CHAT_RATE_LIMIT_EXCEEDED = 4201;

        /// <summary>
        /// 动作请求过频
        /// </summary>
        public const int ACTION_RATE_LIMIT_EXCEEDED = 4202;

        // ====== 43xx - 房间业务逻辑 ======
        
        /// <summary>
        /// 房间已满
        /// </summary>
        public const int ROOM_FULL = 4301;

        /// <summary>
        /// 密码错误
        /// </summary>
        public const int PASSWORD_INCORRECT = 4302;

        /// <summary>
        /// 权限不足
        /// </summary>
        public const int INSUFFICIENT_PERMISSIONS = 4303;

        /// <summary>
        /// 游戏已在进行中
        /// </summary>
        public const int GAME_ALREADY_STARTED = 4304;

        /// <summary>
        /// 地图 Hash 冲突
        /// </summary>
        public const int MAP_HASH_MISMATCH = 4305;

        /// <summary>
        /// 无法开始：有人未准备
        /// </summary>
        public const int NOT_ALL_PLAYERS_READY = 4306;

        /// <summary>
        /// 无法开始：存在断线幽灵
        /// </summary>
        public const int GHOST_PLAYERS_EXIST = 4307;

        /// <summary>
        /// 房间不存在
        /// </summary>
        public const int ROOM_NOT_FOUND = 4308;

        /// <summary>
        /// 已在房间中
        /// </summary>
        public const int ALREADY_IN_ROOM = 4309;

        /// <summary>
        /// 不在房间中
        /// </summary>
        public const int NOT_IN_ROOM = 4310;

        // ====== 44xx - 组队业务逻辑 ======
        
        /// <summary>
        /// 队伍已满
        /// </summary>
        public const int PARTY_FULL = 4401;

        /// <summary>
        /// 不在队伍中
        /// </summary>
        public const int NOT_IN_PARTY = 4402;

        /// <summary>
        /// 不是队长
        /// </summary>
        public const int NOT_PARTY_LEADER = 4403;

        // ====== 45xx - 监管处罚 ======
        
        /// <summary>
        /// 全局禁言中
        /// </summary>
        public const int GLOBAL_MUTED = 4501;

        /// <summary>
        /// 房间禁言中
        /// </summary>
        public const int ROOM_MUTED = 4502;

        /// <summary>
        /// 账号已被封禁
        /// </summary>
        public const int ACCOUNT_BANNED = 4503;

        // ====== 50xx - 服务端错误 ======
        
        /// <summary>
        /// 服务端内部错误
        /// </summary>
        public const int INTERNAL_SERVER_ERROR = 5000;

        /// <summary>
        /// 数据库错误
        /// </summary>
        public const int DATABASE_ERROR = 5001;

        /// <summary>
        /// Redis 错误
        /// </summary>
        public const int REDIS_ERROR = 5002;

        /// <summary>
        /// 获取错误码分类
        /// </summary>
        public static string GetCategory(int errorCode)
        {
            if (errorCode >= 4000 && errorCode < 4100) return "Authentication";
            if (errorCode >= 4100 && errorCode < 4200) return "Validation";
            if (errorCode >= 4200 && errorCode < 4300) return "Rate Limiting";
            if (errorCode >= 4300 && errorCode < 4400) return "Room Logic";
            if (errorCode >= 4400 && errorCode < 4500) return "Party Logic";
            if (errorCode >= 4500 && errorCode < 4600) return "Moderation";
            if (errorCode >= 5000 && errorCode < 5100) return "Server Error";
            return "Unknown";
        }

        /// <summary>
        /// 判断错误码是否表示需要重新连接
        /// </summary>
        public static bool IsConnectionError(int errorCode)
        {
            return errorCode == TICKET_INVALID_OR_EXPIRED ||
                   errorCode == SESSION_EXPIRED ||
                   errorCode == CREDENTIAL_CONFLICT;
        }

        /// <summary>
        /// 判断错误码是否表示权限问题
        /// </summary>
        public static bool IsPermissionError(int errorCode)
        {
            return errorCode == INSUFFICIENT_PERMISSIONS ||
                   errorCode == NOT_PARTY_LEADER ||
                   errorCode == GLOBAL_MUTED ||
                   errorCode == ROOM_MUTED ||
                   errorCode == ACCOUNT_BANNED;
        }

        /// <summary>
        /// 判断错误码是否表示业务逻辑冲突
        /// </summary>
        public static bool IsBusinessLogicError(int errorCode)
        {
            return (errorCode >= 4300 && errorCode < 4400) || // 房间逻辑
                   (errorCode >= 4400 && errorCode < 4500);   // 组队逻辑
        }

        /// <summary>
        /// 获取错误码描述（中文）
        /// </summary>
        public static string GetDescription(int errorCode)
        {
            return errorCode switch
            {
                TICKET_INVALID_OR_EXPIRED => "Ticket 无效或已过期",
                SESSION_EXPIRED => "Session 已失效",
                CREDENTIAL_CONFLICT => "账号在其他设备登录",
                MESSAGE_FORMAT_ERROR => "消息格式错误",
                MISSING_REQUIRED_FIELD => "缺少必要字段",
                FIELD_TYPE_ERROR => "字段类型错误",
                INVALID_DOMAIN_OR_ACTION => "无效的领域或动作",
                CHAT_RATE_LIMIT_EXCEEDED => "聊天发送过快",
                ACTION_RATE_LIMIT_EXCEEDED => "操作频率过高",
                ROOM_FULL => "房间已满",
                PASSWORD_INCORRECT => "密码错误",
                INSUFFICIENT_PERMISSIONS => "权限不足",
                GAME_ALREADY_STARTED => "游戏已在进行中",
                MAP_HASH_MISMATCH => "地图文件版本不一致",
                NOT_ALL_PLAYERS_READY => "还有玩家未准备",
                GHOST_PLAYERS_EXIST => "存在断线玩家",
                ROOM_NOT_FOUND => "房间不存在",
                ALREADY_IN_ROOM => "已在房间中",
                NOT_IN_ROOM => "不在房间中",
                PARTY_FULL => "队伍已满",
                NOT_IN_PARTY => "不在队伍中",
                NOT_PARTY_LEADER => "不是队长",
                GLOBAL_MUTED => "您已被全局禁言",
                ROOM_MUTED => "您在此房间被禁言",
                ACCOUNT_BANNED => "账号已被封禁",
                INTERNAL_SERVER_ERROR => "服务端内部错误",
                DATABASE_ERROR => "数据库错误",
                REDIS_ERROR => "缓存服务错误",
                _ => $"未知错误 ({errorCode})"
            };
        }
    }
}