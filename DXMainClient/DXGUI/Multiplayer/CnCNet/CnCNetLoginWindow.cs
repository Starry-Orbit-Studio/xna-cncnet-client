using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientGUI;
using ClientCore.Extensions;
using ClientCore.ExternalAccount;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    class CnCNetLoginWindow : XNAWindow
    {
        private readonly ExternalAccountService _externalAccountService;

        public CnCNetLoginWindow(WindowManager windowManager, ExternalAccountService externalAccountService) : base(windowManager)
        {
            _externalAccountService = externalAccountService;
        }

        XNALabel lblConnectToCnCNet;
        XNATextBox tbPlayerName;
        XNALabel lblPlayerName;
        XNAClientCheckBox chkRememberMe;
        XNAClientCheckBox chkPersistentMode;
        XNAClientCheckBox chkAutoConnect;
        XNAClientButton btnConnect;
        XNAClientButton btnCancel;
        XNAClientButton btnLogin;
        XNAClientButton btnLogout;
        XNALabel lblAccountInfo;
        XNALabel lblGuestHint;
        XNAClientButton btnAvatar;
        private Texture2D _userAvatarTexture;

        public event EventHandler Cancelled;
        public event EventHandler Connect;
        public event EventHandler LoginRequested;

        public override void Initialize()
        {
            Name = "CnCNetLoginWindow";
            ClientRectangle = new Rectangle(0, 0, 300, 220);
            BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");

            lblConnectToCnCNet = new XNALabel(WindowManager);
            lblConnectToCnCNet.Name = "lblConnectToCnCNet";
            lblConnectToCnCNet.FontIndex = 1;
            lblConnectToCnCNet.Text = "CONNECT TO CNCNET".L10N("Client:Main:ConnectToCncNet");

            AddChild(lblConnectToCnCNet);
            lblConnectToCnCNet.CenterOnParent();
            lblConnectToCnCNet.ClientRectangle = new Rectangle(
                lblConnectToCnCNet.X, 12,
                lblConnectToCnCNet.Width, 
                lblConnectToCnCNet.Height);

            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = "tbPlayerName";
            tbPlayerName.ClientRectangle = new Rectangle(Width - 132, 50, 120, 19);
            tbPlayerName.MaximumTextLength = ClientConfiguration.Instance.MaxNameLength;
            tbPlayerName.IMEDisabled = true;
            string defgame = ClientConfiguration.Instance.LocalGame;

            lblPlayerName = new XNALabel(WindowManager);
            lblPlayerName.Name = "lblPlayerName";
            lblPlayerName.FontIndex = 1;
            lblPlayerName.Text = "PLAYER NAME:".L10N("Client:Main:PlayerName");
            lblPlayerName.ClientRectangle = new Rectangle(12, tbPlayerName.Y + 1,
                lblPlayerName.Width, lblPlayerName.Height);

            chkRememberMe = new XNAClientCheckBox(WindowManager);
            chkRememberMe.Name = "chkRememberMe";
            chkRememberMe.ClientRectangle = new Rectangle(12, tbPlayerName.Bottom + 12, 0, 0);
            chkRememberMe.Text = "Remember me".L10N("Client:Main:RememberMe");
            chkRememberMe.TextPadding = 7;
            chkRememberMe.CheckedChanged += ChkRememberMe_CheckedChanged;

            chkPersistentMode = new XNAClientCheckBox(WindowManager);
            chkPersistentMode.Name = "chkPersistentMode";
            chkPersistentMode.ClientRectangle = new Rectangle(12, chkRememberMe.Bottom + 30, 0, 0);
            chkPersistentMode.Text = "Stay connected outside of the CnCNet lobby".L10N("Client:Main:StayConnect");
            chkPersistentMode.TextPadding = chkRememberMe.TextPadding;
            chkPersistentMode.CheckedChanged += ChkPersistentMode_CheckedChanged;

            chkAutoConnect = new XNAClientCheckBox(WindowManager);
            chkAutoConnect.Name = "chkAutoConnect";
            chkAutoConnect.ClientRectangle = new Rectangle(12, chkPersistentMode.Bottom + 30, 0, 0);
            chkAutoConnect.Text = "Connect automatically on client startup".L10N("Client:Main:AutoConnect");
            chkAutoConnect.TextPadding = chkRememberMe.TextPadding;
            chkAutoConnect.AllowChecking = false;

            btnConnect = new XNAClientButton(WindowManager);
            btnConnect.Name = "btnConnect";
            btnConnect.ClientRectangle = new Rectangle(130, Height - 35, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnConnect.Text = "Connect".L10N("Client:Main:ButtonConnect");
            btnConnect.LeftClick += BtnConnect_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - UIDesignConstants.BUTTON_WIDTH_92 - 12, btnConnect.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(tbPlayerName);
            AddChild(lblPlayerName);
            AddChild(chkRememberMe);
            AddChild(chkPersistentMode);
            AddChild(chkAutoConnect);
            AddChild(btnConnect);
            AddChild(btnCancel);

            btnLogin = new XNAClientButton(WindowManager);
            btnLogin.Name = nameof(btnLogin);
            btnLogin.Text = "Login".L10N("Client:Main:Login");
            btnLogin.ClientRectangle = new Rectangle(12, Height - 35, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnLogin.LeftClick += BtnLogin_LeftClick;

            btnLogout = new XNAClientButton(WindowManager);
            btnLogout.Name = nameof(btnLogout);
            btnLogout.Text = "Logout".L10N("Client:Main:Logout");
            btnLogout.ClientRectangle = new Rectangle(104, Height - 35, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnLogout.LeftClick += BtnLogout_LeftClick;

            lblAccountInfo = new XNALabel(WindowManager);
            lblAccountInfo.Name = nameof(lblAccountInfo);
            lblAccountInfo.FontIndex = 1;
            lblAccountInfo.ClientRectangle = new Rectangle(70, 50, 0, 0);

            lblGuestHint = new XNALabel(WindowManager);
            lblGuestHint.Name = nameof(lblGuestHint);
            lblGuestHint.Text = "You are in guest mode. Login for avatar, level and more features."
                .L10N("Client:Main:GuestModeHint");
            lblGuestHint.ClientRectangle = new Rectangle(12, chkAutoConnect.Bottom + 20, 0, 0);

            btnAvatar = new XNAClientButton(WindowManager);
            btnAvatar.Name = nameof(btnAvatar);
            btnAvatar.ClientRectangle = new Rectangle(12, 50, 48, 48);
            btnAvatar.AllowClick = false;

            AddChild(btnLogin);
            AddChild(btnLogout);
            AddChild(lblAccountInfo);
            AddChild(lblGuestHint);
            AddChild(btnAvatar);

            base.Initialize();

            CenterOnParent();

            UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;
            _externalAccountService.LoginStateChanged += ExternalAccountService_LoginStateChanged;

            UpdateUI();
        }

        private void ExternalAccountService_LoginStateChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void Instance_SettingsSaved(object sender, EventArgs e)
        {
            tbPlayerName.Text = UserINISettings.Instance.PlayerName;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateUI()
        {
            if (_externalAccountService.IsLoggedIn)
            {
                var user = _externalAccountService.CurrentUser;

                tbPlayerName.Visible = false;
                lblPlayerName.Visible = false;
                chkRememberMe.Visible = false;
                lblGuestHint.Visible = false;

                string userInfoText = $"Logged in as: {user?.Nickname ?? "Unknown"}";
                if (user != null && user.Level > 0)
                {
                    userInfoText += $"\nLevel: {user.Level}";
                }
                if (user != null && user.ExperiencePoints > 0)
                {
                    userInfoText += $"\nXP: {user.ExperiencePoints}";
                }
                lblAccountInfo.Text = userInfoText;
                lblAccountInfo.Visible = true;

                btnAvatar.Visible = true;
                if (user != null && !string.IsNullOrEmpty(user.AvatarUrl))
                {
                    LoadAvatarAsync(user.AvatarUrl);
                }
                else
                {
                    var defaultAvatar = LoadDefaultAvatar();
                    btnAvatar.IdleTexture = defaultAvatar;
                    btnAvatar.HoverTexture = defaultAvatar;
                }

                btnLogin.Visible = false;
                btnLogout.Visible = true;
                btnConnect.Visible = true;
                btnConnect.ClientRectangle = new Rectangle(12, Height - 35, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
                btnCancel.ClientRectangle = new Rectangle(Width - UIDesignConstants.BUTTON_WIDTH_92 - 12, btnConnect.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            }
            else
            {
                tbPlayerName.Visible = true;
                lblPlayerName.Visible = true;
                chkRememberMe.Visible = true;

                lblAccountInfo.Visible = false;

                lblGuestHint.Visible = true;

                btnAvatar.Visible = false;

                btnLogin.Visible = true;
                btnLogout.Visible = false;
                btnConnect.Visible = true;
                btnConnect.ClientRectangle = new Rectangle(130, Height - 35, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
                btnCancel.ClientRectangle = new Rectangle(Width - UIDesignConstants.BUTTON_WIDTH_92 - 12, btnConnect.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            }
        }

        private Texture2D LoadDefaultAvatar()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("DTAClient.Icons.esicon.png"))
                {
                    if (stream != null)
                    {
                        using (var image = SixLabors.ImageSharp.Image.Load(stream))
                        {
                            return AssetLoader.TextureFromImage(image);
                        }
                    }
                }
            }
            catch
            {
            }
            return AssetLoader.LoadTexture("MainMenu/button.png");
        }

        private async void LoadAvatarAsync(string avatarUrl)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.Timeout = System.TimeSpan.FromSeconds(10);
                    var response = await httpClient.GetAsync(avatarUrl);
                    response.EnsureSuccessStatusCode();

                    byte[] imageData = await response.Content.ReadAsByteArrayAsync();

                    WindowManager.AddCallback(new Action(() =>
                    {
                        try
                        {
                            using (var memoryStream = new System.IO.MemoryStream(imageData))
                            using (var image = SixLabors.ImageSharp.Image.Load(memoryStream))
                            {
                                var texture = AssetLoader.TextureFromImage(image);
                                if (texture != null)
                                {
                                    btnAvatar.IdleTexture = texture;
                                    btnAvatar.HoverTexture = texture;
                                }
                            }
                        }
                        catch
                        {
                            var defaultAvatar = LoadDefaultAvatar();
                            btnAvatar.IdleTexture = defaultAvatar;
                            btnAvatar.HoverTexture = defaultAvatar;
                        }
                    }), null);
                }
            }
            catch
            {
                WindowManager.AddCallback(new Action(() =>
                {
                    var defaultAvatar = LoadDefaultAvatar();
                    btnAvatar.IdleTexture = defaultAvatar;
                    btnAvatar.HoverTexture = defaultAvatar;
                }), null);
            }
        }

       
        
        private void BtnLogin_LeftClick(object sender, EventArgs e)
        {
            LoginRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnLogout_LeftClick(object sender, EventArgs e)
        {
            _externalAccountService.Logout();
            UpdateUI();
        }

        private void ChkRememberMe_CheckedChanged(object sender, EventArgs e)
        {
            CheckAutoConnectAllowance();
        }

        private void ChkPersistentMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckAutoConnectAllowance();
        }

        private void CheckAutoConnectAllowance()
        {
            chkAutoConnect.AllowChecking = chkPersistentMode.Checked && chkRememberMe.Checked;
            if (!chkAutoConnect.AllowChecking)
                chkAutoConnect.Checked = false;
        }

        private void BtnConnect_LeftClick(object sender, EventArgs e)
        {
            if (_externalAccountService.IsLoggedIn)
            {
                var user = _externalAccountService.CurrentUser;
                ProgramConstants.PLAYERNAME = user?.Nickname ?? "Unknown";
            }
            else
            {
                NameValidationError validationError = NameValidator.IsNameValid(tbPlayerName.Text, out string errorMessage);

                if (validationError != NameValidationError.None)
                {
                    XNAMessageBox.Show(WindowManager, "Invalid Player Name".L10N("Client:Main:InvalidPlayerName"), errorMessage);
                    return;
                }

                ProgramConstants.PLAYERNAME = tbPlayerName.Text;
            }

            UserINISettings.Instance.SkipConnectDialog.Value = chkRememberMe.Checked;
            UserINISettings.Instance.PersistentMode.Value = chkPersistentMode.Checked;
            UserINISettings.Instance.AutomaticCnCNetLogin.Value = chkAutoConnect.Checked;
            UserINISettings.Instance.PlayerName.Value = ProgramConstants.PLAYERNAME;

            UserINISettings.Instance.SaveSettings();

            Connect?.Invoke(this, EventArgs.Empty);
        }

        public void LoadSettings()
        {
            chkAutoConnect.Checked = UserINISettings.Instance.AutomaticCnCNetLogin;
            chkPersistentMode.Checked = UserINISettings.Instance.PersistentMode;
            chkRememberMe.Checked = UserINISettings.Instance.SkipConnectDialog;

            tbPlayerName.Text = UserINISettings.Instance.PlayerName;

            UpdateUI();

            if (_externalAccountService.IsLoggedIn && chkAutoConnect.Checked)
            {
                BtnConnect_LeftClick(this, EventArgs.Empty);
            }
            else if (!_externalAccountService.IsLoggedIn && chkRememberMe.Checked)
            {
                BtnConnect_LeftClick(this, EventArgs.Empty);
            }
        }
    }
}
