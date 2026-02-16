using ClientCore;
using ClientCore.ExternalAccount;
using ClientCore.Extensions;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// 登录窗口 - 支持GitHub OAuth登录
    /// </summary>
    public class LoginWindow : XNAWindow
    {
        private readonly ExternalAccountService _accountService;
        private OAuthService _oauthService;

        private XNAClientButton btnGitHubLogin;
        private XNAClientButton btnCancel;
        private XNALabel lblStatus;
        private XNALabel lblTitle;

        public LoginWindow(WindowManager windowManager, ExternalAccountService accountService) : base(windowManager)
        {
            _accountService = accountService;
        }

        public override void Initialize()
        {
            Name = nameof(LoginWindow);
            BackgroundTexture = AssetLoader.LoadTexture("optionsbg.png");

            // 设置窗口尺寸 - 初始位置设为(0,0)，在Open()中居中
            ClientRectangle = new Rectangle(0, 0, 400, 250);

            lblTitle = new XNALabel(WindowManager);
            lblTitle.Name = nameof(lblTitle);
            lblTitle.Text = "External Account Login".L10N("Client:Main:ExternalAccountLogin");
            lblTitle.FontIndex = 1;
            lblTitle.ClientRectangle = new Rectangle(0, 30, 0, 0);
            AddChild(lblTitle);
            lblTitle.CenterOnParentHorizontally();

            // GitHub登录按钮
            btnGitHubLogin = new XNAClientButton(WindowManager);
            btnGitHubLogin.Name = nameof(btnGitHubLogin);
            btnGitHubLogin.Text = "Login with GitHub".L10N("Client:Main:LoginWithGitHub");
            btnGitHubLogin.ClientRectangle = new Rectangle(120, 90, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnGitHubLogin.LeftClick += BtnGitHubLogin_LeftClick;
            AddChild(btnGitHubLogin);

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.Text = "Cancel".L10N("Client:Main:Cancel");
            btnCancel.ClientRectangle = new Rectangle(120, 140, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.LeftClick += BtnCancel_LeftClick;
            AddChild(btnCancel);

            lblStatus = new XNALabel(WindowManager);
            lblStatus.Name = nameof(lblStatus);
            lblStatus.ClientRectangle = new Rectangle(50, 190, 300, 30);
            lblStatus.Text = "";
            AddChild(lblStatus);

            base.Initialize();

            // 初始化GitHub OAuth服务
            InitializeOAuthService();
        }

        private void InitializeOAuthService()
        {
            try
            {
                // 从ClientConfiguration读取OAuth设置
                var config = ClientConfiguration.Instance;
                string apiBaseUrl = config.ExternalAccountApiBaseUrl;
                string provider = "github"; // 默认使用GitHub
                
                Logger.Log($"初始化OAuth服务: ApiBaseUrl={apiBaseUrl}");
                
                _oauthService = new OAuthService(apiBaseUrl, provider);
                _oauthService.AuthenticationCompleted += OAuthService_AuthenticationCompleted;
            }
            catch (Exception ex)
            {
                Rampastring.Tools.Logger.Log($"初始化OAuth服务失败: {ex.Message}");
            }
        }

        private async void BtnGitHubLogin_LeftClick(object sender, EventArgs e)
        {
            if (_oauthService == null)
            {
                lblStatus.Text = "OAuth service not available.".L10N("Client:Main:OAuthServiceNotAvailable");
                return;
            }

            btnGitHubLogin.AllowClick = false;
            btnCancel.AllowClick = false;
            lblStatus.Text = "Opening browser for GitHub authentication...".L10N("Client:Main:OpeningBrowser");

            try
            {
                await _oauthService.StartAuthenticationAsync();
                lblStatus.Text = "Please complete authentication in your browser.".L10N("Client:Main:CompleteInBrowser");
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Failed to start authentication: {ex.Message}".L10N("Client:Main:AuthStartFailed");
                btnGitHubLogin.AllowClick = true;
                btnCancel.AllowClick = true;
            }
        }

        private async void OAuthService_AuthenticationCompleted(object sender, OAuthResult result)
        {
            // 在主UI线程上执行
            WindowManager.AddCallback(new Action(() => HandleOAuthResult(result)), null);
        }

        private async void HandleOAuthResult(OAuthResult result)
        {
            if (result.Success)
            {
                lblStatus.Text = "Authentication successful, logging in...".L10N("Client:Main:AuthSuccessfulLoggingIn");

                // 使用OAuth令牌登录到外部账户系统
                bool loginSuccess = await _accountService.LoginWithOAuthAsync(result.AccessToken, result.UserInfo);

                if (loginSuccess)
                {
                    await Task.Delay(500);
                    _oauthService?.Stop();
                    Disable();
                }
                else
                {
                    lblStatus.Text = $"Login failed: {_accountService.LastError}".L10N("Client:Main:LoginFailedWithError");
                    btnGitHubLogin.AllowClick = true;
                    btnCancel.AllowClick = true;
                }
            }
            else
            {
                lblStatus.Text = $"Authentication failed: {result.Error}".L10N("Client:Main:AuthFailed");
                btnGitHubLogin.AllowClick = true;
                btnCancel.AllowClick = true;
            }
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            _oauthService?.Stop();
            Disable();
        }

        public void Open()
        {
            lblStatus.Text = "";
            btnGitHubLogin.AllowClick = true;
            btnCancel.AllowClick = true;
            _accountService.LastError = null;

            if (Parent is DarkeningPanel darkeningPanel)
            {
                darkeningPanel.SetPositionAndSize();
                darkeningPanel.Show();
            }

            CenterOnParent();
            Enable();
        }


    }
}