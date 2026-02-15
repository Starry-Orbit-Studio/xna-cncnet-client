using System;
using ClientCore;
using ClientCore.ExternalAccount;

namespace DTAClient.Online
{
    /// <summary>
    /// 统一管理玩家身份的服务
    /// 提供当前玩家的显示名称、头像等信息
    /// </summary>
    public class PlayerIdentityService
    {
        private static PlayerIdentityService _instance;
        private static readonly object _lock = new object();

        private ExternalAccountService _externalAccountService;

        public event EventHandler IdentityChanged;

        private PlayerIdentityService(ExternalAccountService externalAccountService)
        {
            _externalAccountService = externalAccountService;
            _externalAccountService.LoginStateChanged += OnLoginStateChanged;
            _externalAccountService.UserInfoUpdated += OnUserInfoUpdated;
        }

        public static void Initialize(ExternalAccountService externalAccountService)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new PlayerIdentityService(externalAccountService);
                }
            }
        }

        public static PlayerIdentityService Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("PlayerIdentityService not initialized. Call Initialize first.");
                return _instance;
            }
        }

        private void OnLoginStateChanged(object sender, EventArgs e)
        {
            IdentityChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnUserInfoUpdated(object sender, EventArgs e)
        {
            IdentityChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 获取当前玩家的显示名称
        /// 如果已登录真实用户，返回真实用户名
        /// 否则返回本地名称 + " (游客)"
        /// </summary>
        public string GetDisplayName()
        {
            if (_externalAccountService.IsLoggedIn)
            {
                var user = _externalAccountService.CurrentUser;
                return user?.DisplayName ?? user?.Username ?? "Unknown";
            }
            else
            {
                return ProgramConstants.PLAYERNAME + " (游客)";
            }
        }

        /// <summary>
        /// 获取当前玩家的纯名称（不包含游客标识）
        /// 如果已登录真实用户，返回真实用户名
        /// 否则返回本地名称
        /// </summary>
        public string GetPlainName()
        {
            if (_externalAccountService.IsLoggedIn)
            {
                var user = _externalAccountService.CurrentUser;
                return user?.DisplayName ?? user?.Username ?? "Unknown";
            }
            else
            {
                return ProgramConstants.PLAYERNAME;
            }
        }

        /// <summary>
        /// 获取当前玩家的头像URL
        /// 如果已登录真实用户，返回头像URL
        /// 否则返回 null
        /// </summary>
        public string GetAvatarUrl()
        {
            if (_externalAccountService.IsLoggedIn)
            {
                var user = _externalAccountService.CurrentUser;
                return user?.AvatarUrl;
            }
            return null;
        }

        /// <summary>
        /// 判断当前玩家是否是游客
        /// </summary>
        public bool IsGuest()
        {
            return !_externalAccountService.IsLoggedIn;
        }

        /// <summary>
        /// 判断当前玩家是否已登录真实用户系统
        /// </summary>
        public bool IsLoggedIn()
        {
            return _externalAccountService.IsLoggedIn;
        }

        /// <summary>
        /// 获取当前玩家的IRC名称
        /// 返回本地玩家名称（ProgramConstants.PLAYERNAME）
        /// </summary>
        public string GetIRCName()
        {
            return ProgramConstants.PLAYERNAME;
        }
    }
}

