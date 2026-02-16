using ClientCore;
using ClientCore.ExternalAccount;
using ClientCore.Extensions;
using ClientGUI;
using ClientGUI.Settings;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DTAClient.DXGUI.Generic.OptionPanels
{
    class AccountOptionsPanel : XNAOptionsPanel
    {
        private readonly ExternalAccountService _accountService;

        private XNALabel lblNickname;
        private XNALabel lblAvatar;
        private XNALabel lblLinkedAccounts;
        private XNALabel lblStatus;

        private XNATextBox tbNickname;
        private XNATextBox tbAvatar;

        private XNAClientButton btnAvatarPreview;
        private XNAClientButton btnSave;

        private XNAListBox lbLinkedAccounts;

        private XNAClientButton btnLinkGitHub;
        private XNAClientButton btnLinkQQ;
        private XNAClientButton btnLinkWeChat;
        private XNAClientButton btnLinkEmail;
        private XNAClientButton btnUnlink;

        public AccountOptionsPanel(WindowManager windowManager, UserINISettings iniSettings, ExternalAccountService accountService)
            : base(windowManager, iniSettings)
        {
            _accountService = accountService;
        }

        public override void Initialize()
        {
            base.Initialize();

            Name = "AccountOptionsPanel";

            int y = 14;

            lblNickname = new XNALabel(WindowManager);
            lblNickname.Name = nameof(lblNickname);
            lblNickname.Text = "Nickname:".L10N("Client:DTAConfig:Nickname");
            lblNickname.ClientRectangle = new Rectangle(12, y, 0, 0);
            AddChild(lblNickname);

            tbNickname = new XNATextBox(WindowManager);
            tbNickname.Name = nameof(tbNickname);
            tbNickname.MaximumTextLength = 50;
            tbNickname.ClientRectangle = new Rectangle(120, y - 2, 250, 19);
            AddChild(tbNickname);

            y += 40;

            lblAvatar = new XNALabel(WindowManager);
            lblAvatar.Name = nameof(lblAvatar);
            lblAvatar.Text = "Avatar URL:".L10N("Client:DTAConfig:AvatarURL");
            lblAvatar.ClientRectangle = new Rectangle(12, y, 0, 0);
            AddChild(lblAvatar);

            tbAvatar = new XNATextBox(WindowManager);
            tbAvatar.Name = nameof(tbAvatar);
            tbAvatar.MaximumTextLength = 500;
            tbAvatar.ClientRectangle = new Rectangle(120, y - 2, 250, 19);
            AddChild(tbAvatar);

            btnAvatarPreview = new XNAClientButton(WindowManager);
            btnAvatarPreview.Name = nameof(btnAvatarPreview);
            btnAvatarPreview.Text = "Preview".L10N("Client:DTAConfig:Preview");
            btnAvatarPreview.ClientRectangle = new Rectangle(380, y - 3, 80, 23);
            btnAvatarPreview.LeftClick += BtnAvatarPreview_LeftClick;
            AddChild(btnAvatarPreview);

            y += 40;

            lblLinkedAccounts = new XNALabel(WindowManager);
            lblLinkedAccounts.Name = nameof(lblLinkedAccounts);
            lblLinkedAccounts.Text = "Linked Accounts:".L10N("Client:DTAConfig:LinkedAccounts");
            lblLinkedAccounts.ClientRectangle = new Rectangle(12, y, 0, 0);
            AddChild(lblLinkedAccounts);

            lbLinkedAccounts = new XNAListBox(WindowManager);
            lbLinkedAccounts.Name = nameof(lbLinkedAccounts);
            lbLinkedAccounts.ClientRectangle = new Rectangle(12, y + 20, 300, 100);
            AddChild(lbLinkedAccounts);

            btnUnlink = new XNAClientButton(WindowManager);
            btnUnlink.Name = nameof(btnUnlink);
            btnUnlink.Text = "Unlink".L10N("Client:DTAConfig:Unlink");
            btnUnlink.ClientRectangle = new Rectangle(320, y + 20, 80, 23);
            btnUnlink.LeftClick += BtnUnlink_LeftClick;
            AddChild(btnUnlink);

            y += 140;

            var lblLinkNew = new XNALabel(WindowManager);
            lblLinkNew.Name = "lblLinkNew";
            lblLinkNew.Text = "Link New Account:".L10N("Client:DTAConfig:LinkNewAccount");
            lblLinkNew.ClientRectangle = new Rectangle(12, y, 0, 0);
            AddChild(lblLinkNew);

            y += 30;

            btnLinkGitHub = new XNAClientButton(WindowManager);
            btnLinkGitHub.Name = nameof(btnLinkGitHub);
            btnLinkGitHub.Text = "GitHub".L10N("Client:DTAConfig:GitHub");
            btnLinkGitHub.ClientRectangle = new Rectangle(12, y, 80, 23);
            btnLinkGitHub.LeftClick += BtnLinkGitHub_LeftClick;
            AddChild(btnLinkGitHub);

            btnLinkQQ = new XNAClientButton(WindowManager);
            btnLinkQQ.Name = nameof(btnLinkQQ);
            btnLinkQQ.Text = "QQ".L10N("Client:DTAConfig:QQ");
            btnLinkQQ.ClientRectangle = new Rectangle(100, y, 80, 23);
            btnLinkQQ.LeftClick += BtnLinkQQ_LeftClick;
            AddChild(btnLinkQQ);

            btnLinkWeChat = new XNAClientButton(WindowManager);
            btnLinkWeChat.Name = nameof(btnLinkWeChat);
            btnLinkWeChat.Text = "WeChat".L10N("Client:DTAConfig:WeChat");
            btnLinkWeChat.ClientRectangle = new Rectangle(188, y, 80, 23);
            btnLinkWeChat.LeftClick += BtnLinkWeChat_LeftClick;
            AddChild(btnLinkWeChat);

            btnLinkEmail = new XNAClientButton(WindowManager);
            btnLinkEmail.Name = nameof(btnLinkEmail);
            btnLinkEmail.Text = "Email".L10N("Client:DTAConfig:Email");
            btnLinkEmail.ClientRectangle = new Rectangle(276, y, 80, 23);
            btnLinkEmail.LeftClick += BtnLinkEmail_LeftClick;
            AddChild(btnLinkEmail);

            y += 40;

            lblStatus = new XNALabel(WindowManager);
            lblStatus.Name = nameof(lblStatus);
            lblStatus.ClientRectangle = new Rectangle(12, y, 400, 30);
            lblStatus.TextColor = Color.Yellow;
            AddChild(lblStatus);

            y += 40;

            btnSave = new XNAClientButton(WindowManager);
            btnSave.Name = nameof(btnSave);
            btnSave.Text = "Save Profile".L10N("Client:DTAConfig:SaveProfile");
            btnSave.ClientRectangle = new Rectangle(12, y, 120, 23);
            btnSave.LeftClick += BtnSave_LeftClick;
            AddChild(btnSave);
        }

        public override void Load()
        {
            base.Load();

            LoadCurrentUserInfo();
            RefreshLinkedAccountsList();
        }

        public override bool Save()
        {
            return base.Save();
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

        private void RefreshLinkedAccountsList()
        {
            lbLinkedAccounts.Clear();

            var user = _accountService.CurrentUser;
            if (user?.Identities != null)
            {
                foreach (var identity in user.Identities)
                {
                    string displayText = $"{identity.Provider} ({identity.IdentityId})";
                    lbLinkedAccounts.AddItem(displayText);
                    lbLinkedAccounts.Items[lbLinkedAccounts.Items.Count - 1].Tag = identity;
                }
            }
        }

        private async void BtnAvatarPreview_LeftClick(object sender, EventArgs e)
        {
            string avatarUrl = tbAvatar.Text.Trim();
            if (string.IsNullOrEmpty(avatarUrl))
            {
                lblStatus.Text = "Please enter an avatar URL.".L10N("Client:DTAConfig:PleaseEnterAvatarURL");
                return;
            }

            lblStatus.Text = "Loading avatar preview...".L10N("Client:DTAConfig:LoadingAvatarPreview");
            lblStatus.TextColor = Color.Yellow;

            await LoadAvatarPreviewAsync(avatarUrl);
        }

        private async Task LoadAvatarPreviewAsync(string avatarUrl)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
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
                                btnAvatarPreview.IdleTexture = texture;
                                btnAvatarPreview.HoverTexture = texture;
                                lblStatus.Text = "Avatar loaded successfully.".L10N("Client:DTAConfig:AvatarLoadedSuccessfully");
                                lblStatus.TextColor = Color.LightGreen;
                            }
                            }
                        }
                        catch
                        {
                            lblStatus.Text = "Failed to load avatar image.".L10N("Client:DTAConfig:FailedToLoadAvatar");
                            lblStatus.TextColor = Color.Red;
                        }
                    }), null);
                }
            }
            catch
            {
                WindowManager.AddCallback(new Action(() =>
                {
                    lblStatus.Text = "Failed to load avatar image.".L10N("Client:DTAConfig:FailedToLoadAvatar");
                    lblStatus.TextColor = Color.Red;
                }), null);
            }
        }

        private async void BtnSave_LeftClick(object sender, EventArgs e)
        {
            string nickname = tbNickname.Text.Trim();
            string avatar = tbAvatar.Text.Trim();

            if (string.IsNullOrEmpty(nickname))
            {
                lblStatus.Text = "Nickname cannot be empty.".L10N("Client:DTAConfig:NicknameCannotBeEmpty");
                lblStatus.TextColor = Color.Red;
                return;
            }

            btnSave.Enabled = false;
            lblStatus.Text = "Saving profile...".L10N("Client:DTAConfig:SavingProfile");
            lblStatus.TextColor = Color.Yellow;

            bool success = await _accountService.UpdateProfileAsync(nickname, avatar);

            if (success)
            {
                lblStatus.Text = "Profile updated successfully!".L10N("Client:DTAConfig:ProfileUpdatedSuccessfully");
                lblStatus.TextColor = Color.LightGreen;
            }
            else
            {
                lblStatus.Text = $"Failed to update profile: {_accountService.LastError}".L10N("Client:DTAConfig:FailedToUpdateProfile");
                lblStatus.TextColor = Color.Red;
            }

            btnSave.Enabled = true;
        }

        private async void BtnLinkGitHub_LeftClick(object sender, EventArgs e)
        {
            await LinkProviderAsync("github");
        }

        private async void BtnLinkQQ_LeftClick(object sender, EventArgs e)
        {
            await LinkProviderAsync("qq");
        }

        private async void BtnLinkWeChat_LeftClick(object sender, EventArgs e)
        {
            await LinkProviderAsync("wechat");
        }

        private async void BtnLinkEmail_LeftClick(object sender, EventArgs e)
        {
            await LinkProviderAsync("email");
        }

        private async Task LinkProviderAsync(string provider)
        {
            lblStatus.Text = $"Linking {provider} account...".L10N("Client:DTAConfig:LinkingAccount");
            lblStatus.TextColor = Color.Yellow;

            bool success = await _accountService.LinkProviderAsync(provider, "code", "state");

            if (success)
            {
                lblStatus.Text = $"{provider} account linked successfully!".L10N("Client:DTAConfig:AccountLinkedSuccessfully");
                lblStatus.TextColor = Color.LightGreen;
                RefreshLinkedAccountsList();
            }
            else
            {
                lblStatus.Text = $"Failed to link {provider} account: {_accountService.LastError}".L10N("Client:DTAConfig:FailedToLinkAccount");
                lblStatus.TextColor = Color.Red;
            }
        }

        private async void BtnUnlink_LeftClick(object sender, EventArgs e)
        {
            if (lbLinkedAccounts.SelectedIndex == -1)
            {
                lblStatus.Text = "Please select an account to unlink.".L10N("Client:DTAConfig:PleaseSelectAccountToUnlink");
                lblStatus.TextColor = Color.Red;
                return;
            }

            var selectedIdentity = lbLinkedAccounts.SelectedItem.Tag as IdentityInfo;
            if (selectedIdentity == null)
                return;

            btnUnlink.Enabled = false;
            lblStatus.Text = $"Unlinking {selectedIdentity.Provider} account...".L10N("Client:DTAConfig:UnlinkingAccount");
            lblStatus.TextColor = Color.Yellow;

            bool success = await _accountService.UnlinkProviderByProviderAsync(selectedIdentity.Provider);

            if (success)
            {
                lblStatus.Text = $"{selectedIdentity.Provider} account unlinked successfully!".L10N("Client:DTAConfig:AccountUnlinkedSuccessfully");
                lblStatus.TextColor = Color.LightGreen;
                RefreshLinkedAccountsList();
            }
            else
            {
                lblStatus.Text = $"Failed to unlink account: {_accountService.LastError}".L10N("Client:DTAConfig:FailedToUnlinkAccount");
                lblStatus.TextColor = Color.Red;
            }

            btnUnlink.Enabled = true;
        }
    }
}
