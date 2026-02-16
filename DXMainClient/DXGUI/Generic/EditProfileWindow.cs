using ClientCore;
using ClientCore.ExternalAccount;
using ClientCore.Extensions;
using ClientGUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace DTAClient.DXGUI.Generic
{
    public class EditProfileWindow : XNAWindow
    {
        private readonly ExternalAccountService _accountService;

        private XNALabel lblTitle;
        private XNALabel lblNickname;
        private XNALabel lblAvatar;
        private XNALabel lblStatus;

        private XNATextBox tbNickname;
        private XNATextBox tbAvatar;

        private XNAClientButton btnAvatarPreview;
        private XNAClientButton btnSave;
        private XNAClientButton btnCancel;

        private Texture2D _defaultAvatarTexture;

        public EditProfileWindow(WindowManager windowManager, ExternalAccountService accountService) : base(windowManager)
        {
            _accountService = accountService;
        }

        public override void Initialize()
        {
            Name = nameof(EditProfileWindow);
            BackgroundTexture = AssetLoader.LoadTexture("optionsbg.png");
            ClientRectangle = new Rectangle(0, 0, 400, 320);

            lblTitle = new XNALabel(WindowManager);
            lblTitle.Name = nameof(lblTitle);
            lblTitle.Text = "Edit Profile".L10N("Client:Main:EditProfile");
            lblTitle.FontIndex = 1;
            lblTitle.ClientRectangle = new Rectangle(0, 25, 0, 0);
            AddChild(lblTitle);
            lblTitle.CenterOnParentHorizontally();

            lblNickname = new XNALabel(WindowManager);
            lblNickname.Name = nameof(lblNickname);
            lblNickname.Text = "Nickname:".L10N("Client:Main:Nickname");
            lblNickname.ClientRectangle = new Rectangle(30, 70, 0, 0);
            AddChild(lblNickname);

            tbNickname = new XNATextBox(WindowManager);
            tbNickname.Name = nameof(tbNickname);
            tbNickname.MaximumTextLength = 50;
            tbNickname.ClientRectangle = new Rectangle(120, 68, 250, 19);
            AddChild(tbNickname);

            lblAvatar = new XNALabel(WindowManager);
            lblAvatar.Name = nameof(lblAvatar);
            lblAvatar.Text = "Avatar URL:".L10N("Client:Main:AvatarURL");
            lblAvatar.ClientRectangle = new Rectangle(30, 110, 0, 0);
            AddChild(lblAvatar);

            tbAvatar = new XNATextBox(WindowManager);
            tbAvatar.Name = nameof(tbAvatar);
            tbAvatar.MaximumTextLength = 500;
            tbAvatar.ClientRectangle = new Rectangle(120, 108, 250, 19);
            AddChild(tbAvatar);

            btnAvatarPreview = new XNAClientButton(WindowManager);
            btnAvatarPreview.Name = nameof(btnAvatarPreview);
            btnAvatarPreview.Text = "Preview".L10N("Client:Main:Preview");
            btnAvatarPreview.ClientRectangle = new Rectangle(120, 140, 100, 23);
            btnAvatarPreview.LeftClick += BtnAvatarPreview_LeftClick;
            AddChild(btnAvatarPreview);

            lblStatus = new XNALabel(WindowManager);
            lblStatus.Name = nameof(lblStatus);
            lblStatus.ClientRectangle = new Rectangle(30, 180, 340, 30);
            lblStatus.TextColor = Color.Yellow;
            AddChild(lblStatus);

            btnSave = new XNAClientButton(WindowManager);
            btnSave.Name = nameof(btnSave);
            btnSave.Text = "Save".L10N("Client:Main:Save");
            btnSave.ClientRectangle = new Rectangle(100, 250, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnSave.LeftClick += BtnSave_LeftClick;
            AddChild(btnSave);

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.Text = "Cancel".L10N("Client:Main:Cancel");
            btnCancel.ClientRectangle = new Rectangle(210, 250, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.LeftClick += BtnCancel_LeftClick;
            AddChild(btnCancel);

            base.Initialize();

            _defaultAvatarTexture = LoadDefaultAvatar();
        }

        private Texture2D LoadDefaultAvatar()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
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

        public void Open()
        {
            lblStatus.Text = "";
            lblStatus.TextColor = Color.Yellow;
            btnSave.Enabled = true;
            btnCancel.Enabled = true;
            tbNickname.Enabled = true;
            tbAvatar.Enabled = true;
            btnAvatarPreview.Enabled = true;

            LoadCurrentUserInfo();

            if (Parent is DarkeningPanel darkeningPanel)
            {
                darkeningPanel.SetPositionAndSize();
                darkeningPanel.Show();
            }

            CenterOnParent();
            Enable();
        }

        private void LoadCurrentUserInfo()
        {
            var user = _accountService.CurrentUser;
            if (user != null)
            {
                tbNickname.Text = user.Nickname ?? "";
                tbAvatar.Text = user.Avatar ?? "";
            }
            else
            {
                tbNickname.Text = "";
                tbAvatar.Text = "";
            }
        }

        private async void BtnAvatarPreview_LeftClick(object sender, EventArgs e)
        {
            string avatarUrl = tbAvatar.Text.Trim();
            if (string.IsNullOrEmpty(avatarUrl))
            {
                lblStatus.Text = "Please enter an avatar URL.".L10N("Client:Main:PleaseEnterAvatarURL");
                return;
            }

            lblStatus.Text = "Loading avatar preview...".L10N("Client:Main:LoadingAvatarPreview");
            lblStatus.TextColor = Color.Yellow;

            await LoadAvatarPreviewAsync(avatarUrl);
        }

        private async Task LoadAvatarPreviewAsync(string avatarUrl)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    var response = await httpClient.GetAsync(avatarUrl);
                    response.EnsureSuccessStatusCode();

                    byte[] imageData = await response.Content.ReadAsByteArrayAsync();

                    WindowManager.AddCallback(new Action(() =>
                    {
                        try
                        {
                            using (var memoryStream = new MemoryStream(imageData))
                            using (var image = SixLabors.ImageSharp.Image.Load(memoryStream))
                            {
                                var texture = AssetLoader.TextureFromImage(image);
                            if (texture != null)
                            {
                                btnAvatarPreview.IdleTexture = texture;
                                btnAvatarPreview.HoverTexture = texture;
                                lblStatus.Text = "Avatar loaded successfully.".L10N("Client:Main:AvatarLoadedSuccessfully");
                                lblStatus.TextColor = Color.LightGreen;
                            }
                            }
                        }
                        catch
                        {
                            lblStatus.Text = "Failed to load avatar image.".L10N("Client:Main:FailedToLoadAvatar");
                            lblStatus.TextColor = Color.Red;
                            btnAvatarPreview.IdleTexture = _defaultAvatarTexture;
                            btnAvatarPreview.HoverTexture = _defaultAvatarTexture;
                        }
                    }), null);
                }
            }
            catch
            {
                WindowManager.AddCallback(new Action(() =>
                {
                    lblStatus.Text = "Failed to load avatar image.".L10N("Client:Main:FailedToLoadAvatar");
                    lblStatus.TextColor = Color.Red;
                    btnAvatarPreview.IdleTexture = _defaultAvatarTexture;
                    btnAvatarPreview.HoverTexture = _defaultAvatarTexture;
                }), null);
            }
        }

        private async void BtnSave_LeftClick(object sender, EventArgs e)
        {
            string nickname = tbNickname.Text.Trim();
            string avatar = tbAvatar.Text.Trim();

            if (string.IsNullOrEmpty(nickname))
            {
                lblStatus.Text = "Nickname cannot be empty.".L10N("Client:Main:NicknameCannotBeEmpty");
                lblStatus.TextColor = Color.Red;
                return;
            }

            btnSave.Enabled = false;
            btnCancel.Enabled = false;
            tbNickname.Enabled = false;
            tbAvatar.Enabled = false;
            btnAvatarPreview.Enabled = false;

            lblStatus.Text = "Saving profile...".L10N("Client:Main:SavingProfile");
            lblStatus.TextColor = Color.Yellow;

            bool success = await _accountService.UpdateProfileAsync(nickname, avatar);

            if (success)
            {
                lblStatus.Text = "Profile updated successfully!".L10N("Client:Main:ProfileUpdatedSuccessfully");
                lblStatus.TextColor = Color.LightGreen;

                await Task.Delay(1000);
                Disable();
            }
            else
            {
                lblStatus.Text = $"Failed to update profile: {_accountService.LastError}".L10N("Client:Main:FailedToUpdateProfile");
                lblStatus.TextColor = Color.Red;

                btnSave.Enabled = true;
                btnCancel.Enabled = true;
                tbNickname.Enabled = true;
                tbAvatar.Enabled = true;
                btnAvatarPreview.Enabled = true;
            }
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }
    }
}
