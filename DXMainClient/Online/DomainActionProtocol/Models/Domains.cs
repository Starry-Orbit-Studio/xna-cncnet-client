#nullable enable
namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// Domain-Action 协议业务领域定义
    /// </summary>
    public static class Domains
    {
        /// <summary>
        /// 系统域：连接管理、心跳、错误处理
        /// </summary>
        public const string SYSTEM = "SYSTEM";

        /// <summary>
        /// 在线状态域：用户可见性管理
        /// </summary>
        public const string PRESENCE = "PRESENCE";

        /// <summary>
        /// 频道域：公共聊天、成员管理
        /// </summary>
        public const string CHANNEL = "CHANNEL";

        /// <summary>
        /// 房间域：游戏房间创建、加入、状态同步
        /// </summary>
        public const string ROOM = "ROOM";

        /// <summary>
        /// 组队域：玩家组队管理
        /// </summary>
        public const string PARTY = "PARTY";

        /// <summary>
        /// 社交域：好友、私聊、邀请
        /// </summary>
        public const string SOCIAL = "SOCIAL";

        /// <summary>
        /// 战队域：战队聊天、管理
        /// </summary>
        public const string CLAN = "CLAN";

        /// <summary>
        /// 验证领域字符串是否有效
        /// </summary>
        public static bool IsValidDomain(string domain)
        {
            return domain == SYSTEM ||
                   domain == PRESENCE ||
                   domain == CHANNEL ||
                   domain == ROOM ||
                   domain == PARTY ||
                   domain == SOCIAL ||
                   domain == CLAN;
        }

        /// <summary>
        /// 获取所有领域列表
        /// </summary>
        public static string[] GetAllDomains()
        {
            return new[]
            {
                SYSTEM,
                PRESENCE,
                CHANNEL,
                ROOM,
                PARTY,
                SOCIAL,
                CLAN
            };
        }
    }
}